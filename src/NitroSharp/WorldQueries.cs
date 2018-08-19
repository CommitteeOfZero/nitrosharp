using System;
using System.Collections.Generic;

namespace NitroSharp
{
    internal static class WorldQueries
    {
        public static QueryResult Query(this World world, string query)
            => new QueryResult(world, query);

        public struct QueryResult
        {
            private readonly World _world;
            private readonly string _query;

            public QueryResult(World world, string query)
            {
                _world = world;
                _query = query;
            }

            public QueryExecutor GetEnumerator() => new QueryExecutor(_world, _query);
            public bool Any() => GetEnumerator().MoveNext() == true;
        }

        public struct QueryExecutor
        {
            private readonly World _world;
            private Dictionary<string, Entity>.Enumerator _entityEnumerator;
            private readonly string _query;

            public QueryExecutor(World world, string query)
            {
                _world = world;
                _entityEnumerator = world.EntityEnumerator;
                _query = query;
                Current = default;
            }

            public (Entity entity, string name) Current { get; private set; }

            public bool MoveNext()
            {
                if (!_query.EndsWith("*"))
                {
                    if (Current.entity.IsValid) { return false; }

                    bool result = _world.TryGetEntity(_query, out Entity entity);
                    Current = (entity, _query);
                    return result;
                }

                ReadOnlySpan<char> startsWith = _query.AsSpan().Slice(0, _query.Length - 1);
                while (_entityEnumerator.MoveNext())
                {
                    KeyValuePair<string, Entity> kvp = _entityEnumerator.Current;
                    if (kvp.Key.AsSpan().StartsWith(startsWith))
                    {
                        Current = (kvp.Value, kvp.Key);
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
