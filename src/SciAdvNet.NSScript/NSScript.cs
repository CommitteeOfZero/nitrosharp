using System.Collections.Generic;
using System.IO;

namespace SciAdvNet.NSScript
{
    public static class NSScript
    {
        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            var lexer = new NSScriptLexer(text);
            SyntaxToken token = null;
            while (token?.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                token = lexer.Lex();
                yield return token;
            }
        }

        public static NSSyntaxTree ParseScript(string text)
        {
            var parser = new NSScriptParser(new NSScriptLexer(text));
            return parser.ParseScript();
        }

        public static NSSyntaxTree ParseScript(string fileName, Stream stream)
        {
            var parser = new NSScriptParser(new NSScriptLexer(fileName, stream));
            return parser.ParseScript();
        }

        public static Expression ParseExpression(string expression)
        {
            var parser = new NSScriptParser(new NSScriptLexer(expression));
            return parser.ParseExpression();
        }

        public static Statement ParseStatement(string statement)
        {
            var parser = new NSScriptParser(new NSScriptLexer(statement));
            return parser.ParseStatement();
        }

        //public static DialogueBlock ParseDialogueBlock(string text)
        //{
        //    var parser = new NSScriptParser(new NSScriptLexer(text));
        //    return parser.ParseDialogueBlock();
        //}
    }
}
