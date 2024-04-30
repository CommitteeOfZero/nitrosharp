using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        yield return ("foo1", new[] { "foo1" });
        yield return ("foo1/bar", new[] { "foo1/bar" });
        yield return ("foo1/bar", new[] { "foo1/bar" });
        yield return ("*", new[] { "foo1", "foo2" });
        yield return ("*/*", new[] { "foo1/bar", "foo2/bar" });
        yield return ("@*", new[] { "foo1", "foo2/bar" });
        yield return ("foo1/@*", new[] { "foo1", "foo2/bar" });
        //yield return ("@*/@*", new[] { "foo1", "foo2/bar" });
    }

    [Theory]
    [MemberData(nameof(GetGoodQueryTestData))]
    public void Execute(string query, string[] expectedResults)
    {
        var world = new World();
        var parsedQuery = EntityQuery.Parse(query);
        var process = new Process(EntityName.Parse("main"), null, new FontSettings());
        var mainThread = new Thread(EntityName.Parse("test"), process, new NsScriptThreadState(), isMain: true);
        var foo1 = new TestEntity("foo1", mainThread);
        var foo2 = new TestEntity("foo2", mainThread);
        var bar1 = new TestEntity("bar", foo1);
        var bar2 = new TestEntity("bar", foo2);
        world.AddEntity(process);
        world.AddEntity(mainThread);
        world.AddEntity(foo1);
        world.AddEntity(foo2);
        world.AddEntity(bar1);
        world.AddEntity(bar2);

        world.RegisterProcess(process, isMain: true, activate: true);

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
