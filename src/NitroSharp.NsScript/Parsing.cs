using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Syntax.PXml;
using NitroSharp.NsScript.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript
{
    /// <summary>
    /// The public API for lexing and parsing source code.
    /// </summary>
    public static class Parsing
    {
        public static IEnumerable<SyntaxToken> ParseTokens(string text, LexingMode lexingMode = LexingMode.Normal)
        {
            return ParseTokens(SourceText.From(text), lexingMode);
        }

        public static IEnumerable<SyntaxToken> ParseTokens(SourceText text, LexingMode lexingMode = LexingMode.Normal)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lexer = new Lexer(text, lexingMode);
            SyntaxToken token = null;
            while (token?.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                token = lexer.Lex();
                yield return token;
            }
        }

        public static SyntaxTree ParseText(string text) => ParseText(SourceText.From(text));
        public static SyntaxTree ParseText(SourceText sourceText)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var parser = new Parser(new Lexer(sourceText));
            var root = parser.ParseSourceFile();
            return SyntaxTree.Create(sourceText, root, parser.DiagnosticBuilder);
        }

        public static SyntaxTree ParseText(Stream stream, string filePath, Encoding encoding = null)
        {
            var sourceText = SourceText.From(stream, filePath, encoding);
            return ParseText(sourceText);
        }

        public static SyntaxTree ParseExpression(string expression) => ParseString(expression, p => p.ParseExpression());
        public static SyntaxTree ParseStatement(string statement) => ParseString(statement, p => p.ParseStatement());
        public static SyntaxTree ParseMemberDeclaration(string text) => ParseString(text, p => p.ParseMemberDeclaration());

        private static SyntaxTree ParseString(string text, Func<Parser, SyntaxNode> parseFunc)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var sourceText = SourceText.From(text);
            var parser = new Parser(new Lexer(sourceText));
            var root = parseFunc(parser);
            return SyntaxTree.Create(sourceText, root, parser.DiagnosticBuilder);
        }

        public static PXmlContent ParsePXmlString(string text)
        {
            var parser = new PXmlParser(text);
            return parser.Parse();
        }
    }
}
