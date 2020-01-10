using System;
using System.Collections.Generic;
using NitroSharp.Experimental;

namespace NitroSharp
{
    internal struct EntityQueryResult
    {
        private QueryExecutor _executor;

        public readonly string Query;
        public readonly bool IsEmpty;

        public EntityQueryResult(World world, string query, List<ReadOnlyMemory<char>> querySegments)
        {
            Query = query;
            _executor = new QueryExecutor(world, Query, querySegments);
            IsEmpty = _executor.EmptyResult;
        }

        public QueryExecutor GetEnumerator() => _executor;
        public bool Any() => GetEnumerator().MoveNext();

        internal struct QueryExecutor
        {
            private readonly World _world;
            private Dictionary<EntityName, Entity>.Enumerator _entityEnumerator;
            private readonly string _query;
            private readonly List<ReadOnlyMemory<char>> _querySegments;

            public QueryExecutor(World world, string query, List<ReadOnlyMemory<char>> querySegments)
                : this()
            {
                _world = world;
                _entityEnumerator = world.EntityEnumerator;
                _query = query;
                _querySegments = querySegments;
                EmptyResult = !MoveNext();
                _firstMoveNextCall = true;
            }

            public (Entity entity, EntityName name) Current { get; private set; }

            public readonly bool EmptyResult;
            private bool _firstMoveNextCall;

            public bool MoveNext()
            {
                if (_firstMoveNextCall)
                {
                    // Current is already set (see .ctor)
                    _firstMoveNextCall = false;
                    return Current.entity.IsValid;
                }

                if (!_query.Contains("*"))
                {
                    if (Current.entity.IsValid) { return false; }
                    bool result = _world.TryGetEntity(new EntityName(_query), out Entity entity);
                    Current = (entity, new EntityName(_query));
                    return result;
                }

                _querySegments.Clear();
                var segmentEnumerable = new SegmentEnumerable<char>(_query.AsMemory(), separator: '*');
                foreach (ReadOnlyMemory<char> segment in segmentEnumerable)
                {
                    _querySegments.Add(segment);
                }

                while (_entityEnumerator.MoveNext())
                {
                    KeyValuePair<EntityName, Entity> kvp = _entityEnumerator.Current;
                    if (SatisfiesQuery(kvp.Key.Value))
                    {
                        Current = (kvp.Value, kvp.Key);
                        return true;
                    }
                }

                return false;
            }

            private bool SatisfiesQuery(ReadOnlySpan<char> entityName)
            {
                ReadOnlySpan<char> nameSegment = entityName;
                bool firstSegment = true;
                foreach (ReadOnlyMemory<char> segmentMem in _querySegments)
                {
                    ReadOnlySpan<char> querySegment = segmentMem.Span;
                    int index = nameSegment.IndexOf(querySegment, StringComparison.Ordinal);
                    if (index < 0) { return false; }
                    if (firstSegment)
                    {
                        firstSegment = false;
                        if (index > 0 && !_query.StartsWith('*'))
                        {
                            return false;
                        }
                    }
                    nameSegment = nameSegment.Slice(start: index + querySegment.Length);
                }

                if (nameSegment.IndexOf('/') >= 0)
                {
                    // If entity's name has more '/'-separated segments than the query,
                    // we should return false.
                    // Example: Query("選択肢*") should NOT return "選択肢１/MouseUsual/選択肢１板１".
                    return false;
                }

                return true;
            }
        }

        private ref struct SegmentEnumerable<T> where T : IEquatable<T>
        {
            private readonly ReadOnlyMemory<T> _memory;
            private readonly T _separator;

            public SegmentEnumerable(ReadOnlyMemory<T> memory, T separator)
            {
                _memory = memory;
                _separator = separator;
            }

            public SegmentEnumerator<T> GetEnumerator()
                => new SegmentEnumerator<T>(_memory, _separator);
        }

        private ref struct SegmentEnumerator<T> where T : IEquatable<T>
        {
            private ReadOnlyMemory<T> _remaining;
            private readonly T _separator;

            public SegmentEnumerator(ReadOnlyMemory<T> memory, T separator)
            {
                _remaining = memory;
                _separator = separator;
                Current = default;
            }

            public bool MoveNext()
            {
                if (_remaining.IsEmpty)
                {
                    _remaining = Current = default;
                    return false;
                }

                int idx = _remaining.Span.IndexOf(_separator);
                if (idx >= 0)
                {
                    Current = _remaining.Slice(0, idx);
                    _remaining = _remaining.Slice(idx + 1);
                }
                else
                {
                    Current = _remaining;
                    _remaining = default;
                }

                return true;
            }

            public ReadOnlyMemory<T> Current { get; private set; }
        }
    }

    internal static class WorldQueries
    {
        private static readonly List<ReadOnlyMemory<char>> s_querySegments
            = new List<ReadOnlyMemory<char>>();

        public static EntityQueryResult Query(this World world, string query)
            => new EntityQueryResult(world, query, s_querySegments);
    }
}
