using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests;

public class DialogueLexingTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void LexDialogueBlock(string text, (SyntaxTokenKind, string)[] expectedTokens)
    {
        AssertTokens(text, expectedTokens);
    }

    public static IEnumerable<object[]> GetTestData()
    {
        yield return
        [
            "Sample Text",
            new[] { (SyntaxTokenKind.Markup, "Sample Text") }
        ];
        yield return
        [
            "Line 1\r\n\r\nLine 2",
            new[]
            {
                (SyntaxTokenKind.Markup, "Line 1"),
                (SyntaxTokenKind.MarkupBlankLine, "\r\n\r\n"),
                (SyntaxTokenKind.Markup, "Line 2")
            }
        ];
        yield return
        [
            "Sample\r\nText",
            new[] { (SyntaxTokenKind.Markup, "Sample\r\nText") }
        ];
        yield return
        [
            "Sample\r\n{}Text",
            new[]
            {
                (SyntaxTokenKind.Markup, "Sample\r\n"),
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}"),
                (SyntaxTokenKind.Markup, "Text")
            }
        ];
        yield return
        [
            "Sample\nText",
            new[] { (SyntaxTokenKind.Markup, "Sample\nText") }
        ];
        yield return
        [
            "   \r\nSample Text",
            new[] { (SyntaxTokenKind.Markup, "Sample Text") }
        ];
        yield return
        [
            "    {}    Sample Text",
            new[]
            {
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}"),
                (SyntaxTokenKind.Markup, "    Sample Text")
            }
        ];
        yield return
        [
            """
            {}
                Sample Text
            """,
            new[]
            {
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}"),
                (SyntaxTokenKind.Markup, "    Sample Text")
            }
        ];
        yield return
        [
            "{} \r\n    Sample Text",
            new[]
            {
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}"),
                (SyntaxTokenKind.Markup, " \r\n    Sample Text")
            }
        ];
        yield return
        [
            "Sample Text{}",
            new[]
            {
                (SyntaxTokenKind.Markup, "Sample Text"),
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}")
            }
        ];
        yield return
        [
            "Sample {} Text",
            new[]
            {
                (SyntaxTokenKind.Markup, "Sample "),
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}"),
                (SyntaxTokenKind.Markup, " Text")
            }
        ];
        yield return
        [
            """
            Sample
            // This is
            // a comment
            Text
            """,
            new[]
            {
                (SyntaxTokenKind.Markup, """
                                         Sample
                                         // This is
                                         // a comment
                                         Text
                                         """)
            }
        ];

        yield return
        [
            """
            Line 1

            // This is
            // a comment
            Line2
            """.ReplaceLineEndings(),
            new[]
            {
                (SyntaxTokenKind.Markup, "Line 1"),
                (SyntaxTokenKind.MarkupBlankLine, "\r\n\r\n"),
                (SyntaxTokenKind.Markup, """
                                         // This is
                                         // a comment
                                         Line2
                                         """)
            }
        ];

        yield return
        [
            """
            {}// this is a comment {
            Sample Text
            """,
            new[]
            {
                (SyntaxTokenKind.OpenBrace, "{"),
                (SyntaxTokenKind.CloseBrace, "}"),
                (SyntaxTokenKind.Markup, """
                                         // this is a comment {
                                         Sample Text
                                         """)
            }
        ];

        yield return
        [
            "\r\n// this\r\n// is\r\n// a comment\r\nSample Text",
            new[]
            {
                (SyntaxTokenKind.Markup, "// this\r\n// is\r\n// a comment\r\nSample Text")
            }
        ];

        yield return
        [
            """

            // this is
            Line 1
            // a comment


            """,
            new[]
            {
                (SyntaxTokenKind.Markup, """
                                             // this is
                                             Line 1
                                             // a comment


                                             """)
            }
        ];
        yield return
        [
            " \r\n// this is\r\nLine 1\r\n// a comment\r\n\r\n",
            new[] { (SyntaxTokenKind.Markup, "// this is\r\nLine 1\r\n// a comment\r\n\r\n") }
        ];
        yield return
        [
            "//\n    foo",
            new[] { (SyntaxTokenKind.Markup, "//\n    foo") }
        ];
    }

    private static void AssertTokens(string text, (SyntaxTokenKind, string)[] expectedTokens)
    {
        text = $"""

                <PRE box00>
                [text001]
                {text}</PRE>
                """;

        var lexResult = SyntaxToken.Lex(text);
        SyntaxToken[] actualTokens = lexResult.RealizeTokens().Skip(2).SkipLast(2).ToArray();
        Assert.Equal(expectedTokens.Length, actualTokens.Length);
        var zipped = expectedTokens.Zip(actualTokens);
        foreach (((SyntaxTokenKind expectedKind, string expectedText), SyntaxToken token) in zipped)
        {
            const string crlf = "\r\n";
            string expected = expectedText.ReplaceLineEndings(crlf);
            string actual = lexResult.GetText(token).ToString().ReplaceLineEndings(crlf);
            Assert.Equal(expectedKind, token.Kind);
            Assert.Equal(expected, actual);
        }
    }
}
