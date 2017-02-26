using System.Linq;
using Xunit;

namespace SciAdvNet.NSScript.Tests
{
    public class StatementParsingTests
    {
        [Fact]
        public void TestChapter()
        {
            string text = "chapter main{}";
            var root = NSScript.ParseScript(text);
            var chapter = root.MainChapter;

            Assert.Equal(SyntaxNodeKind.Chapter, chapter.Kind);
            Assert.Equal("main", chapter.Name.FullName);

            string toStringResult = Helpers.RemoveNewLineCharacters(chapter.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void TestScene()
        {
            string text = "scene TestScene{}";
            var root = NSScript.ParseScript(text);
            var scene = root.Scenes.SingleOrDefault();

            Assert.NotNull(scene);
            Assert.Equal(SyntaxNodeKind.Scene, scene.Kind);
            Assert.Equal("TestScene", scene.Name.FullName);
        }

        [Fact]
        public void TestMethod()
        {
            string text = "function Test(){}";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            Assert.Equal(SyntaxNodeKind.Method, method.Kind);
            Assert.Equal("Test", method.Name.FullName);
            Assert.Equal(0, method.Parameters.Length);

            string toStringResult = Helpers.RemoveNewLineCharacters(method.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void TestMethodCall()
        {
            string text = "WaitKey(10000);";
            var call = NSScript.ParseStatement(text) as MethodCall;
            Assert.NotNull(call);
            Assert.Equal(SyntaxNodeKind.MethodCall, call.Kind);
            Assert.Equal("WaitKey", call.TargetMethodName.FullName);
            Assert.Equal(call.TargetMethodName.FullName, call.TargetMethodName.SimplifiedName);
            Assert.Equal(SigilKind.None, call.TargetMethodName.Sigil);
            Assert.Equal(1, call.Arguments.Length);

            Assert.Equal(text, call.ToString());
        }

        [Fact]
        public void TestMethodWithIntParameter()
        {
            string text = "function Test(intParam){}";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            Assert.Equal(SyntaxNodeKind.Method, method.Kind);
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
        public void TestMethodWithStringParameter()
        {
            string text = "function Test(\"stringParam\"){}";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            Assert.Equal(SyntaxNodeKind.Method, method.Kind);
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
        public void TestMethodWithStringParameterWithSigil()
        {
            TestMethodWithStringParameterWithSigilImpl("\"$stringParam\"", "stringParam", SigilKind.Dollar);
            TestMethodWithStringParameterWithSigilImpl("\"#stringParam\"", "stringParam", SigilKind.Hash);
        }

        private void TestMethodWithStringParameterWithSigilImpl(string fullName, string simplifiedName, SigilKind sigil)
        {
            string text = $"function Test({fullName}){{}}";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            Assert.Equal(SyntaxNodeKind.Method, method.Kind);
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
        public void TestIfStatement()
        {
            string text = "if (#flag == true){}";
            var ifStatement = NSScript.ParseStatement(text) as IfStatement;
            Assert.NotNull(ifStatement);
            Assert.Equal(SyntaxNodeKind.IfStatement, ifStatement.Kind);
            Assert.NotNull(ifStatement.Condition);
            Assert.NotNull(ifStatement.IfTrueStatement);
            Assert.Null(ifStatement.IfFalseStatement);

            string toStringResult = Helpers.RemoveNewLineCharacters(ifStatement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void TestBreakStatement()
        {
            string text = "break;";
            var statement = NSScript.ParseStatement(text) as BreakStatement;
            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.BreakStatement, statement.Kind);

            string toStringResult = Helpers.RemoveNewLineCharacters(statement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void TestIfStatementWithElseClause()
        {
            string text = "if (#flag == true){}else{}";
            var ifStatement = NSScript.ParseStatement(text) as IfStatement;
            Assert.NotNull(ifStatement);
            Assert.Equal(SyntaxNodeKind.IfStatement, ifStatement.Kind);
            Assert.NotNull(ifStatement.Condition);
            Assert.NotNull(ifStatement.IfTrueStatement);
            Assert.NotNull(ifStatement.IfFalseStatement);

            string toStringResult = Helpers.RemoveNewLineCharacters(ifStatement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void TestWhileStatement()
        {
            string text = "while (true){}";
            var whileStatement = NSScript.ParseStatement(text) as WhileStatement;
            Assert.NotNull(whileStatement);
            Assert.Equal(SyntaxNodeKind.WhileStatement, whileStatement.Kind);
            Assert.NotNull(whileStatement.Condition);
            Assert.NotNull(whileStatement.Body);

            string toStringResult = Helpers.RemoveNewLineCharacters(whileStatement.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void TestSelectStatement()
        {
            string text = @"
select
{
case option1:{}
case option2:{}
case option3:{}
}";

            var selectStatement = NSScript.ParseStatement(text) as SelectStatement;
            Assert.NotNull(selectStatement);
            Assert.Equal(SyntaxNodeKind.SelectStatement, selectStatement.Kind);

            var body = selectStatement.Body;
            Assert.Equal(3, body.Statements.Length);
            var firstSection = body.Statements[0] as SelectSection;
            Assert.NotNull(firstSection);
            Assert.Equal(SyntaxNodeKind.SelectSection, firstSection.Kind);
            Assert.Equal("option1", firstSection.Label.FullName);
            Assert.NotNull(firstSection.Body);
        }

        [Fact]
        public void TestReturnStatement()
        {
            string text = "return;";
            var statement = NSScript.ParseStatement(text) as ReturnStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.ReturnStatement, statement.Kind);
        }

        [Fact]
        public void TestCallChapterStatement()
        {
            string text = "call_chapter @->testchapter;";
            var statement = NSScript.ParseStatement(text) as CallChapterStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallChapterStatement, statement.Kind);
            Assert.Equal("@->testchapter", statement.ChapterName.FullName);
        }

        [Fact]
        public void TestCallSceneStatement()
        {
            string text = "call_scene @->testscene;";
            var statement = NSScript.ParseStatement(text) as CallSceneStatement;

            Assert.NotNull(statement);
            Assert.Equal(SyntaxNodeKind.CallSceneStatement, statement.Kind);
            Assert.Equal("@->testscene", statement.SceneName.FullName);
        }
    }
}
