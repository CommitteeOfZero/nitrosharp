using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Syntax.Markup;
using System;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript;

public static class Parsing
{
    public static SyntaxTree ParseText(string text)
        => ParseText(SourceText.From(text));

    public static SyntaxTree ParseText(SourceText sourceText)
    {
        var diagnostics = new DiagnosticBuilder();
        var parser = new Parser(new Lexer(sourceText, diagnostics, LexingMode.Normal));
        SourceFileRoot root = parser.ParseSourceFile();
        return new SyntaxTree(sourceText, root, diagnostics);
    }

    public static SyntaxTree ParseText(Stream stream, string filePath, Encoding? encoding = null)
    {
        var sourceText = SourceText.From(stream, new ResolvedPath(filePath), encoding);
        return ParseText(sourceText);
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

    public static MarkupContent ParseMarkup(string text)
    {
        var parser = new MarkupParser(text);
        return parser.Parse();
    }
}
