using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScript;
using Xunit;

namespace NitroSharp.Tests;

using Part = EntityQueryPart;
using Scope = EntityQueryScope;

public sealed class EntityQueryTests
{
    [Theory]
    [MemberData(nameof(GetValidQueries))]
    public void ValidQueriesParse(string query, Part[] expectedParts)
    {
        var parsedQuery = EntityQuery.Parse(query);
        Assert.Equal(parsedQuery.Parts.ToArray(), expectedParts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("//")]
    [InlineData("foo/")]
    [InlineData("f@o")]
    [InlineData("f<o")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("@")]
    [InlineData("<@")]
    [InlineData("<@>")]
    [InlineData("<foo>/bar")]
    [InlineData("foo/<bar")]
    [InlineData("@foo/<bar")]
    [InlineData("<@foo/<bar")]
    [InlineData("<foo/@bar")]
    [InlineData("<foo/<bar")]
    [InlineData("<foo/<@bar")]
    public void InvalidQueriesDoNotParse(string query)
    {
        Assert.False(EntityQuery.TryParse(query).HasValue);
    }

    public static IEnumerable<object[]> GetValidQueries()
        => GetValidQueriesImpl().Select(x => new object[] { x.Item1, x.Item2 });

    private static IEnumerable<(string, Part[])> GetValidQueriesImpl()
    {
        yield return ("foo", [new Part("foo", Scope.Current, false)]);
        yield return ("*", [new Part("*", Scope.Current, false)]);
        yield return ("foo/bar/baz",
        [
            new Part("foo", Scope.Current, false),
            new Part("bar", Scope.Current, false),
            new Part("baz", Scope.Current, false)
        ]);
        yield return ("@foo/bar",
        [
            new Part("foo", Scope.CurrentAliases, false),
            new Part("bar", Scope.Current, false)
        ]);
        yield return ("foo/@bar",
        [
            new Part("foo", Scope.Current, false),
            new Part("bar", Scope.CurrentAliases, false)
        ]);
        yield return ("@fo*/@b*r",
        [
            new Part("fo*", Scope.CurrentAliases, false),
            new Part("b*r", Scope.CurrentAliases, false)
        ]);
        yield return ("<@foo/bar",
        [
            new Part("foo", Scope.AllAliases, false),
            new Part("bar", Scope.Current, false)
        ]);
        yield return ("<@foo/bar>",
        [
            new Part("foo", Scope.AllAliases, false),
            new Part("bar", Scope.Current, true)
        ]);
        yield return ("<@foo/@bar",
        [
            new Part("foo", Scope.AllAliases, false),
            new Part("bar", Scope.CurrentAliases, false)
        ]);
    }
}
