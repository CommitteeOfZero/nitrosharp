using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ProjectHoppy
{
    public static class EntityManagerExtensions
    {
        public static IEnumerable<Entity> PerformQuery(this EntityManager entityManager, string query)
        {
            if (query.Contains('*'))
            {
                return WildcardQuery(entityManager, query);
            }
            else
            {
                var result = entityManager.SafeGet(query);
                return result != null ? new[] { result } : Enumerable.Empty<Entity>();
            }
        }

        public static IEnumerable<Entity> WildcardQuery(this EntityManager entityManager, string query)
        {
            query = query.Replace("*", string.Empty).ToUpper();
            return entityManager.AllEntities.Where(x => x.Key.ToUpper().StartsWith(query)).Select(x => x.Value);
        }
    }
}
