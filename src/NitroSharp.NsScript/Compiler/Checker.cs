using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Compiler
{
    internal enum LookupResultVariant : byte
    {
        Empty = 0,
        Subroutine,
        BuiltInFunction,
        BuiltInConstant,
        Variable,
        Flag
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct LookupResult
    {
        [FieldOffset(0)]
        public readonly LookupResultVariant Variant;

        [FieldOffset(4)]
        public readonly BuiltInFunction BuiltInFunction;

        [FieldOffset(4)]
        public readonly BuiltInConstant BuiltInConstant;

        [FieldOffset(8)]
        public readonly SubroutineSymbol? Subroutine;

        [FieldOffset(8)]
        public readonly ParameterSymbol? Parameter;

        [FieldOffset(8)]
        public readonly string? Global;

        public LookupResult(SubroutineSymbol subroutine) : this()
        {
            Variant = LookupResultVariant.Subroutine;
            Subroutine = subroutine;
        }

        public LookupResult(BuiltInFunction builtInFunction) : this()
        {
            Variant = LookupResultVariant.BuiltInFunction;
            BuiltInFunction = builtInFunction;
        }

        public LookupResult(BuiltInConstant builtInConstant) : this()
        {
            Variant = LookupResultVariant.BuiltInConstant;
            BuiltInConstant = builtInConstant;
        }

        public LookupResult(LookupResultVariant variant, string name) : this()
        {
            Variant = variant;
            Global = name;
        }

        public static LookupResult Empty = default;

        public bool IsEmpty => Variant == LookupResultVariant.Empty;
    }

    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal struct CompileTimeBezierSegment
    {
#pragma warning disable CS0649
        public BezierControlPoint P0;
        public BezierControlPoint P1;
        public BezierControlPoint P2;
        public BezierControlPoint P3;
#pragma warning restore CS0649

        public int PointCount { get; private set; }

        public readonly bool IsComplete => PointCount == 4;

        public Span<BezierControlPoint> Points
            => MemoryMarshal.CreateSpan(ref P0, 4);

        public bool AddPoint(BezierControlPoint pt)
        {
            if (PointCount == 4) { return false; }
            Points[PointCount++] = pt;
            return true;
        }
    }

    internal readonly struct Checker(EmitContext context, SubroutineSymbol subroutine)
    {
        private readonly SourceModuleSymbol _module = subroutine.DeclaringSourceFile.Module;
        private readonly DiagnosticBuilder _diagnostics = context.DiagnosticBuilder;

        public LookupResult ResolveAssignmentTarget(Expression expression)
        {
            if (expression is NameExpression nameExpression)
            {
                return LookupNonInvocableSymbol(nameExpression);
            }

            Report(expression, DiagnosticId.BadAssignmentTarget);
            return LookupResult.Empty;
        }

        public ChapterSymbol? ResolveCallChapterTarget(CallChapterStatement callChapterStmt)
        {
            Spanned<string> targetName = callChapterStmt.TargetModule;
            SourceModuleSymbol? targetModule = context.Compilation.TryGetSourceModule(targetName.Value);
            if (targetModule is null)
            {
                Report(callChapterStmt, targetName.Span, DiagnosticId.ExternalModuleNotFound, targetName.Value);
                return null;
            }

            ChapterSymbol? chapter = targetModule.LookupChapter("main");
            if (chapter is null)
            {
                Report(callChapterStmt, targetName.Span, DiagnosticId.ChapterMainNotFound);
            }
            return chapter;
        }

        public SceneSymbol? ResolveCallSceneTarget(CallSceneStatement callSceneStmt)
        {
            if (callSceneStmt.TargetModule is null)
            {
                return LookupScene(callSceneStmt, callSceneStmt.TargetScene);
            }

            Spanned<string> targetModuleName = callSceneStmt.TargetModule.Value;
            SourceModuleSymbol? targetModule = context.Compilation.TryGetSourceModule(targetModuleName.Value);
            if (targetModule is null)
            {
                Report(callSceneStmt, targetModuleName.Span, DiagnosticId.ExternalModuleNotFound, targetModuleName.Value);
                return null;
            }

            SceneSymbol? scene = targetModule.LookupScene(callSceneStmt.TargetScene.Value);
            if (scene is null)
            {
                ReportUnresolvedIdentifier(callSceneStmt, callSceneStmt.TargetScene);
            }

            return scene;
        }

        public LookupResult LookupNonInvocableSymbol(NameExpression name)
        {
            if (name.Sigil == SigilKind.Dollar || context.TryGetVariableToken(name.Name, out _))
            {
                return new LookupResult(LookupResultVariant.Variable, name.Name);
            }
            if (name.Sigil == SigilKind.Hash)
            {
                return new LookupResult(LookupResultVariant.Flag, name.Name);
            }

            BuiltInConstant? builtInConstant = WellKnownSymbols.LookupBuiltInConstant(name.Name);
            if (builtInConstant.HasValue)
            {
                return new LookupResult(builtInConstant.Value);
            }

            return LookupResult.Empty;
        }

        public LookupResult LookupFunction(SyntaxNode callExpression, Spanned<string> identifier)
        {
            string name = identifier.Value;
            BuiltInFunction? builtInFunction = WellKnownSymbols.LookupBuiltInFunction(name);
            if (builtInFunction.HasValue)
            {
                return new LookupResult(builtInFunction.Value);
            }

            FunctionSymbol? function = _module.LookupFunction(name);
            if (function is not null)
            {
                return new LookupResult(function);
            }

            ReportUnresolvedIdentifier(callExpression, identifier);
            return LookupResult.Empty;
        }

        public ChapterSymbol? LookupChapter(SyntaxNode callExpression, Spanned<string> identifier)
        {
            ChapterSymbol? chapter = _module.LookupChapter(identifier.Value);
            if (chapter is not null) { return chapter; }

            ReportUnresolvedIdentifier(callExpression, identifier);
            return null;
        }

        public SceneSymbol? LookupScene(SyntaxNode callExpression, Spanned<string> identifier)
        {
            SceneSymbol? scene = _module.LookupScene(identifier.Value);
            if (scene is not null) { return scene; }

            ReportUnresolvedIdentifier(callExpression, identifier);
            return null;
        }

        public bool ParseBezierCurve(
            BezierExpression bezierExpr,
            out ImmutableArray<CompileTimeBezierSegment> segments)
        {
            ReadOnlySpan<BezierControlPoint> remainingPoints = bezierExpr.ControlPoints.AsSpan();
            var mutSegments = ImmutableArray.CreateBuilder<CompileTimeBezierSegment>();
            CompileTimeBezierSegment seg = default;
            while (consumePoint(ref remainingPoints, out BezierControlPoint pt))
            {
                if (pt.IsStartingPoint)
                {
                    if (seg.PointCount is 0 or 3)
                    {
                        seg.AddPoint(pt);
                        if (seg.IsComplete)
                        {
                            mutSegments.Add(seg);
                            seg = default;
                            if (remainingPoints.Length > 0)
                            {
                                seg.AddPoint(pt);
                            }
                        }
                    }
                    else
                    {
                        goto error;
                    }
                }
                else if (seg.PointCount is > 0 and < 3)
                {
                    seg.AddPoint(pt);
                }
            }
            if (mutSegments.Count == 0 || seg is { PointCount: > 0, IsComplete: false })
            {
                goto error;
            }

            segments = mutSegments.ToImmutable();
            return true;

        error:
            Report(bezierExpr, DiagnosticId.InvalidBezierCurve);
            segments = default;
            return false;

            static bool consumePoint(
                ref ReadOnlySpan<BezierControlPoint> points,
                out BezierControlPoint pt)
            {
                if (points.Length == 0)
                {
                    pt = default;
                    return false;
                }
                pt = points[0];
                points = points[1..];
                return true;
            }
        }

        private void ReportUnresolvedIdentifier(SyntaxNode node, Spanned<string> identifier)
        {
            var location = new SourceLocation(node.SyntaxTree.SourceText, identifier.Span);
            _diagnostics.Add(
                Diagnostic.Create(location, DiagnosticId.UnresolvedIdentifier, identifier.Value)
            );
        }

        public void Report(SyntaxNode node, DiagnosticId diagnosticId)
        {
            var location = new SourceLocation(node.SyntaxTree.SourceText, node.Span);
            _diagnostics.Add(Diagnostic.Create(location, diagnosticId));
        }

        private void Report(SyntaxNode node, TextSpan span, DiagnosticId diagnosticId)
        {
            var location = new SourceLocation(node.SyntaxTree.SourceText, span);
            _diagnostics.Add(Diagnostic.Create(location, diagnosticId));
        }

        private void Report(SyntaxNode node, TextSpan span, DiagnosticId diagnosticId, params object[] args)
        {
            var location = new SourceLocation(node.SyntaxTree.SourceText, span);
            _diagnostics.Add(Diagnostic.Create(location, diagnosticId, args));
        }
    }
}
