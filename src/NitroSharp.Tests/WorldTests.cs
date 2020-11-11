using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScript;
using Xunit;

#nullable enable

namespace NitroSharp.Tests
{
    public class WorldTests
    {
        private readonly EntityPath[][] _treeLevels;
        private readonly World _world;

        public WorldTests()
        {
            var levels = new List<EntityPath>[3];
            for (int i = 0; i < 3; i++)
            {
                levels[i] = new List<EntityPath>();
            }

            void entity(string name)
            {
                var path = new EntityPath(name);
                ResolvedEntityPath resolvedPath = _world.ResolvePath(0, path);
                _world.Add(new SimpleEntity(resolvedPath));
                int level = name.Count(c => c == '/');
                levels[level].Add(path);
            }

            _world = new World();
            entity("root");
            entity("root1");
            entity("root/e11");
            entity("root/e12");
            entity("root/e11/e21");
            entity("root/e11/e22");
            entity("root/e11/e23");
            entity("root/e12/e21");
            entity("root/e12/e22");
            entity("root/e12/e23");
            entity("root/MouseOver/img");

            _treeLevels = new EntityPath[3][];
            for (int i = 0; i < 3; i++)
            {
                _treeLevels[i] = levels[i].ToArray();
            }
        }

        [Fact]
        public void Query()
        {
            void Q(string query, params string[] expectedResults)
            {
                var results = _world.Query(0, new EntityQuery(query)).AsSpan().ToArray()
                    .Select(x => x.Id.Path);
                Assert.Equal(expectedResults, results);
            }

            EntityPath[][] levels = _treeLevels;

            Q("root", "root");
            Q("root/e11", "root/e11");
            Q("root/e12/e22", "root/e12/e22");

            Q("*", levels[0].Select(x => x.Value).ToArray());
            Q("root/*", levels[1].Select(x => x.Value).ToArray());
            Q("root/e*", levels[1].Select(x => x.Value).ToArray());
            Q("root/*/*", levels[2].Select(x => x.Value).ToArray());

            Q("root/e11/*", new[] { "root/e11/e21", "root/e11/e22", "root/e11/e23" });
            Q("root/e12/*", new[] { "root/e12/e21", "root/e12/e22", "root/e12/e23" });

            Q("root/*/e21", new[] { "root/e11/e21", "root/e12/e21" });
            Q("root/e*/e23", new[] { "root/e11/e23", "root/e12/e23" });
        }

        [Fact]
        public void SetAlias()
        {
            var resolvedPath = _world.ResolvePath(0, new EntityPath("parent"));
            var parent = _world.Add(new SimpleEntity(resolvedPath));
            _world.SetAlias(parent.Id, new EntityPath("alias"));
            Assert.Equal(parent, _world.Get(0, new EntityPath("@alias")));

            resolvedPath = _world.ResolvePath(0, new EntityPath("parent/child"));
            var child = _world.Add(new SimpleEntity(resolvedPath));
            Assert.Equal(child, _world.Get(0, new EntityPath("@alias/child")));

            _world.DestroyEntity(child);
            Assert.Null(_world.Get(0, new EntityPath("parent/child")));
            Assert.Null(_world.Get(0, new EntityPath("@alias/child")));
        }

        [Fact]
        public void ComplexQuery()
        {
            var parent = _world.Add(new SimpleEntity(_world.ResolvePath(0, new EntityPath("parent"))));
            var child = _world.Add(new SimpleEntity(_world.ResolvePath(0, new EntityPath("parent/child"))));
            var grandchild = _world.Add(new SimpleEntity(_world.ResolvePath(0, new EntityPath("parent/child/MouseOver/grandchild"))));

            var parent2 = _world.Add(new SimpleEntity(_world.ResolvePath(0, new EntityPath("parent2"))));
            var child2 = _world.Add(new SimpleEntity(_world.ResolvePath(0, new EntityPath("parent2/child2"))));
            var grandchild2 = _world.Add(new SimpleEntity(_world.ResolvePath(0, new EntityPath("parent2/child2/MouseOver/grandchild2"))));

            _world.SetAlias(child.Id, new EntityPath("alias"));
            _world.SetAlias(child2.Id, new EntityPath("alias2"));
            var results = _world.Query(0, new EntityQuery("@alias*/*/*"));
            Assert.Equal(results.AsSpan().ToArray(), new[] { grandchild, grandchild2 });
        }
    }
}
