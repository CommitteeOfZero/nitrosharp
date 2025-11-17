using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public sealed class SyntaxTree
{
    private SyntaxTree(SourceText sourceText, SyntaxNode root, DiagnosticBuilder diagnostics)
    {
        SourceText = sourceText;
        Root = root;
        DiagnosticBuilder = diagnostics;
        BindNode(root);
    }

    public SourceText SourceText { get; }
    public SyntaxNode Root { get; }

    internal DiagnosticBuilder DiagnosticBuilder { get; }
    public ImmutableArray<Diagnostic> Diagnostics => DiagnosticBuilder.ToImmutable();

    public static SyntaxTree ParseText(SourceText sourceText)
    {
        var diagnostics = new DiagnosticBuilder();
        var parser = new Parser(new Lexer(sourceText, diagnostics));
        SourceFileRoot root = parser.ParseSourceFile();
        return new SyntaxTree(sourceText, root, diagnostics);
    }

    public static SyntaxTree ParseExpression(string expression)
        => ParseString(expression, p => p.ParseExpression());

    public static SyntaxTree ParseStatement(string statement)
        => ParseString(statement, p => p.ParseStatement());

    public static SyntaxTree ParseSubroutineDeclaration(string text)
        => ParseString(text, p => p.ParseSubroutineDeclaration());

    private static SyntaxTree ParseString(string text, Func<Parser, SyntaxNode> parseFunc)
    {
        var diagnostics = new DiagnosticBuilder();
        var sourceText = SourceText.From(text);
        var parser = new Parser(new Lexer(sourceText, diagnostics));
        SyntaxNode root = parseFunc(parser);
        return new SyntaxTree(sourceText, root, diagnostics);
    }

    private void BindNode(SyntaxNode node)
    {
        node.Bind(this);
        foreach (SyntaxNode child in node.GetChildren())
        {
            BindNode(child);
        }
    }
}
