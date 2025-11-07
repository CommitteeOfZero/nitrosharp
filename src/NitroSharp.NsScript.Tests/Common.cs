using System;
using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests;

internal static class Common
{
    public static void AssertSpannedText(string text, string substring, Spanned<string> spannedSubstring)
    {
        Assert.Equal(substring, spannedSubstring.Value);
        var actualSpan = new TextSpan(text.IndexOf(substring, StringComparison.Ordinal), substring.Length);
        Assert.Equal(actualSpan, spannedSubstring.Span);
    }
}
