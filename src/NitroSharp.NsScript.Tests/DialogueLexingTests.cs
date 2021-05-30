using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class DialogueLexingTests
    {
        [Theory]
        [MemberData(nameof(GetTestData))]
        public void LexDialogueBlock(string text, (SyntaxTokenKind, string)[] expectedTokens)
        {
            AssertTokens(text, expectedTokens);
        }

        public static IEnumerable<object> GetTestData()
        {
            yield return new object[]
            {
                "Sample Text",
                new[] { (SyntaxTokenKind.Markup, "Sample Text") }
            };
            yield return new object[]
            {
                "Line 1\r\n\r\nLine 2",
                new[]
                {
                    (SyntaxTokenKind.Markup, "Line 1"),
                    (SyntaxTokenKind.MarkupBlankLine, "\r\n\r\n"),
                    (SyntaxTokenKind.Markup, "Line 2")
                }
            };
            yield return new object[]
            {
                "Sample\r\nText",
                new[] { (SyntaxTokenKind.Markup, "Sample\r\nText") }
            };
            yield return new object[]
            {
                "Sample\r\n{}Text",
                new[]
                {
                    (SyntaxTokenKind.Markup, "Sample\r\n"),
                    (SyntaxTokenKind.OpenBrace, "{"),
                    (SyntaxTokenKind.CloseBrace, "}"),
                    (SyntaxTokenKind.Markup, "Text")
                }
            };
            yield return new object[]
            {
                "Sample\nText",
                new[] { (SyntaxTokenKind.Markup, "Sample\nText") }
            };
            yield return new object[]
            {
                "   \r\nSample Text",
                new[] { (SyntaxTokenKind.Markup, "Sample Text") }
            };
            yield return new object[]
            {
                "    {}    Sample Text",
                new[]
                {
                    (SyntaxTokenKind.OpenBrace, "{"),
                    (SyntaxTokenKind.CloseBrace, "}"),
                    (SyntaxTokenKind.Markup, "    Sample Text")
                }
            };
            yield return new object[]
            {
                @"{}
    Sample Text",
                new[]
                {
                    (SyntaxTokenKind.OpenBrace, "{"),
                    (SyntaxTokenKind.CloseBrace, "}"),
                    (SyntaxTokenKind.Markup, "    Sample Text")
                }
            };
            yield return new object[]
            {
                "Sample Text{}",
                new[]
                {
                    (SyntaxTokenKind.Markup, "Sample Text"),
                    (SyntaxTokenKind.OpenBrace, "{"),
                    (SyntaxTokenKind.CloseBrace, "}")
                }
            };
            yield return new object[]
            {
                "Sample {} Text",
                new[]
                {
                    (SyntaxTokenKind.Markup, "Sample "),
                    (SyntaxTokenKind.OpenBrace, "{"),
                    (SyntaxTokenKind.CloseBrace, "}"),
                    (SyntaxTokenKind.Markup, " Text")
                }
            };
            yield return new object[]
            {
                @"Sample
// This is
// a comment
Text",
                new[]
                {
                    (SyntaxTokenKind.Markup, @"Sample
// This is
// a comment
Text"),
                }
            };

            yield return new object[]
            {
                @"Line 1

// This is
// a comment
Line2",
                new[]
                {
                    (SyntaxTokenKind.Markup, "Line 1"),
                    (SyntaxTokenKind.MarkupBlankLine, "\r\n\r\n"),
                    (SyntaxTokenKind.Markup, @"// This is
// a comment
Line2")
                }
            };

            yield return new object[]
            {
                @"{}// this is a comment {
Sample Text",
                new[]
                {
                    (SyntaxTokenKind.OpenBrace, "{"),
                    (SyntaxTokenKind.CloseBrace, "}"),
                    (SyntaxTokenKind.Markup, @"// this is a comment {
Sample Text") }
            };
        }

        private static void AssertTokens(string text, (SyntaxTokenKind, string)[] expectedTokens)
        {
            text = @$"
<PRE box00>
[text001]
{text}</PRE>";

            (SyntaxTokenEnumerable tokens, LexingContext context) = Parsing.LexTokens(text);
            SyntaxToken[] actualTokens = tokens.ToArray().Skip(2).SkipLast(2).ToArray();
            Assert.Equal(expectedTokens.Length, actualTokens.Length);
            var zipped = expectedTokens.Zip(actualTokens);
            foreach (((SyntaxTokenKind expectedKind, string expectedText), SyntaxToken token) in zipped)
            {
                Assert.Equal(expectedKind, token.Kind);
                Assert.Equal(expectedText, context.GetText(token).ToString());
            }
        }
    }
}
