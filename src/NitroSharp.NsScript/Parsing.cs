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
    /// The public API for parsing NSS and PXml.
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

        public static SourceFile ParseScript(string text) => ParseScript(SourceText.From(text));
        public static SourceFile ParseScript(SourceText text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var parser = new Parser(new Lexer(text));
            return parser.ParseScript();
        }

        public static SourceFile ParseScript(Stream stream, string fileName, Encoding encoding = null)
        {
            var sourceText = SourceText.From(stream, fileName, encoding);
            var parser = new Parser(new Lexer(sourceText));
            return parser.ParseScript();
        }

        public static Expression ParseExpression(string expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var parser = new Parser(new Lexer(SourceText.From(expression)));
            return parser.ParseExpression();
        }

        public static Statement ParseStatement(string statement)
        {
            if (statement == null)
            {
                throw new ArgumentNullException(nameof(statement));
            }

            var parser = new Parser(new Lexer(SourceText.From(statement)));
            return parser.ParseStatement();
        }

        public static MemberDeclaration ParseMemberDeclaration(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var parser = new Parser(new Lexer(SourceText.From(text)));
            return parser.ParseMemberDeclaration();
        }

        public static PXmlContent ParsePXmlString(string text)
        {
            var parser = new PXmlParser(text);
            return parser.Parse();
        }
    }
}
