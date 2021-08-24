using NitroSharp.NsScript.Syntax;
using System.Collections.Generic;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class StatementParsingTests
    {
        [Fact]
        public void Block()
        {
            var block = AssertStatement<Block>("{}", SyntaxNodeKind.Block);
            Assert.Empty(block.Statements);
        }

        [Theory]
        [InlineData("if ($condition) {}", false)]
        [InlineData("if ($condition) {} else {}", true)]
        public void If(string text, bool hasElseClause)
        {
            var ifStmt = AssertStatement<IfStatement>(text, SyntaxNodeKind.IfStatement);
            Assert.NotNull(ifStmt.Condition);
            Assert.NotNull(ifStmt.IfTrueStatement);
            if (hasElseClause)
            {
                Assert.NotNull(ifStmt.IfFalseStatement);
            }
        }

        [Fact]
        public void BreakStatement()
        {
            AssertStatement<BreakStatement>("break;", SyntaxNodeKind.BreakStatement);
        }

        [Fact]
        public void While()
        {
            var whileStmt = AssertStatement<WhileStatement>("while (true) {}", SyntaxNodeKind.WhileStatement);
            Assert.NotNull(whileStmt.Condition);
            Assert.NotNull(whileStmt.Body);
        }

        [Fact]
        public void ReturnStatement()
        {
            AssertStatement<ReturnStatement>("return;", SyntaxNodeKind.ReturnStatement);
        }

        [Fact]
        public void Select()
        {
            string text = @"
                select
                {
                    case foo: {}
                }";

            var selectStmt = AssertStatement<SelectStatement>(text, SyntaxNodeKind.SelectStatement);
            var selectSection = Assert.IsType<SelectSection>(
                Assert.Single(selectStmt.Body.Statements));

            Common.AssertSpannedText(text, "foo", selectSection.Label);
            Assert.NotNull(selectSection.Body);
        }

        [Theory]
        [InlineData("foo.nss")]
        [InlineData("nss/foo.nss")]
        [InlineData("root/nss/foo.nss")]
        public void CallChapter(string filePath)
        {
            string text = $"call_chapter {filePath}";
            var callChapterStmt = AssertStatement<CallChapterStatement>(text, SyntaxNodeKind.CallChapterStatement);
            Common.AssertSpannedText(text, filePath, callChapterStmt.TargetModule);
        }

        [Theory]
        [InlineData("Scene", null, "Scene")]
        [InlineData("@->LocalScene", null, "LocalScene")]
        [InlineData("nss/foo.nss->Scene", "nss/foo.nss", "Scene")]
        public void CallSceneStatement_Parses_Correctly(string path, string file, string scene)
        {
            string text = $"call_scene {path}";
            var callSceneStmt = AssertStatement<CallSceneStatement>(text, SyntaxNodeKind.CallSceneStatement);
            if (!string.IsNullOrEmpty(file))
            {
                Assert.True(callSceneStmt.TargetModule.HasValue);
                Common.AssertSpannedText(text, file, callSceneStmt.TargetModule.Value);
            }

            Common.AssertSpannedText(text, scene, callSceneStmt.TargetScene);
        }

        [Theory]
        [MemberData(nameof(GetDialogueBlockTestData))]
        public void DialogueBlock(string text, string blockName, string boxName, int partCount)
        {
            var dialogueBlock = AssertStatement<DialogueBlock>(text, SyntaxNodeKind.DialogueBlock);
            Assert.Equal(blockName, dialogueBlock.Name);
            Assert.Equal(boxName, dialogueBlock.AssociatedBox);
            Assert.Equal(partCount, dialogueBlock.Parts.Length);
        }

        [Fact]
        public void Markup_RawString_WithDoubleSlash()
        {
            const string text = "function foo() { <PRE box01><pre>https://sonome.dareno.me</pre></PRE>\r\nfoo(); }";
            var func = (FunctionDeclaration)Parsing.ParseSubroutineDeclaration(text).Root;
            var stmts = func.Body.Statements;
            Assert.Equal(2, stmts.Length);
            var markup = Assert.IsType<MarkupNode>(Assert.IsType<DialogueBlock>(stmts[0]).Parts[0]);
            Assert.Equal("<pre>https://sonome.dareno.me</pre>", markup.Text);
            Assert.Equal(SyntaxNodeKind.ExpressionStatement, stmts[1].Kind);
        }

        public static IEnumerable<object[]> GetDialogueBlockTestData()
        {
            yield return new object[]
            {
                @"<PRE @box01>
                [text001]
                </PRE>",
                "text001",
                "@box01",
                0
            };

            yield return new object[]
            {
                @"<PRE box01>
                [text001]
                </PRE>",
                "text001",
                "box01",
                0
            };

            yield return new object[]
            {
                @"<PRE @box01>
                [text001]
                {}
                </PRE>",
                "text001",
                "@box01",
                1
            };
        }

        private static T AssertStatement<T>(string text, SyntaxNodeKind expectedKind) where T : Statement
        {
            var result = Assert.IsType<T>(Parsing.ParseStatement(text).Root);
            Assert.Equal(expectedKind, result.Kind);
            return result;
        }
    }
}
