using System.Linq;
using Xunit;

namespace CommitteeOfZero.NsScript.Tests
{
    public class StatementParsingTests
    {
        [Fact]
        public void ParseChapterDefinition()
        {
            string text = "chapter main{}";
            var root = NsScript.ParseScript(text);
            var chapter = root.MainChapter;

            Assert.Equal(SyntaxNodeKind.Chapter, chapter.Kind);
            Assert.Equal("main", chapter.Name.FullName);

            string toStringResult = Helpers.RemoveNewLineCharacters(chapter.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseSceneDefinition()
        {
            string text = "scene TestScene{}";
            var root = NsScript.ParseScript(text);
            var scene = root.Scenes.SingleOrDefault();

            Assert.NotNull(scene);
            Assert.Equal(SyntaxNodeKind.Scene, scene.Kind);
            Assert.Equal("TestScene", scene.Name.FullName);
        }

        [Fact]
        public void ParseFunctionDefinition()
        {
            string text = "function Test(){}";
            var root = NsScript.ParseScript(text);
            var functionDef = root.Functions[0];

            Assert.Equal(SyntaxNodeKind.Function, functionDef.Kind);
            Assert.Equal("Test", functionDef.Name.FullName);
            Assert.Equal(0, functionDef.Parameters.Length);

            string toStringResult = Helpers.RemoveNewLineCharacters(functionDef.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseFunctionDefinitionWithIntParameter()
        {
            string text = "function Test(intParam){}";
            var root = NsScript.ParseScript(text);
            var method = root.Functions[0];

            Assert.Equal(SyntaxNodeKind.Function, method.Kind);
            Assert.Equal("Test", method.Name.FullName);

            Assert.Equal(1, method.Parameters.Length);
            var p = method.Parameters[0];
            Assert.Equal(SyntaxNodeKind.Parameter, p.Kind);
            Assert.Equal("intParam", p.ParameterName.FullName);
            Assert.Equal(p.ParameterName.FullName, p.ParameterName.SimplifiedName);
            Assert.Equal(SigilKind.None, p.ParameterName.Sigil);

            string toStringResult = Helpers.RemoveNewLineCharacters(method.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseFunctionDefinitionWithStringParameter()
        {
            string text = "function Test(\"stringParam\"){}";
            var root = NsScript.ParseScript(text);
            var method = root.Functions[0];

            Assert.Equal(SyntaxNodeKind.Function, method.Kind);
            Assert.Equal("Test", method.Name.FullName);

            Assert.Equal(1, method.Parameters.Length);
            var p = method.Parameters[0];
            Assert.Equal(SyntaxNodeKind.Parameter, p.Kind);
            Assert.Equal("\"stringParam\"", p.ParameterName.FullName);
            Assert.Equal("stringParam", p.ParameterName.SimplifiedName);
            Assert.Equal(SigilKind.None, p.ParameterName.Sigil);

            string toStringResult = Helpers.RemoveNewLineCharacters(method.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseFunctionDefinitionWithStringParameterWithSigil()
        {
            TestFunctionWithStringParameterWithSigilImpl("\"$stringParam\"", "stringParam", SigilKind.Dollar);
            TestFunctionWithStringParameterWithSigilImpl("\"#stringParam\"", "stringParam", SigilKind.Hash);
        }

        private void TestFunctionWithStringParameterWithSigilImpl(string fullName, string simplifiedName, SigilKind sigil)
        {
            string text = $"function Test({fullName}){{}}";
            var root = NsScript.ParseScript(text);
            var method = root.Functions[0];

            Assert.Equal(SyntaxNodeKind.Function, method.Kind);
            Assert.Equal("Test", method.Name.FullName);

            Assert.Equal(1, method.Parameters.Length);
            var p = method.Parameters[0];
            Assert.Equal(SyntaxNodeKind.Parameter, p.Kind);
            Assert.Equal(fullName, p.ParameterName.FullName);
            Assert.Equal(simplifiedName, p.ParameterName.SimplifiedName);
            Assert.Equal(sigil, p.ParameterName.Sigil);

            string toStringResult = Helpers.RemoveNewLineCharacters(method.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseIfStatement()
        {
            string text = "if (#flag == true){}";
            var ifStatement = NsScript.ParseStatement(text) as IfStatement;
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
            var ifStatement = NsScript.ParseStatement(text) as IfStatement;
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
            var statement = NsScript.ParseStatement(text) as BreakStatement;
            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.BreakStatement, statement.Kind);

            string toStringResult = Helpers.RemoveNewLineCharacters(statement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseWhileStatement()
        {
            string text = "while (true){}";
            var whileStatement = NsScript.ParseStatement(text) as WhileStatement;
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

            var selectStatement = NsScript.ParseStatement(text) as SelectStatement;
            Assert.NotNull(selectStatement);
            Assert.Equal(SyntaxNodeKind.SelectStatement, selectStatement.Kind);

            var body = selectStatement.Body;
            Assert.Equal(1, body.Statements.Length);
            var firstSection = body.Statements[0] as SelectSection;
            Assert.NotNull(firstSection);
            Assert.Equal(SyntaxNodeKind.SelectSection, firstSection.Kind);
            Assert.Equal("option", firstSection.Label.FullName);
            Assert.NotNull(firstSection.Body);
        }

        [Fact]
        public void ParseReturnStatement()
        {
            string text = "return;";
            var statement = NsScript.ParseStatement(text) as ReturnStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.ReturnStatement, statement.Kind);
        }

        [Fact]
        public void ParseCallChapterStatement()
        {
            string text = "call_chapter @->testchapter;";
            var statement = NsScript.ParseStatement(text) as CallChapterStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallChapterStatement, statement.Kind);
            Assert.Equal("@->testchapter", statement.ChapterName.FullName);
        }

        [Fact]
        public void ParseCallSceneStatement()
        {
            string text = "call_scene @->testscene;";
            var statement = NsScript.ParseStatement(text) as CallSceneStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallSceneStatement, statement.Kind);
            Assert.Equal("@->testscene", statement.SceneName.FullName);
        }
    }
}
