using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
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
            (Variant, Subroutine) = (LookupResultVariant.Subroutine, subroutine);
        }

        public LookupResult(BuiltInFunction builtInFunction) : this()
        {
            (Variant, BuiltInFunction) = (LookupResultVariant.BuiltInFunction, builtInFunction);
        }

        public LookupResult(BuiltInConstant builtInConstant) : this()
        {
            (Variant, BuiltInConstant) = (LookupResultVariant.BuiltInConstant, builtInConstant);
        }

        public LookupResult(LookupResultVariant variant, string name) : this()
        {
            (Variant, Global) = (variant, name);
        }

        public static LookupResult Empty = default;

        public bool IsEmpty => Variant == LookupResultVariant.Empty;
    }

    internal struct CompileTimeBezierSegment
    {
        private int _count;

#pragma warning disable CS0649
        public BezierControlPoint P0;
        public BezierControlPoint P1;
        public BezierControlPoint P2;
        public BezierControlPoint P3;
#pragma warning restore CS0649

        public readonly int PointCount => _count;
        public readonly bool IsComplete => _count == 4;

        public Span<BezierControlPoint> Points
            => MemoryMarshal.CreateSpan(ref P0, 4);

        public bool AddPoint(BezierControlPoint pt)
        {
            if (_count == 4) { return false; }
            Points[_count++] = pt;
            return true;
        }
    }

    internal readonly struct Checker
    {
        private readonly EmitContext _context;
        private readonly SourceModuleSymbol _module;
        private readonly DiagnosticBuilder _diagnostics;

        public Checker(EmitContext context, SubroutineSymbol subroutine)
        {
            _context = context;
            _module = subroutine.DeclaringSourceFile.Module;
            _diagnostics = context.DiagnosticBuilder;
        }

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
            string modulePath = callChapterStmt.TargetModule.Value;
            try
            {
                SourceModuleSymbol targetSourceModule = _context.Compilation.GetSourceModule(modulePath);
                ChapterSymbol? chapter = targetSourceModule.LookupChapter("main");
                if (chapter is null)
                {
                    Report(callChapterStmt, callChapterStmt.TargetModule.Span, DiagnosticId.ChapterMainNotFound);
                }
                return chapter;

            }
            catch (FileNotFoundException)
            {
                string moduleName = callChapterStmt.TargetModule.Value;
                Report(callChapterStmt, callChapterStmt.TargetModule.Span, DiagnosticId.ExternalModuleNotFound, moduleName);
                return null;
            }
        }

        public SceneSymbol? ResolveCallSceneTarget(CallSceneStatement callSceneStmt)
        {
            if (callSceneStmt.TargetModule is null)
            {
                return LookupScene(callSceneStmt, callSceneStmt.TargetScene);
            }

            Spanned<string> targetModule = callSceneStmt.TargetModule.Value;
            string modulePath = targetModule.Value;
            try
            {
                SourceModuleSymbol targetSourceModule = _context.Compilation.GetSourceModule(modulePath);
                SceneSymbol? scene = targetSourceModule.LookupScene(callSceneStmt.TargetScene.Value);
                if (scene is null)
                {
                    ReportUnresolvedIdentifier(callSceneStmt, callSceneStmt.TargetScene);
                }

                return scene;
            }
            catch (FileNotFoundException)
            {
                string moduleName = targetModule.Value;
                Report(callSceneStmt, targetModule.Span, DiagnosticId.ExternalModuleNotFound, moduleName);
                return null;
            }
        }

        public LookupResult LookupNonInvocableSymbol(NameExpression name)
        {
            if (name.Sigil == SigilKind.Dollar || _context.TryGetVariableToken(name.Name, out _))
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

        public void Report(SyntaxNode node, TextSpan span, DiagnosticId diagnosticId)
        {
            var location = new SourceLocation(node.SyntaxTree.SourceText, span);
            _diagnostics.Add(Diagnostic.Create(location, diagnosticId));
        }

        public void Report(SyntaxNode node, TextSpan span, DiagnosticId diagnosticId, params object[] args)
        {
            var location = new SourceLocation(node.SyntaxTree.SourceText, span);
            _diagnostics.Add(Diagnostic.Create(location, diagnosticId, args));
        }
    }
}
