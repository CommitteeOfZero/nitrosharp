using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript
{
    public static class NsScript
    {
        private static readonly Encoding s_defaultEncoding = CodePagesEncodingProvider.Instance.GetEncoding("shift-jis");

        public static IEnumerable<SyntaxToken> ParseTokens(string text, NsScriptLexer.Context initialContext = NsScriptLexer.Context.Code)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lexer = new NsScriptLexer(text, initialContext);
            SyntaxToken token = null;
            while (token?.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                token = lexer.Lex();
                yield return token;
            }
        }

        public static NsSyntaxTree ParseScript(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var parser = new NsScriptParser(new NsScriptLexer(text));
            return parser.ParseScript();
        }

        public static NsSyntaxTree ParseScript(string fileName, Stream stream, Encoding encoding = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must support read operation.", nameof(stream));
            }

            string sourceText = ReadStream(stream, encoding);
            var parser = new NsScriptParser(new NsScriptLexer(sourceText, fileName));
            return parser.ParseScript();
        }

        public static Expression ParseExpression(string expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var parser = new NsScriptParser(new NsScriptLexer(expression));
            return parser.ParseExpression();
        }

        public static Statement ParseStatement(string statement)
        {
            if (statement == null)
            {
                throw new ArgumentNullException(nameof(statement));
            }

            var parser = new NsScriptParser(new NsScriptLexer(statement));
            return parser.ParseStatement();
        }

        private static string ReadStream(Stream stream, Encoding encoding)
        {
            encoding = encoding ?? s_defaultEncoding;
            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
