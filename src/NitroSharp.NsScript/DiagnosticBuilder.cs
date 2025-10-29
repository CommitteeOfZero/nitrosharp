using System.Collections.Immutable;

namespace NitroSharp.NsScript;

internal readonly struct DiagnosticBuilder
{
    private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;

    public DiagnosticBuilder()
    {
        _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
    }

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

    public DiagnosticCollection ToImmutable()
    {
        return new DiagnosticCollection(_diagnostics.ToImmutable());
    }
}
