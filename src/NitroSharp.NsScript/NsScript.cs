using System.Collections.Generic;
using System.IO;

namespace NitroSharp.NsScript
{
    public static class NsScript
    {
        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            var lexer = new NsScriptLexer(text);
            SyntaxToken token = null;
            while (token?.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                token = lexer.Lex();
                yield return token;
            }
        }

        public static NsSyntaxTree ParseScript(string text)
        {
            var parser = new NsScriptParser(new NsScriptLexer(text));
            return parser.ParseScript();
        }

        public static NsSyntaxTree ParseScript(string fileName, Stream stream)
        {
            var parser = new NsScriptParser(new NsScriptLexer(fileName, stream));
            return parser.ParseScript();
        }

        public static Expression ParseExpression(string expression)
        {
            var parser = new NsScriptParser(new NsScriptLexer(expression));
            return parser.ParseExpression();
        }

        public static Statement ParseStatement(string statement)
        {
            var parser = new NsScriptParser(new NsScriptLexer(statement));
            return parser.ParseStatement();
        }
    }
}
