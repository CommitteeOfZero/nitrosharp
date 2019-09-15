using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Text;
using System.Collections.Generic;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    public class StatementParsingTests
    {
        [Fact]
        public void BlockSyntax_Parses_Correctly()
        {
            var block = AssertStatement<BlockSyntax>("{}", SyntaxNodeKind.Block);
            Assert.Empty(block.Statements);
        }

        [Theory]
        [InlineData("if ($condition) {}", false)]
        [InlineData("if ($condition) {} else {}", true)]
        public void IfStatement_Parses_Correctly(string text, bool hasElseClause)
        {
            var ifStmt = AssertStatement<IfStatementSyntax>(text, SyntaxNodeKind.IfStatement);
            Assert.NotNull(ifStmt.Condition);
            Assert.NotNull(ifStmt.IfTrueStatement);
            if (hasElseClause)
            {
                Assert.NotNull(ifStmt.IfFalseStatement);
            }
        }

        [Fact]
        public void BreakStatement_Parses_Correctly()
        {
            var breakStmt = AssertStatement<BreakStatementSyntax>("break;", SyntaxNodeKind.BreakStatement);
        }

        [Fact]
        public void WhileStatement_Parses_Correctly()
        {
            var whileStmt = AssertStatement<WhileStatementSyntax>("while (true) {}", SyntaxNodeKind.WhileStatement);
            Assert.NotNull(whileStmt.Condition);
            Assert.NotNull(whileStmt.Body);
        }

        [Fact]
        public void ReturnStatement_Parses_Correctly()
        {
            AssertStatement<ReturnStatementSyntax>("return;", SyntaxNodeKind.ReturnStatement);
        }

        [Fact]
        public void SelectStatement_Parses_Correctly()
        {
            string text = @"
                select
                {
                    case foo: {}
                }";

            var selectStmt = AssertStatement<SelectStatementSyntax>(text, SyntaxNodeKind.SelectStatement);
            var selectSection = Assert.IsType<SelectSectionSyntax>(
                Assert.Single(selectStmt.Body.Statements));

            Common.AssertSpannedText(text, "foo", selectSection.Label);
            Assert.NotNull(selectSection.Body);
        }

        [Theory]
        [InlineData("foo.nss")]
        [InlineData("nss/foo.nss")]
        [InlineData("root/nss/foo.nss")]
        public void CallChapterStatement_Parses_Correctly(string filePath)
        {
            string text = $"call_chapter {filePath}";
            var callChapterStmt = AssertStatement<CallChapterStatementSyntax>(text, SyntaxNodeKind.CallChapterStatement);
            Common.AssertSpannedText(text, filePath, callChapterStmt.TargetModule);
        }

        [Theory]
        [InlineData("Scene", null, "Scene")]
        [InlineData("@->LocalScene", null, "LocalScene")]
        [InlineData("nss/foo.nss->Scene", "nss/foo.nss", "Scene")]
        public void CallSceneStatement_Parses_Correctly(string path, string file, string scene)
        {
            string text = $"call_scene {path}";
            var callSceneStmt = AssertStatement<CallSceneStatementSyntax>(text, SyntaxNodeKind.CallSceneStatement);
            if (!string.IsNullOrEmpty(file))
            {
                Assert.True(callSceneStmt.TargetModule.HasValue);
                Common.AssertSpannedText(text, file, callSceneStmt.TargetModule.Value);
            }

            Common.AssertSpannedText(text, scene, callSceneStmt.TargetScene);
        }

        [Theory]
        [MemberData(nameof(GetDialogueBlockTestData))]
        public void DialogueBlock_Parses_Correctly(string text, string blockName, string boxName, int partCount)
        {
            var dialogueBlock = AssertStatement<DialogueBlockSyntax>(text, SyntaxNodeKind.DialogueBlock);
            Assert.Equal(blockName, dialogueBlock.Name);
            Assert.Equal(boxName, dialogueBlock.AssociatedBox);
            Assert.Equal(partCount, dialogueBlock.Parts.Length);
        }

        public static IEnumerable<object[]> GetDialogueBlockTestData()
        {
            yield return new object[]
            {
                @"<PRE @box01>
                [text001]
                </PRE>",
                "text001",
                "box01",
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
                "box01",
                1
            };
        }

        private static T AssertStatement<T>(string text, SyntaxNodeKind expectedKind) where T : StatementSyntax
        {
            var result = Assert.IsType<T>(Parsing.ParseStatement(text).Root);
            Assert.Equal(expectedKind, result.Kind);
            return result;
        }
    }
}
