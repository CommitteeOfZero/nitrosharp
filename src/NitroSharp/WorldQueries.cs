using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using NitroSharp.NsScript;
using NitroSharp.Utilities;
using SharpDX.Direct3D11;

#nullable enable

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

        public QueryResultsEnumerator<T> GetEnumerator()
            => new QueryResultsEnumerator<T>(_results);
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
            return _current is object;
        }
    }

    internal sealed partial class World
    {
        public QueryResultsEnumerable<T> Query<T>(EntityQuery query)
            where T : Entity
        {
            SmallList<Entity> results = Query(query);
            return new QueryResultsEnumerable<T>(results);
        }

        public SmallList<Entity> Query(EntityQuery query)
        {
            if (EntityPath.IsValidPath(query, out EntityPath simplePath)
                && Get(simplePath) is Entity result)
            {
                return new SmallList<Entity>(result);
            }
            return QuerySlow(query);
        }

        private SmallList<Entity> QuerySlow(EntityQuery query)
        {
            var queryParts = query.EnumerateParts().ToSmallList();
            EntityQueryPart queryRoot = queryParts[0];
            SmallList<Entity> roots = default;
            if (queryRoot.IsWildcardPattern)
            {
                ReadOnlySpan<char> prefix = queryRoot.Value.Span[..^1];
                if (!queryRoot.SearchInAliases)
                {
                    foreach ((EntityId id, EntityRec rec) in _entities)
                    {
                        if (!rec.Entity.HasParent && id.Name.StartsWith(prefix))
                        {
                            roots.Add(rec.Entity);
                        }
                    }
                }
                else
                {
                    prefix = prefix[1..];
                    foreach ((EntityPath id, EntityId rec) in _aliases)
                    {
                        // TODO
                        Entity entity = Get(rec)!;
                        if (!entity.HasParent && id.Name.StartsWith(prefix))
                        {
                            roots.Add(Get(rec)!);
                        }
                    }
                }
            }
            else
            {
                Entity? root = Get(new EntityPath(queryRoot.Value.ToString()));
                if (root != null)
                {
                    roots.Add(root);
                }
            }

            SmallList<Entity> results = default;
            ReadOnlySpan<EntityQueryPart> remainingParts = queryParts.AsSpan()[1..];
            foreach (Entity root in roots.AsSpan())
            {
                Match(root, remainingParts, ref results);
            }

            return results;
        }

        private void Match(
            Entity entity,
            ReadOnlySpan<EntityQueryPart> remainingQueryParts,
            ref SmallList<Entity> results)
        {
            if (remainingQueryParts.Length == 0)
            {
                results.Add(entity);
                return;
            }

            EntityQueryPart part = remainingQueryParts[0];
            ReadOnlySpan<char> prefix = part.Value.Span;
            if (part.IsWildcardPattern)
            {
                prefix = prefix[..^1];
            }

            foreach (EntityId child in entity.Children)
            {
                if (child.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    // TODO
                    Match(Get(child)!, remainingQueryParts[1..], ref results);
                }
            }

            if (prefix.Length == 0 && entity.Id.MouseState != MouseState.Invalid)
            {
                Match(entity, remainingQueryParts[1..], ref results);
            }
        }
    }
}
