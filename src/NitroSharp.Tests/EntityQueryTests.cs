using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NitroSharp.Tests
{
    public class EntityQueryTests
    {
        private readonly string[][] _treeLevels;
        private readonly OldWorld _world;

        public EntityQueryTests()
        {
            var levels = new List<string>[3];
            for (int i = 0; i < 3; i++)
            {
                levels[i] = new List<string>();
            }

            void entity(string name)
            {
                _world.CreateEntity(name, EntityKind.Sprite);
                int level = name.Count(c => c == '/');
                levels[level].Add(name);
            }

            _world = new OldWorld();
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

            _treeLevels = new string[3][];
            for (int i = 0; i < 3; i++)
            {
                _treeLevels[i] = levels[i].ToArray();
            }
        }

        [Fact]
        public void TestEntityQuerying()
        {
            void Q(string query, params string[] expectedResults)
            {
                Assert.Equal(expectedResults, _world.Query(query).ToArray());
            }

            string[][] levels = _treeLevels;
            
            Q("root", "root");
            Q("root/e11", "root/e11");
            Q("root/e12/e22", "root/e12/e22");

            Q("*", levels[0]);
            Q("root/*", levels[1]);
            Q("root/e*", levels[1]);
            Q("root/*/*", levels[2]);

            Q("root/e11/*", new[] { "root/e11/e21", "root/e11/e22", "root/e11/e23" });
            Q("root/e12/*", new[] { "root/e12/e21", "root/e12/e22", "root/e12/e23" });

            Q("root/*/e21", new[] { "root/e11/e21", "root/e12/e21" });
            Q("root/e*/e23", new[] { "root/e11/e23", "root/e12/e23" });
        }
    }

    internal static class EntityQueryResultExtensions
    {
        public static string[] ToArray(this EntityQueryResult queryResult)
        {
            var list = new List<string>();
            foreach ((OldEntity _, string name) in queryResult)
            {
                list.Add(name);
            }
            return list.ToArray();
        }
    }
}
