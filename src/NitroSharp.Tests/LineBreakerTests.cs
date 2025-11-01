using System.Collections.Generic;
using NitroSharp.Text;
using Xunit;
using System.Linq;

namespace NitroSharp.Tests;

public sealed class LineBreakerTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void LineBreakerTest(string text, LineBreak[] expectedLineBreaks)
    {
        var lb = new LineBreaker(text);
        Assert.Equal(expectedLineBreaks, lb.ToArray());
    }

    public static IEnumerable<object[]> GetTestData()
        => GetTests().Select(x => new object[] { x.Item1, x.Item2 });

    // Mostly ported from https://github.com/alexheretic/glyph-brush
    private static IEnumerable<(string, LineBreak[])> GetTests()
    {
        yield return ("meow meow", [Soft(5), Soft(9)]);
        // LB7, LB18
        yield return ("a  b", [Soft(3), Soft(4)]);
        // LB5
        yield return ("a\nb", [Hard(2), Soft(3)]);
        yield return ("\r\n\r\n", [Hard(2), Hard(4)]);
        // LB8a
        yield return ("\u200D\U0001F3FB", [Soft(2)]);
        // LB10 combining mark after space
        yield return ("a \u0301", [Soft(2), Soft(3)]);
        // LB15
        yield return ("\" [", [Soft(3)]);
        // LB17
        yield return ("a \u2014 \u2014", [Soft(2), Soft(5)]);
        // LB18
        yield return ("a \"b\" c", [Soft(2), Soft(6), Soft(7)]);
        // LB21
        yield return ("a-b", [Soft(2), Soft(3)]);
        // LB21a
        yield return ("\u05D0-\u05D0", [Soft(3)]);
        // LB23a
        yield return ("\U0001F3FB%", [Soft(2)]);
        // LB30b
        yield return ("\U0001F466\U0001F3FB", [Soft(2)]);
        // LB31
        yield return ("\U0001F1E6\U0001F1E6\U0001F1E6\U0001F1E6", [Soft(2), Soft(4)]);
    }

    private static LineBreak Soft(int pos) => new(pos, LineBreakKind.Soft);
    private static LineBreak Hard(int pos) => new(pos, LineBreakKind.Hard);
}
