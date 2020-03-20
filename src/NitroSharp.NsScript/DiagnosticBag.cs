using System.Collections.Immutable;
using System;

namespace NitroSharp.NsScript
{
    public sealed class DiagnosticBag
    {
        private struct DiagnosticCollections
        {
            public ImmutableArray<Diagnostic> Information;
            public ImmutableArray<Diagnostic> Warnings;
            public ImmutableArray<Diagnostic> Errors;
        }

        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly Lazy<DiagnosticCollections> _collections;

        internal DiagnosticBag(ImmutableArray<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
            _collections = new Lazy<DiagnosticCollections>(Categorize);
        }

        public bool IsEmpty => _diagnostics.Length == 0;

        public ImmutableArray<Diagnostic> All => _diagnostics;
        public ImmutableArray<Diagnostic> Information
            => IsEmpty ? ImmutableArray<Diagnostic>.Empty : _collections.Value.Information;
        public ImmutableArray<Diagnostic> Warnings
            => IsEmpty ? ImmutableArray<Diagnostic>.Empty : _collections.Value.Warnings;
        public ImmutableArray<Diagnostic> Errors
            => IsEmpty ? ImmutableArray<Diagnostic>.Empty : _collections.Value.Errors;

        private DiagnosticCollections Categorize()
        {
            var information = ImmutableArray.CreateBuilder<Diagnostic>();
            var warnings = ImmutableArray.CreateBuilder<Diagnostic>();
            var errors = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var diagnostic in _diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Info:
                        information.Add(diagnostic);
                        break;
                    case DiagnosticSeverity.Warning:
                        warnings.Add(diagnostic);
                        break;
                    case DiagnosticSeverity.Error:
                        errors.Add(diagnostic);
                        break;
                }
            }

            return new DiagnosticCollections
            {
                Information = information.ToImmutable(),
                Warnings = warnings.ToImmutable(),
                Errors = errors.ToImmutable()
            };
        }
    }
}
