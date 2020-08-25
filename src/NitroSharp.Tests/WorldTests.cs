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
        private readonly World _entityManager;

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
                ResolvedEntityPath resolvedPath = _entityManager.ResolvePath(path);
                _entityManager.Add(new SimpleEntity(resolvedPath));
                int level = name.Count(c => c == '/');
                levels[level].Add(path);
            }

            _entityManager = new World();
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
                var results = _entityManager.Query(new EntityQuery(query)).AsSpan().ToArray()
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
            var resolvedPath = _entityManager.ResolvePath(new EntityPath("parent"));
            var parent = _entityManager.Add(new SimpleEntity(resolvedPath));
            _entityManager.SetAlias(parent.Id, new EntityPath("alias"));
            Assert.Equal(parent, _entityManager.Get(new EntityPath("@alias")));

            resolvedPath = _entityManager.ResolvePath(new EntityPath("parent/child"));
            var child = _entityManager.Add(new SimpleEntity(resolvedPath));
            Assert.Equal(child, _entityManager.Get(new EntityPath("@alias/child")));

            _entityManager.DestroyEntity(child);
            Assert.Null(_entityManager.Get(new EntityPath("parent/child")));
            Assert.Null(_entityManager.Get(new EntityPath("@alias/child")));
        }

        [Fact]
        public void ComplexQuery()
        {
            var parent = _entityManager.Add(new SimpleEntity(_entityManager.ResolvePath(new EntityPath("parent"))));
            var child = _entityManager.Add(new SimpleEntity(_entityManager.ResolvePath(new EntityPath("parent/child"))));
            var grandchild = _entityManager.Add(new SimpleEntity(_entityManager.ResolvePath(new EntityPath("parent/child/MouseOver/grandchild"))));

            var parent2 = _entityManager.Add(new SimpleEntity(_entityManager.ResolvePath(new EntityPath("parent2"))));
            var child2 = _entityManager.Add(new SimpleEntity(_entityManager.ResolvePath(new EntityPath("parent2/child2"))));
            var grandchild2 = _entityManager.Add(new SimpleEntity(_entityManager.ResolvePath(new EntityPath("parent2/child2/MouseOver/grandchild2"))));

            _entityManager.SetAlias(child.Id, new EntityPath("alias"));
            _entityManager.SetAlias(child2.Id, new EntityPath("alias2"));
            var results = _entityManager.Query(new EntityQuery("@alias*/*/*"));
            Assert.Equal(results.AsSpan().ToArray(), new[] { grandchild, grandchild2 });
        }
    }
}
