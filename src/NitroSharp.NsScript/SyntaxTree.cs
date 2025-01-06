using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript;

public sealed class SyntaxTree
{
    private readonly DiagnosticBuilder _diagnosticBuilder;
    private DiagnosticBag? _diagnostics;

    internal SyntaxTree(SourceText sourceText, SyntaxNode root, DiagnosticBuilder diagnosticBuilder)
    {
        SourceText = sourceText;
        Root = root;
        _diagnosticBuilder = diagnosticBuilder;
        BindNode(root);
    }

    private void BindNode(SyntaxNode node)
    {
        node.Bind(this);
        foreach (SyntaxNode child in node.GetChildren())
        {
            BindNode(child);
        }
    }

    public SyntaxNode Root { get; }
    public SourceText SourceText { get; }
    public DiagnosticBag Diagnostics
    {
        get
        {
            if (_diagnostics is null || _diagnostics.All.Length != _diagnosticBuilder.Count)
            {
                _diagnostics = _diagnosticBuilder.ToImmutableBag();
            }

            return _diagnostics;
        }
    }
}
