using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Syntax.PXml;
using NitroSharp.NsScript.Text;
using System;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript
{
    public static class Parsing
    {
        public static (SyntaxToken token, LexingContext context) LexToken(string text)
        {
            var lexer = new Lexer(SourceText.From(text));
            return (lexer.Lex(), new LexingContext(lexer));
        }

        public static (SyntaxTokenEnumerable tokens, LexingContext context) LexTokens(
            string text, LexingMode mode = LexingMode.Normal)
        {
            var lexer = new Lexer(SourceText.From(text), mode);
            var context = new LexingContext(lexer);
            var enumerable = new SyntaxTokenEnumerable(lexer);
            return (enumerable, context);
        }

        public static (SyntaxTokenEnumerable tokens, LexingContext context) LexTokens(
            SourceText sourceText, LexingMode mode = LexingMode.Normal)
        {
            var lexer = new Lexer(sourceText);
            var context = new LexingContext(lexer);
            var enumerable = new SyntaxTokenEnumerable(lexer);
            return (enumerable, context);
        }

        public static SyntaxTree ParseText(string text) => ParseText(SourceText.From(text));
        public static SyntaxTree ParseText(SourceText sourceText)
        {
            var parser = new Parser(new Lexer(sourceText));
            var root = parser.ParseSourceFile();
            return new SyntaxTree(sourceText, root, parser.DiagnosticBuilder);
        }

        public static SyntaxTree ParseText(Stream stream, string filePath, Encoding? encoding = null)
        {
            var sourceText = SourceText.From(stream, filePath, encoding);
            return ParseText(sourceText);
        }

        public static SyntaxTree ParseExpression(string expression)
            => ParseString(expression, p => p.ParseExpression());

        public static SyntaxTree ParseStatement(string statement)
            => ParseString(statement, p => p.ParseStatement());

        public static SyntaxTree ParseSubroutineDeclaration(string text)
            => ParseString(text, p => p.ParseSubroutineDeclaration());

        private static SyntaxTree ParseString(string text, Func<Parser, SyntaxNode?> parseFunc)
        {
            var sourceText = SourceText.From(text);
            var parser = new Parser(new Lexer(sourceText));
            var root = parseFunc(parser);
            return new SyntaxTree(sourceText, root, parser.DiagnosticBuilder);
        }

        public static PXmlContent ParsePXmlString(string text)
        {
            var parser = new PXmlParser(text);
            return parser.Parse();
        }
    }
}
