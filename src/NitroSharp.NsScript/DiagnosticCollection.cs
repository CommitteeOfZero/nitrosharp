using System.Collections.Immutable;
using System;

namespace NitroSharp.NsScript;

public sealed class DiagnosticCollection
{
    internal static readonly DiagnosticCollection Empty = new(ImmutableArray<Diagnostic>.Empty);

    private struct Categories
    {
        public ImmutableArray<Diagnostic> Information;
        public ImmutableArray<Diagnostic> Warnings;
        public ImmutableArray<Diagnostic> Errors;
    }

    private readonly Lazy<Categories> _collections;

    internal DiagnosticCollection(ImmutableArray<Diagnostic> diagnostics)
    {
        All = diagnostics;
        _collections = new Lazy<Categories>(Categorize);
    }

    public bool IsEmpty => All.Length == 0;

    public ImmutableArray<Diagnostic> All { get; }
    public ImmutableArray<Diagnostic> Information
        => IsEmpty ? ImmutableArray<Diagnostic>.Empty : _collections.Value.Information;
    public ImmutableArray<Diagnostic> Warnings
        => IsEmpty ? ImmutableArray<Diagnostic>.Empty : _collections.Value.Warnings;
    public ImmutableArray<Diagnostic> Errors
        => IsEmpty ? ImmutableArray<Diagnostic>.Empty : _collections.Value.Errors;

    private Categories Categorize()
    {
        var information = ImmutableArray.CreateBuilder<Diagnostic>();
        var warnings = ImmutableArray.CreateBuilder<Diagnostic>();
        var errors = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (Diagnostic diagnostic in All)
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

        return new Categories
        {
            Information = information.ToImmutable(),
            Warnings = warnings.ToImmutable(),
            Errors = errors.ToImmutable()
        };
    }
}
