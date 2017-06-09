using CommitteeOfZero.Nitro.Foundation;
using System.Collections.Generic;
using System.Linq;

namespace CommitteeOfZero.Nitro
{
    public static class EntityManagerExtensions
    {
        private static Entity[] s_oneElementArray = new Entity[1];

        private static bool IsWildcardQuery(string s) => s[s.Length - 1] == '*';

        public static IEnumerable<Entity> Query(this EntityManager entityManager, string query)
        {
            if (IsWildcardQuery(query))
            {
                return WildcardQuery(entityManager, query);
            }
            else
            {
                if (entityManager.TryGet(query, out var result))
                {
                    if (result.IsScheduledForRemoval)
                    {
                        return Enumerable.Empty<Entity>();
                    }

                    s_oneElementArray[0] = result;
                    return s_oneElementArray;
                }

                return WildcardQuery(entityManager, query + "/*");
            }
        }

        private static IEnumerable<Entity> WildcardQuery(this EntityManager entityManager, string query)
        {
            query = query.ToUpperInvariant();

            foreach (var pair in entityManager.AllEntities)
            {
                string key = pair.Key.ToUpperInvariant();
                if (key.Length < query.Length - 1)
                {
                    continue;
                }

                bool matches = true;
                for (int i = 0; i < query.Length - 1; i++)
                {
                    if (key[i] != query[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches && !pair.Value.IsScheduledForRemoval)
                {
                    yield return pair.Value;
                }
            }
        }
    }
}
