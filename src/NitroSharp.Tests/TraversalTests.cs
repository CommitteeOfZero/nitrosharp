using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NitroSharp.Tests;

public class TraversalTests
{
    [Fact]
    public void NodesAreTraversedInDepthFirstOrder()
    {
        var world = new World();
        TestEntity a = world.AddEntity(new TestEntity("A", parent: null));
        TestEntity b = world.AddEntity(new TestEntity("B", parent: a));
        TestEntity d = world.AddEntity(new TestEntity("D", parent: b));
        TestEntity e = world.AddEntity(new TestEntity("E", parent: b));
        TestEntity f = world.AddEntity(new TestEntity("F", parent: b));
        TestEntity h = world.AddEntity(new TestEntity("H", parent: f));
        TestEntity c = world.AddEntity(new TestEntity("C", parent: a));
        TestEntity g = world.AddEntity(new TestEntity("G", parent: c));
        TestEntity i = world.AddEntity(new TestEntity("I", parent: g));

        world.BeginFrame();
        var visitedNodes = new List<Entity>();
        foreach (Entity descendant in a.GetDescendants())
        {
            visitedNodes.Add(descendant);
        }

        IEnumerable<string> nodeNames = visitedNodes.Select(x => x.Name.Value);
        Assert.Equal(["C", "G", "I", "B", "F", "H", "E", "D"], nodeNames);
    }
}
