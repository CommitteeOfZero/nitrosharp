using MoeGame.Framework;
using System.Collections.Generic;

namespace CommitteeOfZero.Nitro
{
    public static class EntityManagerExtensions
    {
        public static IEnumerable<Entity> WildcardQuery(this EntityManager entityManager, string query)
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

                if (matches)
                {
                    yield return pair.Value;
                }
            }
        }
    }
}
