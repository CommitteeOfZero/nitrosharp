﻿using System.Collections.Immutable;

namespace NitroSharp.NsScript
{
    public sealed class DiagnosticBuilder
    {
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;

        public DiagnosticBuilder()
        {
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        }

        public int Count => _diagnostics.Count;

        public void Add(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public void Report(DiagnosticId diagnosticId, TextSpan textSpan)
        {
            _diagnostics.Add(Diagnostic.Create(textSpan, diagnosticId));
        }

        public void Report(DiagnosticId diagnosticId, TextSpan textSpan, params object[] arguments)
        {
            _diagnostics.Add(Diagnostic.Create(textSpan, diagnosticId, arguments));
        }

        public DiagnosticBag ToImmutableBag()
        {
            return new(_diagnostics.ToImmutable());
        }
    }
}
