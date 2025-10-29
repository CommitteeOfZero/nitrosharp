using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript;

public sealed class SyntaxTree
{
    internal SyntaxTree(SourceText sourceText, SyntaxNode root, DiagnosticBuilder diagnostics)
    {
        SourceText = sourceText;
        Root = root;
        DiagnosticBuilder = diagnostics;
        BindNode(root);
    }

    public SourceText SourceText { get; }
    public SyntaxNode Root { get; }

    internal DiagnosticBuilder DiagnosticBuilder { get; }
    public DiagnosticCollection Diagnostics => DiagnosticBuilder.ToImmutable();

    private void BindNode(SyntaxNode node)
    {
        node.Bind(this);
        foreach (SyntaxNode child in node.GetChildren())
        {
            BindNode(child);
        }
    }
}
