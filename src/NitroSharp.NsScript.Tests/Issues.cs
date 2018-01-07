using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class Issues
    {
        [Fact]
        public void ParseProblematicLineFromKarteScript()
        {
            string text = "$カルテ位置 = Integer($カルテ縦幅 * ScrollbarValue(\"@カルテスクロール\"));";
            var stmt = Parsing.ParseStatement(text);
        }

        [Fact]
        public void ParseCommaDotSeparatedArgumentList()
        {
            string text = "FadeDelete(\"痛い\", 150,. true);";
            var expr = Parsing.ParseExpression(text);
        }

        [Fact]
        public void ParseProblematicAssignmentFromSystem()
        {
            string text = "#play_speed_plus=#SYSTEM_play_speed;";
            var exprStatement = Parsing.ParseStatement(text) as ExpressionStatement;

            Assert.NotNull(exprStatement);
            var expr = exprStatement.Expression as AssignmentExpression;
            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.Identifier, expr.Value.Kind);
        }
    }
}
