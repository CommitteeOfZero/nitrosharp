using NitroSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp
{
    internal abstract class EntityTable
    {
        private readonly List<Row> _rows = new List<Row>(8);
        internal readonly Dictionary<ushort, ushort> _idToIndex;
        internal readonly Dictionary<ushort, ushort> _indexToId;

        private ushort _columnCount;
        private ushort _nextFreeColumn;

        public HashSet<Entity> AddedEntities = new HashSet<Entity>();

        protected EntityTable(World world, ushort initialColumnCount)
        {
            World = world;
            _columnCount = initialColumnCount;
            Parents = AddRow<Entity>();
            IsLocked = AddRow<bool>();
            _idToIndex = new Dictionary<ushort, ushort>(initialColumnCount);
            _indexToId = new Dictionary<ushort, ushort>(initialColumnCount);
        }

        public World World { get; }

        public ushort ColumnsUsed => _nextFreeColumn;

        public Row<bool> IsLocked { get; }
        public Row<Entity> Parents { get; }

        public void BeginFrame()
        {
            AddedEntities.Clear();
            foreach (Row row in _rows)
            {
                row.FlushEvents();
            }
        }

        public bool TryLookupIndex(Entity entity, out ushort index)
        {
            return _idToIndex.TryGetValue(entity.Id, out index);
        }

        public ushort LookupIndex(Entity entity)
        {
            return _idToIndex[entity.Id];
        }

        protected Row<T> AddRow<T>() where T : struct
        {
            var row = new Row<T>(this, _columnCount);
            _rows.Add(row);
            return row;
        }

        protected SystemDataRow<T> AddSystemDataRow<T>() where T : struct
        {
            var row = new SystemDataRow<T>(this, _columnCount);
            _rows.Add(row);
            return row;
        }

        protected RefTypeRow<T> AddRefTypeRow<T>() where T : class
        {
            var row = new RefTypeRow<T>(this, _columnCount);
            _rows.Add(row);
            return row;
        }

        internal void Insert(Entity entity)
        {
            ushort index = _nextFreeColumn++;
            if (index == _columnCount)
            {
                _columnCount *= 2;
                ushort newSize = _columnCount;
                foreach (Row row in _rows)
                {
                    row.Resize(newSize);
                }
            }

            _idToIndex[entity.Id] = index;
            _indexToId[index] = entity.Id;

            if (World.Kind == WorldKind.Primary)
            {
                AddedEntities.Add(entity);
            }
        }

        internal void Remove(Entity entity)
        {
            ushort index = _idToIndex[entity.Id];
            ushort lastIndex = (ushort)(_nextFreeColumn - 1);

            if (index < lastIndex)
            {
                ushort lastId = _indexToId[lastIndex];
                _idToIndex[lastId] = index;
                _indexToId[index] = lastId;

                foreach (Row row in _rows)
                {
                    row.Move(srcIndex: lastIndex, dstIndex: index);
                }
            }
            else
            {
                foreach (Row row in _rows)
                {
                    row.ResetValue(index);
                }
            }

            _idToIndex.Remove(entity.Id);
            _indexToId.Remove(lastIndex);
            _nextFreeColumn--;

            if (World.IsPrimary)
            {
                AddedEntities.Remove(entity);
            }
        }

        public void MergeChanges(EntityTable target)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].MergeChanges(target._rows[i]);
            }

            Debug_CompareIdMaps(this, target);
        }

        internal bool EntityExists(Entity entity)
        {
            return _idToIndex.ContainsKey(entity.Id);
        }

        internal abstract class Row
        {
            protected readonly EntityTable _table;

            protected Row(EntityTable table)
            {
                _table = table;
            }

            internal abstract void FlushEvents();
            internal abstract void MergeChanges(Row dstRow);
            internal abstract void ResetValue(ushort index);
            internal abstract void Move(ushort srcIndex, ushort dstIndex);
            internal abstract void Resize(ushort newSize);

            [Conditional("DEBUG")]
            [DebuggerNonUserCode]
            internal abstract void Debug_CompareTo(Row other);
        }

        internal abstract class RowBase<T> : Row
        {
            protected T[] _data;
            protected readonly HashSet<ushort> _dirtyIds;
            public bool _entireRowChanged;

            protected RowBase(EntityTable table, int initialColumnCount) : base(table)
            {
                _data = new T[initialColumnCount];
                _dirtyIds = new HashSet<ushort>();
            }

            public ushort CellsUsed => _table.ColumnsUsed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected ushort IndexOf(Entity entity) => _table.LookupIndex(entity);

            public T GetValue(ushort index) => _data[index];
            public T GetValue(Entity key) => _data[IndexOf(key)];
            public ref readonly T GetReadonlyRef(Entity entity) => ref _data[IndexOf(entity)];
            public ReadOnlySpan<T> Enumerate() => new ReadOnlySpan<T>(_data, 0, CellsUsed);

            public virtual void Set(Entity key, T value)
            {
                ushort index = IndexOf(key);
                _data[index] = value;
                ValueChanged(key.Id);
            }

            public virtual void Set(Entity key, ref T value)
            {
                ushort index = IndexOf(key);
                _data[index] = value;
                ValueChanged(key.Id);
            }

            public virtual ref T Mutate(Entity key)
            {
                ushort index = IndexOf(key);
                ValueChanged(key.Id);
                return ref _data[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ValueChanged(ushort id)
            {
                if (!_entireRowChanged)
                {
                    _dirtyIds.Add(id);
                }
            }

            public Span<T> MutateAll()
            {
                if (CellsUsed > 0)
                {
                    _entireRowChanged = true;
                }

                return new Span<T>(_data, 0, CellsUsed);
            }

            internal override void FlushEvents()
            {
            }

            internal override void MergeChanges(Row dstRow)
            {
                var other = (RowBase<T>)dstRow;
                if (_entireRowChanged && other._dirtyIds.Count > 0)
                {
                    other._dirtyIds.Clear();
                }

                if (_data.Length == other._data.Length)
                {
                    if (_entireRowChanged)
                    {
                        Array.Copy(_data, 0, other._data, 0, _data.Length);
                    }
                    else
                    {
                        var map = _table._idToIndex;
                        foreach (ushort id in _dirtyIds)
                        {
                            if (map.TryGetValue(id, out ushort index))
                            {
                                other._data[index] = _data[index];
                            }
                        }
                    }
                }
                else
                {
                    var newArray = new T[CellsUsed];
                    Array.Copy(_data, 0, newArray, 0, CellsUsed);
                    other._data = newArray;
                }

                _entireRowChanged = false;
                _dirtyIds.Clear();
            }

            internal override void ResetValue(ushort index)
            {
                _data[index] = default;
            }

            internal override void Move(ushort srcIndex, ushort dstIndex)
            {
                ref T srcRef = ref _data[srcIndex];
                _data[dstIndex] = srcRef;
                srcRef = default;
            }

            internal override void Resize(ushort newSize)
            {
                Array.Resize(ref _data, newSize);
            }

            internal override void Debug_CompareTo(Row other)
            {
                var b = (RowBase<T>)other;
                for (int i = 0; i < CellsUsed; i++)
                {
                    if (b._entireRowChanged || b._dirtyIds.Count > 0) { continue; }
                    Debug.Assert(EqualityComparer<T>.Default.Equals(_data[i], b._data[i]));
                }
            }
        }

        internal sealed class Row<T> : RowBase<T> where T : struct
        {
            public Row(EntityTable table, int initialColumnCount)
                : base(table, initialColumnCount)
            {
            }
        }

        internal sealed class SystemDataRow<T> : RowBase<T> where T : struct
        {
            public Span<T> RecycledComponents
            {
                get
                {
                    if (_recycledElements.Count == 0) { return default; }
                    ArrayUtil.EnsureCapacity(ref _recycledElementsBackbuffer, _recycledElements.Count);
                    _recycledElements.CopyTo(_recycledElementsBackbuffer);
                    int count = _recycledElements.Count;
                    _recycledElements.Clear();
                    return _recycledElementsBackbuffer.AsSpan(0, count);
                }
            }

            private readonly List<T> _recycledElements = new List<T>();
            private T[] _recycledElementsBackbuffer = new T[32];

            public SystemDataRow(EntityTable table, int initialColumnCount)
                : base(table, initialColumnCount)
            {
            }

            internal override void ResetValue(ushort index)
            {
                _recycledElements.Add(_data[index]);
                _data[index] = default;
            }

            internal override void Move(ushort srcIndex, ushort dstIndex)
            {
                _recycledElements.Add(_data[dstIndex]);
                ref T srcRef = ref _data[srcIndex];
                _data[dstIndex] = srcRef;
                srcRef = default;
            }

            internal override void FlushEvents()
            {
            }
        }

        internal sealed class RefTypeRow<T> : RowBase<T> where T : class
        {
            public RefTypeRow(EntityTable table, int initialColumnCount)
                : base(table, initialColumnCount)
            {
            }
        }

        [Conditional("DEBUG")]
        [DebuggerNonUserCode]
        internal static void Debug_CompareTables(EntityTable left, EntityTable right)
        {
            for (int i = 0; i < left._rows.Count; i++)
            {
                left._rows[i].Debug_CompareTo(right._rows[i]);
            }
        }

        [Conditional("DEBUG")]
        [DebuggerNonUserCode]
        private static void Debug_CompareIdMaps(EntityTable left, EntityTable right)
        {
            validateMaps(left);
            validateMaps(right);

            compareMaps(left._idToIndex, right._idToIndex);
            compareMaps(left._indexToId, right._indexToId);

            void validateMaps(EntityTable table)
            {
                Debug.Assert(table._idToIndex.Count == table._indexToId.Count);
                foreach (var kvp in table._idToIndex)
                {
                    Debug.Assert(table._indexToId[kvp.Value] == kvp.Key);
                }
            }

            void compareMaps<K, V>(Dictionary<K, V> l, Dictionary<K, V> r) where V : IEquatable<V>
            {
                Debug.Assert(l.Count == r.Count);
                foreach (var kvp in l)
                {
                    Debug.Assert(r.ContainsKey(kvp.Key) && r[kvp.Key].Equals(kvp.Value));
                }
            }
        }
    }
}
