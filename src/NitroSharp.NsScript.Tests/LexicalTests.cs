using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public sealed class LexicalTests
    {
        [Fact]
        public void LexEmptyString()
        {
            var token = LexToken(string.Empty);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void LexStringLiteral()
        {
            string text = "\"literal\"";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.StringLiteralToken, token.Kind);
            Assert.Equal(text, token.Text);
            Assert.Equal("literal", token.Value);
        }

        [Fact]
        public void LexUppercaseNullKeyword()
        {
            LexNullKeyword("NULL");
        }

        [Fact]
        public void LexQuotedNullKeyword()
        {
            LexNullKeyword("\"null\"");
        }

        private void LexNullKeyword(string text)
        {
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.NullKeyword, token.Kind);
            Assert.Null(token.Value);
        }

        [Fact]
        public void LexTrueKeyword()
        {
            var token = LexToken("true");

            Assert.Equal(SyntaxTokenKind.TrueKeyword, token.Kind);
            Assert.True((bool)token.Value);
        }

        [Fact]
        public void LexFalseKeyword()
        {
            var token = LexToken("false");

            Assert.Equal(SyntaxTokenKind.FalseKeyword, token.Kind);
            Assert.False((bool)token.Value);
        }

        [Fact]
        public void LexNumericLiteral()
        {
            string text = "42";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.NumericLiteralToken, token.Kind);
            Assert.Equal(42.0d, token.Value);
        }

        [Fact]
        public void LexFloatNumericLiteral()
        {
            string text = "4.2";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.NumericLiteralToken, token.Kind);
            Assert.Equal(4.2d, token.Value);
        }

        [Fact]
        public void LexSingleLetterIdentifier()
        {
            string text = "a";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexDollarPrefixedIdentifier()
        {
            string text = "$globalVar";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexHashPrefixedIdentifier()
        {
            string text = "#flag";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexArrowPrefixedIdentifier()
        {
            string text = "@->test";
            var token = LexToken(text);
            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexJapaneseIdentifier()
        {
            string identifier = "ev100_06_1_６人祈る_a";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

        [Fact]
        public void LexIdentifierWithSlash()
        {
            string identifier = "nss/sys_load.nss";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

        [Fact]
        public void LexIdentifieWithDot()
        {
            string text = "foo.bar";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexIdentifierInQuotes()
        {
            string identifier = "\"$test\"";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

        // TODO: figure out why it's commented out.
        //[Fact]
        //public void LexIdentifierStartingWithDigit()
        //{
        //    string identifier = "7_hoppies";
        //    var token = LexToken(identifier);

        //    Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
        //    Assert.Equal(identifier, token.Text);
        //}

        [Fact]
        public void LexSingleLineComment()
        {
            string comment = "// this is a comment.";
            var token = LexToken(comment);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void LexMultiLineComment()
        {
            string comment = @"/*
				初回起動時ではないときは、プレイ速度をバックアップ
			*/";
            var token = LexToken(comment);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void LexParagraphStartTag()
        {
            string text = "<PRE box69>";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.DialogueBlockStartTag, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexParagraphEndTag()
        {
            string text = "</PRE>";
            var token = LexToken(text, LexingMode.DialogueBlock);

            Assert.Equal(SyntaxTokenKind.DialogueBlockEndTag, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexSimplePXmlString()
        {
            string text = "sample text";
            var token = LexToken(text, LexingMode.DialogueBlock);

            Assert.Equal(SyntaxTokenKind.PXmlString, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexPXmlLineSeparator()
        {
            string text = "\r\n";
            var token = LexToken(text, LexingMode.DialogueBlock);

            Assert.Equal(SyntaxTokenKind.PXmlLineSeparator, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void LexPXmlStringWithVerbatimText()
        {
            string text = "<PRE>scene</PRE>";
            var token = LexToken(text, LexingMode.DialogueBlock);

            Assert.Equal(SyntaxTokenKind.PXmlString, token.Kind);
            Assert.Equal(text, token.Text);
        }

        private SyntaxToken LexToken(string text, LexingMode mode = LexingMode.Normal)
        {
            SyntaxToken result = null;
            foreach (var token in Parsing.ParseTokens(text, mode))
            {
                if (result == null)
                {
                    result = token;
                }
                else if (token.Kind != SyntaxTokenKind.EndOfFileToken)
                {
                    Assert.True(false, "More than one token was lexed.");
                }
            }

            if (result == null)
            {
                Assert.True(false, "No tokens were lexed.");
            }

            return result;
        }
    }
}
