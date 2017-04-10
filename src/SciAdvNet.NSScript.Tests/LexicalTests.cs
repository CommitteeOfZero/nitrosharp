using Xunit;

namespace SciAdvNet.NSScript.Tests
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
        public void LexNumericLiteral()
        {
            string text = "42";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.NumericLiteralToken, token.Kind);
            Assert.Equal(42, token.Value);
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
            string identifier = "#ev100_06_1_６人祈る_a";
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
        public void LexIdentifierInQuotes()
        {
            string identifier = "\"$test\"";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

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
        public void LexIncludeDirective()
        {
            string text = "#include \"test.nss\"";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IncludeDirective, token.Kind);
            Assert.Equal(text, token.Text);
        }

        private SyntaxToken LexToken(string text)
        {
            SyntaxToken result = null;
            foreach (var token in NSScript.ParseTokens(text))
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
