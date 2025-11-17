using System.Collections.Immutable;

namespace NitroSharp.NsScript;

internal readonly struct DiagnosticBuilder
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

    public void MergeFrom(DiagnosticBuilder source)
    {
        foreach (Diagnostic diagnostic in source._diagnostics)
        {
            _diagnostics.Add(diagnostic);
        }
    }

    public ImmutableArray<Diagnostic> ToImmutable()
    {
        return _diagnostics.ToImmutable();
    }
}
