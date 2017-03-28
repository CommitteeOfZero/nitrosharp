using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ProjectHoppy
{
    public static class EntityManagerExtensions
    {
        //public static IEnumerable<Entity> PerformQuery(this EntityManager entityManager, string query)
        //{
        //    if (query.Contains('*'))
        //    {
        //        return WildcardQuery(entityManager, query);
        //    }
        //    else
        //    {
        //        var result = entityManager.SafeGet(query);
        //        return result != null ? new[] { result } : Enumerable.Empty<Entity>();
        //    }
        //}

        public static IEnumerable<Entity> WildcardQuery(this EntityManager entityManager, string query)
        {
            query = query.ToUpperInvariant();

            foreach (var pair in entityManager.AllEntities)
            {
                string key = pair.Key.ToUpperInvariant();

                bool matches = true;
                for (int i = 0; i < query.Length - 1; i++)
                {
                    if (key[i] != query[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    yield return pair.Value;
                }
                //if (pair.Key.ToUpperInvariant().StartsWith(query))
                //{
                //    yield return pair.Value;
                //}
            }
        }
    }
}
