using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using Xunit;

namespace NitroSharp.Tests;

public class QueryExecutionTests
{
    public static IEnumerable<object[]> GetGoodQueryTestData()
        => GetGoodQueries().Select(x => new object[] { x.Item1, x.Item2 });

    private static IEnumerable<(string, string[])> GetGoodQueries()
    {
        yield return ("foo1", ["foo1"]);
        yield return ("foo1/bar", ["foo1/bar"]);
        yield return ("foo1/bar", ["foo1/bar"]);
        yield return ("*", ["foo1", "foo2"]);
        yield return ("*/*", ["foo1/bar", "foo2/bar"]);
        yield return ("@*", ["foo1", "foo2/bar"]);
        yield return ("foo1/@*", ["foo1", "foo2/bar"]);
        //yield return ("@*/@*", new[] { "foo1", "foo2/bar" });
    }

    [Theory]
    [MemberData(nameof(GetGoodQueryTestData))]
    public void Execute(string query, string[] expectedResults)
    {
        var (process, mainThread) = (TestContext.MainProcess, TestContext.MainThread);
        var world = new World(process, mainThread);
        var parsedQuery = EntityQuery.Parse(query);
        var foo1 = new TestEntity("foo1", mainThread);
        var foo2 = new TestEntity("foo2", mainThread);
        var bar1 = new TestEntity("bar", foo1);
        var bar2 = new TestEntity("bar", foo2);

        world.AddEntity(foo1);
        world.AddEntity(foo2);
        world.AddEntity(bar1);
        world.AddEntity(bar2);

        foo1.SetAlias(EntityAlias.Parse("cat1"));
        bar2.SetAlias(EntityAlias.Parse("cat2"));

        world.BeginFrame();

        IEnumerable<string> actualResults = world.Query(parsedQuery)
            .AsSpan()
            .ToArray()
            .Select(x => x.GetAbsolutePath());

        Assert.Equal(expectedResults, actualResults);
    }
}
