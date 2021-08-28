using System;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal readonly struct QueryResultsEnumerable<T>
        where T : Entity
    {
        private readonly SmallList<Entity> _results;

        public QueryResultsEnumerable(SmallList<Entity> results)
        {
            _results = results;
        }

        public bool IsEmpty => _results.Count == 0;

        public QueryResultsEnumerator<T> GetEnumerator()
            => new(_results);
    }

    internal struct QueryResultsEnumerator<T>
        where T : Entity
    {
        private SmallList<Entity> _results;
        private int _pos;
        private T? _current;

        public QueryResultsEnumerator(SmallList<Entity> results)
        {
            _results = results;
            _pos = 0;
            _current = null;
        }

        public T Current => _current!;

        public bool MoveNext()
        {
            _current = null;
            while (_pos < _results.Count && _current is null)
            {
                _current = _results[_pos++] as T;
            }
            return _current is not null;
        }
    }

    internal sealed partial class World
    {
        public QueryResultsEnumerable<T> Query<T>(uint contextId, EntityQuery query)
            where T : Entity
        {
            SmallList<Entity> results = Query(contextId, query);
            return new QueryResultsEnumerable<T>(results);
        }

        public SmallList<Entity> Query(uint contextId, EntityQuery query)
        {
            if (EntityPath.IsValidPath(query, out EntityPath simplePath)
                && Get(contextId, simplePath) is Entity result)
            {
                return new SmallList<Entity>(result);
            }
            return QuerySlow(contextId, query);
        }

        private SmallList<Entity> QuerySlow(uint contextId, EntityQuery query)
        {
            static bool match(ref SmallList<EntityQueryPart> queryParts, string entityPath)
            {
                var pathParts = new EntityQuery(entityPath).EnumerateParts();
                ReadOnlySpan<EntityQueryPart> remainingQueryParts = queryParts.AsSpan();
                bool matches = false;
                foreach (EntityQueryPart pathPart in pathParts)
                {
                    if (remainingQueryParts.IsEmpty || !matchParts(remainingQueryParts[0], pathPart))
                    {
                        matches = false;
                        break;
                    }

                    remainingQueryParts = remainingQueryParts[1..];
                    matches = remainingQueryParts.IsEmpty;
                }

                return matches;
            }

            static bool matchParts(EntityQueryPart queryPart, EntityQueryPart pathPart)
            {
                if (queryPart.Value.Span.Equals("*", StringComparison.Ordinal))
                {
                    return true;
                }
                if (queryPart.IsWildcardPattern)
                {
                    ReadOnlySpan<char> prefix = queryPart.Value.Span[..^1];
                    return pathPart.Value.Span.StartsWith(prefix);
                }
                return pathPart.Value.Span
                    .Equals(queryPart.Value.Span, StringComparison.Ordinal);
            }

            var queryParts = query.EnumerateParts().ToSmallList();
            EntityQueryPart queryRoot = queryParts[0];

            SmallList<Entity> results = default;
            if (queryRoot.SearchInAliases)
            {
                foreach ((EntityPath path, EntityId id) in _aliases)
                {
                    if (matchParts(queryRoot, new EntityQueryPart(path.Value.AsMemory(), 0, false)))
                    {
                        string amendedQuery = query.Value.Replace(queryRoot.Value.ToString(), id.Path);
                        uint parentContext = id.Context;
                        SmallList<Entity> subQueryRes = Query(parentContext, new EntityQuery(amendedQuery));
                        foreach (Entity e in subQueryRes)
                        {
                            results.Add(e);
                        }
                    }
                }

                return results;
            }

            foreach ((EntityId id, EntityRec _) in _entities)
            {
                if (id.Context == contextId && match(ref queryParts, id.Path))
                {
                    results.Add(Get(id)!);
                }
            }

            return results;
        }
    }
}
