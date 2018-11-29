using NitroSharp.NsScriptNew.Syntax;
using NitroSharp.NsScriptNew.Text;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    internal static class Common
    {
        public static void AssertSpannedText(string text, string substring, Spanned<string> spannedSubstring)
        {
            Assert.Equal(substring, spannedSubstring.Value);
            Assert.Equal(SpanOf(text, substring), spannedSubstring.Span);
        }

        private static TextSpan SpanOf(string text, string substring)
        {
            return new TextSpan(text.IndexOf(substring), substring.Length);
        }
    }
}
