using System;
using System.Collections.Generic;

#nullable enable

namespace NitroSharp
{
    internal struct EntityQueryResult
    {
        private readonly World _world;
        private QueryExecutor _executor;

        public readonly string Query;
        public readonly bool IsEmpty;

        public EntityQueryResult(World world, string query, List<ReadOnlyMemory<char>> querySegments)
        {
            _world = world;
            Query = query;
            _executor = new QueryExecutor(_world, Query, querySegments);
            IsEmpty = _executor.EmptyResult;
        }

        public QueryExecutor GetEnumerator() => _executor;
        public bool Any() => GetEnumerator().MoveNext();

        internal struct QueryExecutor
        {
            private readonly World _world;
            private Dictionary<string, Entity>.Enumerator _entityEnumerator;
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

            public (Entity entity, string name) Current { get; private set; }

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
                    bool result = _world.TryGetEntity(_query, out Entity entity);
                    Current = (entity, _query);
                    return result;
                }

                _querySegments.Clear();
                foreach (ReadOnlyMemory<char> segment in EnumerateQuerySegments(_query))
                {
                    _querySegments.Add(segment);
                }

                while (_entityEnumerator.MoveNext())
                {
                    KeyValuePair<string, Entity> kvp = _entityEnumerator.Current;
                    if (SatisfiesQuery(kvp.Key))
                    {
                        Current = (kvp.Value, kvp.Key);
                        return true;
                    }
                }

                return false;
            }

            private bool SatisfiesQuery(string entityName)
            {
                ReadOnlySpan<char> nameSegment = entityName;
                bool firstSegment = true;
                foreach (ReadOnlyMemory<char> segmentMem in EnumerateQuerySegments(_query))
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

                return true;
            }

            private SegmentEnumerable<char> EnumerateQuerySegments(string query)
                => new SegmentEnumerable<char>(query.AsMemory(), '*');
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
        private static List<ReadOnlyMemory<char>> s_querySegments
            = new List<ReadOnlyMemory<char>>();

        public static EntityQueryResult Query(this World world, string query)
            => new EntityQueryResult(world, query, s_querySegments);
    }
}
