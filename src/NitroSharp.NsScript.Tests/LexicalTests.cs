using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Text;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public sealed class LexicalTests
    {
        [Fact]
        public void LexEmptyString()
        {
            AssertValidToken(string.Empty, SyntaxTokenKind.EndOfFileToken);
        }

        [Fact]
        public void LexStringLiteral()
        {
            AssertValidToken("\"literal\"", SyntaxTokenKind.StringLiteralToken, "literal");
        }

        [Fact]
        public void LexUnterminatedStringLiteral()
        {
            string text = "\"foo";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.StringLiteralToken, token.Kind);
            Assert.True(token.HasErrors);
            Assert.Equal(DiagnosticId.UnterminatedString, token.SyntaxError.Id);
        }

        [Fact]
        public void LextNullKeyword()
        {
            AssertValidToken("null", SyntaxTokenKind.NullKeyword, null);
        }

        [Fact]
        public void LexUppercaseNullKeyword()
        {
            AssertValidToken("NULL", SyntaxTokenKind.NullKeyword, "null", null);
        }

        [Fact]
        public void LexQuotedNullKeyword()
        {
            AssertValidToken("\"null\"", SyntaxTokenKind.NullKeyword, "null", null);
        }

        [Fact]
        public void LexTrueKeyword()
        {
            AssertValidToken("true", SyntaxTokenKind.TrueKeyword, true);
        }

        [Fact]
        public void LexFalseKeyword()
        {
            AssertValidToken("false", SyntaxTokenKind.FalseKeyword, false);
        }

        [Fact]
        public void LexNumericLiteral()
        {
            AssertValidToken("42", SyntaxTokenKind.NumericLiteralToken, 42.0d);
        }

        [Fact]
        public void LexFloatNumericLiteral()
        {
            AssertValidToken("4.2", SyntaxTokenKind.NumericLiteralToken, 4.2d);
        }

        [Fact]
        public void LexHexTriplet()
        {
            AssertValidToken("#FFFFFF", SyntaxTokenKind.NumericLiteralToken, (double)0xffffff);
        }

        [Fact]
        public void LexIdentifierThatStartsLikeHexTriplet()
        {
            AssertValidIdentifier("#ABCDEFghijklmno");
        }

        [Fact]
        public void LexSingleLetterIdentifier()
        {
            AssertValidIdentifier("a");
        }

        [Fact]
        public void LexDollarPrefixedIdentifier()
        {
            AssertValidIdentifier("$globalVar");
        }

        [Fact]
        public void LexHashPrefixedIdentifier()
        {
            AssertValidIdentifier("#flag");
        }

        [Fact]
        public void LexIdentifierStartingWithDigit()
        {
            AssertValidIdentifier("42_foo");
        }

        [Fact]
        public void LexIdentifierWithJapaneseCharacters()
        {
            AssertValidIdentifier("ev100_06_1_６人祈る_a");
        }

        [Fact]
        public void LexIdentifierWithDot()
        {
            AssertValidIdentifier("foo.bar");
        }

        [Fact]
        public void LexQuotedIdentifier()
        {
            AssertValidIdentifier("\"$test\"");
        }

        private void AssertValidIdentifier(string text)
        {
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
            Assert.False(token.HasErrors);
        }

        [Fact]
        public void LexUnterminatedQuotedIdentifier()
        {
            string text = "\"$foo";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.True(token.HasErrors);
            Assert.Equal(DiagnosticId.UnterminatedQuotedIdentifier, token.SyntaxError.Id);
            Assert.Equal(new TextSpan(0, 0), token.SyntaxError.TextSpan);
        }

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
        public void LexUnterminatedMultilineToken()
        {
            string text = "/* foo";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
            Assert.True(token.HasErrors);
            Assert.Equal(DiagnosticId.UnterminatedComment, token.SyntaxError.Id);
            Assert.Equal(new TextSpan(0, 0), token.SyntaxError.TextSpan);
        }

        [Fact]
        public void LexDialogueBlockStartTag()
        {
            AssertValidToken("<PRE box69>", SyntaxTokenKind.DialogueBlockStartTag, "box69");
        }

        [Fact]
        public void LexUnterminatedDialogueBlockStartTag()
        {
            string text = "<PRE foo";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.DialogueBlockStartTag, token.Kind);
            Assert.True(token.HasErrors);
            Assert.Equal(DiagnosticId.UnterminatedDialogueBlockStartTag, token.SyntaxError.Id);
            Assert.Equal(new TextSpan(0, 0), token.SyntaxError.TextSpan);
        }
        
        [Fact]
        public void LexDialogueBlockIdentifier()
        {
            AssertValidToken("[text069]", SyntaxTokenKind.DialogueBlockIdentifier, "text069", LexingMode.DialogueBlock);
        }

        [Fact]
        public void LexUnterminatedDialogueBlockIdentifier()
        {
            string text = "[foo";
            var token = LexToken(text, LexingMode.DialogueBlock);

            Assert.Equal(SyntaxTokenKind.DialogueBlockIdentifier, token.Kind);
            Assert.Equal(DiagnosticId.UnterminatedDialogueBlockIdentifier, token.SyntaxError.Id);
            Assert.Equal(new TextSpan(0, 0), token.SyntaxError.TextSpan);
        }

        [Fact]
        public void LexDialogueBlockEndTag()
        {
            AssertValidToken("</PRE>", SyntaxTokenKind.DialogueBlockEndTag, LexingMode.DialogueBlock);
        }

        [Fact]
        public void LexSimplePXmlString()
        {
            AssertValidToken("sample text", SyntaxTokenKind.PXmlString, LexingMode.DialogueBlock);
        }

        [Fact]
        public void LexPXmlLineSeparator()
        {
            AssertValidToken("\r\n", SyntaxTokenKind.PXmlLineSeparator, LexingMode.DialogueBlock);
        }

        [Fact]
        public void LexPXmlStringWithVerbatimText()
        {
            AssertValidToken("<PRE>scene</PRE>", SyntaxTokenKind.PXmlString, LexingMode.DialogueBlock);
        }

        [Fact]
        public void LexArrowToken()
        {
            AssertValidToken("->", SyntaxTokenKind.ArrowToken);
        }

        [Fact]
        public void LexAtArrowToken()
        {
            AssertValidToken("@->", SyntaxTokenKind.AtArrowToken);
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

        private void AssertValidToken(string text, SyntaxTokenKind expectedKind, LexingMode mode = LexingMode.Normal)
        {
            var token = LexToken(text, mode);

            Assert.Equal(expectedKind, token.Kind);
            Assert.Equal(text, token.Text);
            Assert.False(token.HasErrors);
        }

        private void AssertValidToken(string text, SyntaxTokenKind expectedKind, string expectedText, object expectedValue, LexingMode mode = LexingMode.Normal)
        {
            var token = LexToken(text, mode);

            Assert.Equal(expectedKind, token.Kind);
            Assert.Equal(expectedValue, token.Value);
            Assert.Equal(expectedText, token.Text);
            Assert.False(token.HasErrors);
        }

        private void AssertValidToken(string text, SyntaxTokenKind expectedKind, object expectedValue, LexingMode mode = LexingMode.Normal)
        {
            var token = LexToken(text, mode);

            Assert.Equal(expectedKind, token.Kind);
            Assert.Equal(expectedValue, token.Value);
            Assert.Equal(text, token.Text);
            Assert.False(token.HasErrors);
        }
    }
}
