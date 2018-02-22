using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class StatementParsingTests
    {
        [Fact]
        public void ParseFunctionCall_NoParentheses_NoArguments()
        {
            string text = "WaitKey;";
            var stmt = Parsing.ParseStatement(text).Root as ExpressionStatement;
            var call = stmt.Expression as FunctionCall;
            Assert.NotNull(call);
            Assert.Equal(SyntaxNodeKind.FunctionCall, call.Kind);
            Assert.Equal("WaitKey", call.Target.Name);
            Assert.Empty(call.Arguments);

            Assert.Equal("WaitKey()", call.ToString());
        }

        [Fact]
        public void ParseFunctionCall_NoParentheses_WithArguments()
        {
            string text = "WaitKey 2000, true;";
            var stmt = Parsing.ParseStatement(text).Root as ExpressionStatement;
            var call = stmt.Expression as FunctionCall;
            Assert.NotNull(call);
            Assert.Equal(SyntaxNodeKind.FunctionCall, call.Kind);
            Assert.Equal("WaitKey", call.Target.Name);
            Assert.Equal(2, call.Arguments.Length);

            Assert.Equal("WaitKey(2000, true)", call.ToString());
        }

        [Fact]
        public void ParseFunctionCall_NameInQuotes_NoParentheses()
        {
            string text = "\"WaitKey\";";
            var stmt = Parsing.ParseStatement(text).Root as ExpressionStatement;
            var call = stmt.Expression as FunctionCall;
            Assert.NotNull(call);
            Assert.Equal(SyntaxNodeKind.FunctionCall, call.Kind);
            Assert.Equal("WaitKey", call.Target.Name);
            Assert.Empty(call.Arguments);

            Assert.Equal("WaitKey()", call.ToString());
        }

        [Fact]
        public void ParseIfStatement()
        {
            string text = "if (#flag == true){}";
            var ifStatement = Parsing.ParseStatement(text).Root as IfStatement;
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
            var ifStatement = Parsing.ParseStatement(text).Root as IfStatement;
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
            var statement = Parsing.ParseStatement(text).Root as BreakStatement;
            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.BreakStatement, statement.Kind);

            string toStringResult = Helpers.RemoveNewLineCharacters(statement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseWhileStatement()
        {
            string text = "while (true){}";
            var whileStatement = Parsing.ParseStatement(text).Root as WhileStatement;
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

            var selectStatement = Parsing.ParseStatement(text).Root as SelectStatement;
            Assert.NotNull(selectStatement);
            Assert.Equal(SyntaxNodeKind.SelectStatement, selectStatement.Kind);

            var body = selectStatement.Body;
            Assert.Single(body.Statements);
            var firstSection = body.Statements[0] as SelectSection;
            Assert.NotNull(firstSection);
            Assert.Equal(SyntaxNodeKind.SelectSection, firstSection.Kind);
            Assert.Equal("option", firstSection.Label.Name);
            Assert.NotNull(firstSection.Body);
        }

        [Fact]
        public void ParseSelectSectionWithSlashesInName()
        {
            string text = @"
select
{
case goo/foo/bar:{}
}";
            var selectStatement = Parsing.ParseStatement(text).Root as SelectStatement;
            Assert.NotNull(selectStatement);
            var section = selectStatement.Body.Statements[0] as SelectSection;
            Assert.NotNull(section);
            Assert.Equal("goo/foo/bar", section.Label.Name);
        }

        [Fact]
        public void ParseReturnStatement()
        {
            string text = "return;";
            var statement = Parsing.ParseStatement(text).Root as ReturnStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.ReturnStatement, statement.Kind);
        }

        [Fact]
        public void ParseCallChapterStatement()
        {
            string text = "call_chapter nss/foo.nss;";
            var statement = Parsing.ParseStatement(text).Root as CallChapterStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallChapterStatement, statement.Kind);
            Assert.Equal("nss/foo.nss", statement.Target);
        }

        [Fact]
        public void ParseCallSceneStatement()
        {
            string text = "call_scene @->foo;";
            var statement = Parsing.ParseStatement(text).Root as CallSceneStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallSceneStatement, statement.Kind);
            Assert.Equal("foo", statement.SceneName);
        }

        [Fact]
        public void ParseCallSceneStatementWithFilePath()
        {
            string text = "call_scene nss/foo.nss->bar;";
            var statement = Parsing.ParseStatement(text).Root as CallSceneStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallSceneStatement, statement.Kind);
            Assert.Equal("nss/foo.nss", statement.TargetFile);
            Assert.Equal("bar", statement.SceneName);
        }
    }
}
