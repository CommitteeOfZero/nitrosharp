using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class StatementParsingTests
    {
        [Fact]
        public void ParseIfStatement()
        {
            string text = "if (#flag == true){}";
            var ifStatement = Parsing.ParseStatement(text) as IfStatement;
            Assert.NotNull(ifStatement);
            Assert.Equal(SyntaxNodeKind.IfStatement, ifStatement.Kind);
            Assert.NotNull(ifStatement.Condition);
            Assert.NotNull(ifStatement.IfTrueStatement);
            Assert.Null(ifStatement.IfFalseStatement);

            string toStringResult = Helpers.RemoveNewLineCharacters(ifStatement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseIfStatementWithElseClause()
        {
            string text = "if (#flag == true){}else{}";
            var ifStatement = Parsing.ParseStatement(text) as IfStatement;
            Assert.NotNull(ifStatement);
            Assert.Equal(SyntaxNodeKind.IfStatement, ifStatement.Kind);
            Assert.NotNull(ifStatement.Condition);
            Assert.NotNull(ifStatement.IfTrueStatement);
            Assert.NotNull(ifStatement.IfFalseStatement);

            string toStringResult = Helpers.RemoveNewLineCharacters(ifStatement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseBreakStatement()
        {
            string text = "break;";
            var statement = Parsing.ParseStatement(text) as BreakStatement;
            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.BreakStatement, statement.Kind);

            string toStringResult = Helpers.RemoveNewLineCharacters(statement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseWhileStatement()
        {
            string text = "while (true){}";
            var whileStatement = Parsing.ParseStatement(text) as WhileStatement;
            Assert.NotNull(whileStatement);
            Assert.Equal(SyntaxNodeKind.WhileStatement, whileStatement.Kind);
            Assert.NotNull(whileStatement.Condition);
            Assert.NotNull(whileStatement.Body);

            string toStringResult = Helpers.RemoveNewLineCharacters(whileStatement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseSelectStatement()
        {
            string text = @"
select
{
case option:{}
}";

            var selectStatement = Parsing.ParseStatement(text) as SelectStatement;
            Assert.NotNull(selectStatement);
            Assert.Equal(SyntaxNodeKind.SelectStatement, selectStatement.Kind);

            var body = selectStatement.Body;
            Assert.Single(body.Statements);
            var firstSection = body.Statements[0] as SelectSection;
            Assert.NotNull(firstSection);
            Assert.Equal(SyntaxNodeKind.SelectSection, firstSection.Kind);
            Assert.Equal("option", firstSection.Label.OriginalName);
            Assert.NotNull(firstSection.Body);
        }

        [Fact]
        public void ParseReturnStatement()
        {
            string text = "return;";
            var statement = Parsing.ParseStatement(text) as ReturnStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.ReturnStatement, statement.Kind);
        }

        [Fact]
        public void ParseCallChapterStatement()
        {
            string text = "call_chapter @->testchapter;";
            var statement = Parsing.ParseStatement(text) as CallChapterStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallChapterStatement, statement.Kind);
            Assert.Equal("@->testchapter", statement.Module.OriginalName);
        }

        [Fact]
        public void ParseCallSceneStatement()
        {
            string text = "call_scene @->testscene;";
            var statement = Parsing.ParseStatement(text) as CallSceneStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallSceneStatement, statement.Kind);
            Assert.Equal("@->testscene", statement.SceneName.OriginalName);
        }
    }
}
