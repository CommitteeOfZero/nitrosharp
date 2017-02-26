using Xunit;

namespace SciAdvNet.NSScript.Tests
{
    public sealed class LexicalTests
    {
        [Fact]
        public void TestEmptyString()
        {
            var token = LexToken(string.Empty);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void TestStringLiteral()
        {
            string text = "\"literal\"";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.StringLiteralToken, token.Kind);
            Assert.Equal(text, token.Text);
            Assert.Equal("literal", token.Value);
        }

        [Fact]
        public void TestNumericLiteral()
        {
            string text = "42";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.NumericLiteralToken, token.Kind);
            Assert.Equal(42, token.Value);
        }

        [Fact]
        public void TestAtPrefixedNumericLiteral()
        {
            string text = "@42";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.NumericLiteralToken, token.Kind);
            Assert.Equal(42, token.Value);
        }

        [Fact]
        public void TestSingleLetterIdentifier()
        {
            string text = "a";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void TestDollarPrefixedIdentifier()
        {
            string text = "$globalVar";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void TestHashPrefixedIdentifier()
        {
            string text = "#flag";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void TestArrowPrefixedIdentifier()
        {
            string text = "@->test";
            var token = LexToken(text);
            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void TestJapaneseIdentifier()
        {
            string identifier = "#ev100_06_1_６人祈る_a";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

        [Fact]
        public void TestIdentifierWithSlash()
        {
            string identifier = "nss/sys_load.nss";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

        [Fact]
        public void TestIdentifierInQuotes()
        {
            string identifier = "\"$test\"";
            var token = LexToken(identifier);

            Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
            Assert.Equal(identifier, token.Text);
        }

        //[Fact]
        //public void TestIdentifierStartingWithDigit()
        //{
        //    string identifier = "7_hoppies";
        //    var token = LexToken(identifier);

        //    Assert.Equal(SyntaxTokenKind.IdentifierToken, token.Kind);
        //    Assert.Equal(identifier, token.Text);
        //}

        [Fact]
        public void TestSingleLineComment()
        {
            string comment = "// this is a comment.";
            var token = LexToken(comment);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void TestMultiLineComment()
        {
            string comment = @"/*
				初回起動時ではないときは、プレイ速度をバックアップ
			*/";
            var token = LexToken(comment);
            Assert.Equal(SyntaxTokenKind.EndOfFileToken, token.Kind);
        }

        [Fact]
        public void TestXmlStartTag()
        {
            string text = "<U>";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.XmlElementStartTag, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void TestXmlEndTag()
        {
            string text = "</U>";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.XmlElementEndTag, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        public void TestXmlTagWithNewline()
        {
            string text = "<FONT\n incolor=\"#88abda\" outcolor=\"BLACK\">";
            var token = LexToken(text);

            Assert.Equal(SyntaxTokenKind.XmlElementStartTag, token.Kind);
        }

        //[Fact]
        //public void TestVerbatimStringLiteral()
        //{
        //    string text = "<pre>sample text</pre>";
        //    var token = LexToken(text);

        //    Assert.Equal(SyntaxTokenKind.Xml_VerbatimText, token.Kind);
        //    Assert.Equal(text, token.Text);
        //    Assert.Equal("sample text", token.Value);
        //}

        [Fact]
        public void TestIncludeDirective()
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
