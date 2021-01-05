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
        public readonly SubroutineSymbol Subroutine;

        [FieldOffset(8)]
        public readonly ParameterSymbol Parameter;

        [FieldOffset(8)]
        public readonly string Global;

        public LookupResult(SubroutineSymbol subroutine) : this()
            => (Variant, Subroutine) = (LookupResultVariant.Subroutine, subroutine);

        public LookupResult(BuiltInFunction builtInFunction) : this()
            => (Variant, BuiltInFunction) = (LookupResultVariant.BuiltInFunction, builtInFunction);

        public LookupResult(BuiltInConstant builtInConstant) : this()
            => (Variant, BuiltInConstant) = (LookupResultVariant.BuiltInConstant, builtInConstant);

        public LookupResult(LookupResultVariant variant, string name) : this()
            => (Variant, Global) = (variant, name);

        public static LookupResult Empty = default;

        public bool IsEmpty => Variant == LookupResultVariant.Empty;
    }

    internal struct CompileTimeBezierSegment
    {
        private int _count;

#pragma warning disable CS0649
        public BezierControlPointSyntax P0;
        public BezierControlPointSyntax P1;
        public BezierControlPointSyntax P2;
        public BezierControlPointSyntax P3;
#pragma warning restore CS0649

        public int PointCount => _count;
        public bool IsComplete => _count == 4;

        public Span<BezierControlPointSyntax> Points
            => MemoryMarshal.CreateSpan(ref P0, 4);

        public bool AddPoint(BezierControlPointSyntax pt)
        {
            if (_count == 4) { return false; }
            Points[_count++] = pt;
            return true;
        }
    }

    internal readonly struct Checker
    {
        private readonly SubroutineSymbol _subroutine;
        private readonly SourceModuleSymbol _module;
        private readonly Compilation _compilation;
        private readonly DiagnosticBuilder _diagnostics;

        public Checker(SubroutineSymbol subroutine, DiagnosticBuilder diagnostics)
        {
            _subroutine = subroutine;
            _module = subroutine.DeclaringSourceFile.Module;
            _compilation = _module.Compilation;
            _diagnostics = diagnostics;
        }

        public LookupResult ResolveAssignmentTarget(ExpressionSyntax expression)
        {
            if (expression is NameExpressionSyntax nameExpression)
            {
                return LookupNonInvocableSymbol(nameExpression);
            }

            Report(expression, DiagnosticId.BadAssignmentTarget);
            return LookupResult.Empty;
        }

        public ChapterSymbol? ResolveCallChapterTarget(CallChapterStatementSyntax callChapterStmt)
        {
            string modulePath = callChapterStmt.TargetModule.Value;
            try
            {
                SourceModuleSymbol targetSourceModule = _compilation.GetSourceModule(modulePath);
                ChapterSymbol? chapter = targetSourceModule.LookupChapter("main");
                if (chapter == null)
                {
                    Report(callChapterStmt.TargetModule, DiagnosticId.ChapterMainNotFound);
                }
                return chapter;

            }
            catch (FileNotFoundException)
            {
                string moduleName = callChapterStmt.TargetModule.Value;
                Report(callChapterStmt.TargetModule, DiagnosticId.ExternalModuleNotFound, moduleName);
                return null;
            }
        }

        public SceneSymbol? ResolveCallSceneTarget(CallSceneStatementSyntax callSceneStmt)
        {
            if (callSceneStmt.TargetModule == null)
            {
                return LookupScene(callSceneStmt.TargetScene);
            }

            Spanned<string> targetModule = callSceneStmt.TargetModule.Value;
            string modulePath = targetModule.Value;
            try
            {
                SourceModuleSymbol targetSourceModule = _compilation.GetSourceModule(modulePath);
                SceneSymbol? scene = targetSourceModule.LookupScene(callSceneStmt.TargetScene.Value);
                if (scene == null)
                {
                    ReportUnresolvedIdentifier(callSceneStmt.TargetScene);
                }

                return scene;
            }
            catch (FileNotFoundException)
            {
                string moduleName = targetModule.Value;
                Report(targetModule, DiagnosticId.ExternalModuleNotFound, moduleName);
                return null;
            }
        }

        public LookupResult LookupNonInvocableSymbol(NameExpressionSyntax name)
        {
            if (name.Sigil == SigilKind.Dollar || _compilation.TryGetVariableToken(name.Name, out _))
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

        public LookupResult LookupFunction(Spanned<string> identifier)
        {
            string name = identifier.Value;
            BuiltInFunction? builtInFunction = WellKnownSymbols.LookupBuiltInFunction(name);
            if (builtInFunction.HasValue)
            {
                return new LookupResult(builtInFunction.Value);
            }

            FunctionSymbol? function = _module.LookupFunction(name);
            if (function != null)
            {
                return new LookupResult(function);
            }

            ReportUnresolvedIdentifier(identifier);
            return LookupResult.Empty;
        }

        public ChapterSymbol? LookupChapter(Spanned<string> identifier)
        {
            ChapterSymbol? chapter = _module.LookupChapter(identifier.Value);
            if (chapter != null) { return chapter; }

            ReportUnresolvedIdentifier(identifier);
            return null;
        }

        public SceneSymbol? LookupScene(Spanned<string> identifier)
        {
            SceneSymbol? scene = _module.LookupScene(identifier.Value);
            if (scene != null) { return scene; }

            ReportUnresolvedIdentifier(identifier);
            return null;
        }

        public bool ParseBezierCurve(
            BezierExpressionSyntax bezierExpr,
            out ImmutableArray<CompileTimeBezierSegment> segments)
        {
            static bool consumePoint(
                ref ReadOnlySpan<BezierControlPointSyntax> points,
                out BezierControlPointSyntax pt)
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

            ReadOnlySpan<BezierControlPointSyntax> remainingPoints = bezierExpr
                .ControlPoints.AsSpan();
            var mutSegments = ImmutableArray.CreateBuilder<CompileTimeBezierSegment>();
            CompileTimeBezierSegment seg = default;
            while (consumePoint(ref remainingPoints, out BezierControlPointSyntax pt))
            {
                if (pt.IsStartingPoint)
                {
                    if (seg.PointCount == 0 || seg.PointCount == 3)
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
                else if (seg.PointCount > 0 && seg.PointCount < 3)
                {
                    seg.AddPoint(pt);
                }
            }
            if (mutSegments.Count == 0 || (seg.PointCount > 0 && !seg.IsComplete))
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

        private void ReportUnresolvedIdentifier(Spanned<string> identifier)
        {
            _diagnostics.Add(
                Diagnostic.Create(identifier.Span, DiagnosticId.UnresolvedIdentifier, identifier.Value)
            );
        }

        public void Report(Spanned<string> identifier, DiagnosticId diagnosticId)
        {
            _diagnostics.Add(Diagnostic.Create(identifier.Span, diagnosticId));
        }

        public void Report(Spanned<string> identifier, DiagnosticId diagnosticId, params object[] args)
        {
            _diagnostics.Add(Diagnostic.Create(identifier.Span, diagnosticId, args));
        }

        public void Report(SyntaxNode node, DiagnosticId diagnosticId)
        {
            _diagnostics.Add(Diagnostic.Create(node.Span, diagnosticId));
        }
    }
}
