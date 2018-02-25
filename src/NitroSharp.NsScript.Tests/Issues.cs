using NitroSharp.NsScript.Syntax;
using System.Linq;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class Issues
    {
        [Fact]
        public void ParseCommaDotSeparatedArgumentList()
        {
            string text = "FadeDelete(\"痛い\", 150,. true);";
            var expr = Parsing.ParseExpression(text);
        }

        [Fact]
        public void ParseSemicolonTerminatedIncludeDirective()
        {
            string text = "#include \"foo.nss\";";
            var sourceFile = Parsing.ParseText(text).Root as SourceFile;
            Assert.NotNull(sourceFile);
            var fileRef = sourceFile.FileReferences.SingleOrDefault();
            Assert.Equal("foo.nss", fileRef);
        }

        [Fact]
        public void LexProblematicCode_ch01_003()
        {
            string text = @"<PRE @box01>
[text029]
グリム：おいおいｗｗｗ
</PRE>";

            var tokens = Parsing.ParseTokens(text).ToArray();
            Assert.Equal(SyntaxTokenKind.DialogueBlockStartTag, tokens[0].Kind);
            Assert.Equal(SyntaxTokenKind.DialogueBlockIdentifier, tokens[1].Kind);
            Assert.Equal(SyntaxTokenKind.PXmlString, tokens[2].Kind);
            Assert.Equal(SyntaxTokenKind.DialogueBlockEndTag, tokens[3].Kind);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, tokens[4].Kind);
        }

        [Fact]
        public void LexProblamaticLine_sys_backlog()
        {
            string text = "$Revision: 10 $";
            var tokens = Parsing.ParseTokens(text).ToArray();
            Assert.Equal(SyntaxTokenKind.IdentifierToken, tokens[0].Kind);
            Assert.Equal(SyntaxTokenKind.ColonToken, tokens[1].Kind);
            Assert.Equal(SyntaxTokenKind.NumericLiteralToken, tokens[2].Kind);
            Assert.Equal(SyntaxTokenKind.DollarToken, tokens[3].Kind);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, tokens[4].Kind);
        }

        [Fact]
        public void ParseProblematicIfStatement()
        {
            string text = "if(!EnableBacklog()||!$SYSTEM_backlog_enable)){break;}";
            var stmt = Parsing.ParseStatement(text);
        }

        [Fact]
        public void LexLongAssMultilineComment()
        {
            string text = @"/*
	$PreConfigScrollbar = Integer((ImageVertical(""Config1f_ConfigGround"")-720) * ScrollbarValue(""Config9f_Scrollbar""));

				//★全体スクロール
				$ConfigScrollbar = Integer((ImageVertical(""Config1f_ConfigGround"")-720) * ScrollbarValue(""Config9f_Scrollbar""));
				$ConfigScrollbarY = $PreConfigScrollbar - $ConfigScrollbar;
				if($PreConfigScrollbar!=$ConfigScrollbar){
					Move(""Config0*/*/*"", 0, @0, @$ConfigScrollbarY, null, false);
					Move(""Config1*/*/*"", 0, @0, @$ConfigScrollbarY, null, false);
					Move(""Config0*"", 0, @0, @$ConfigScrollbarY, null, false);
					Move(""Config1*"", 0, @0, @$ConfigScrollbarY, null, false);
				}
				$PreConfigScrollbar = $ConfigScrollbar;

				case Config9f_Scrollbar{}
*/";

            var token = Parsing.ParseTokens(text).First();
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void ParseIfStatementWithExtraCloseParen()
        {
            string text = "if($condition)){}";
            var stmt = Parsing.ParseStatement(text).Root as IfStatement;
            Assert.NotNull(stmt);
            Assert.NotNull(stmt);
        }

        [Fact]
        public void LexPXmlElementInNormalMode()
        {
            string text = @"<voice name=""優愛"" class=""優愛"" src=""ch02/03700050yu"">

	CreateColor";
            var tokens = Parsing.ParseTokens(text).ToArray();
        }
    }
}
