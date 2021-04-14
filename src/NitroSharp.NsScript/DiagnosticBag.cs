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
        private readonly Lazy<DiagnosticCollections> _collections;

        internal DiagnosticBag(ImmutableArray<Diagnostic> diagnostics)
        {
            All = diagnostics;
            _collections = new Lazy<DiagnosticCollections>(Categorize);
        }

        public bool IsEmpty => All.Length == 0;

        public ImmutableArray<Diagnostic> All { get; }
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
            foreach (var diagnostic in All)
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
