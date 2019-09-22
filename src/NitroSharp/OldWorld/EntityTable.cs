using NitroSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NitroSharp
{
    internal abstract class EntityTable
    {
        internal struct DummyEntityStruct
        {
            public EntityTable Table;
            public ushort Index;
        }

        private readonly List<Row> _rows = new List<Row>(8);
        private readonly Dictionary<ushort, ushort> _idToIndex;

        private OldEntity[] _entities;

        private ushort _capacity;
        private ushort _nextFreeColumn;

        private readonly HashSet<OldEntity> _newEntities;
        public ReadOnlyHashSet<OldEntity> NewEntities { get; }

        protected EntityTable(OldWorld world, ushort initialColumnCount)
        {
            World = world;
            _capacity = initialColumnCount;
            Parents = AddRow<OldEntity>();
            IsLocked = AddRow<bool>();
            _idToIndex = new Dictionary<ushort, ushort>(initialColumnCount);
            _entities = new OldEntity[initialColumnCount];
            _newEntities = new HashSet<OldEntity>();
            NewEntities = new ReadOnlyHashSet<OldEntity>(_newEntities);
        }

        public OldWorld World { get; }

        public uint Capacity => _capacity;
        public ushort EntryCount => _nextFreeColumn;

        public Row<bool> IsLocked { get; }
        public Row<OldEntity> Parents { get; }

        public ReadOnlySpan<OldEntity> Entities => _entities.AsSpan(0, _nextFreeColumn);

        public bool TryLookupIndex(OldEntity entity, out ushort index)
        {
            return _idToIndex.TryGetValue(entity.Id, out index);
        }

        public ushort LookupIndex(OldEntity entity)
        {
            return _idToIndex[entity.Id];
        }

        public T Get<T>(OldEntity entity) where T : EntityStruct
        {
            var entityStruct = new DummyEntityStruct
            {
                Table = this,
                Index = LookupIndex(entity)
            };
            return Unsafe.As<DummyEntityStruct, T>(ref entityStruct);
        }

        internal void FlushFrameEvents()
        {
            _newEntities.Clear();
        }

        protected Row<T> AddRow<T>()
        {
            var row = new Row<T>(this, _capacity);
            _rows.Add(row);
            return row;
        }

        protected SystemDataRow<T> AddSystemDataRow<T>() where T : struct
        {
            var row = new SystemDataRow<T>(this, _capacity);
            _rows.Add(row);
            return row;
        }

        internal void Insert(OldEntity entity)
        {
            ushort index = _nextFreeColumn++;
            if (index == _capacity)
            {
                _capacity *= 2;
                ushort newSize = _capacity;
                Array.Resize(ref _entities, newSize);
                foreach (Row row in _rows)
                {
                    row.Resize(newSize);
                }
            }

            _idToIndex[entity.Id] = index;
            _entities[index] = entity;
            _newEntities.Add(entity);
        }

        internal void Remove(OldEntity entity)
        {
            ushort index = _idToIndex[entity.Id];
            ushort lastIndex = (ushort)(_nextFreeColumn - 1);
            if (index < lastIndex)
            {
                OldEntity lastEntity = _entities[lastIndex];
                _idToIndex[lastEntity.Id] = index;
                _entities[index] = lastEntity;
                foreach (Row row in _rows)
                {
                    if (row.IsSystemDataRow) continue;
                    row.MoveValue(srcIndex: lastIndex, dstIndex: index);
                }
            }
            else
            {
                foreach (Row row in _rows)
                {
                    if (row.IsSystemDataRow) continue;
                    row.EraseValue(index);
                }
            }

            _idToIndex.Remove(entity.Id);
            _entities[lastIndex] = OldEntity.Invalid;
            _nextFreeColumn--;
        }

        internal bool EntityExists(OldEntity entity)
        {
            return _idToIndex.ContainsKey(entity.Id);
        }

        public ref struct Enumerator<T, S>
            where T : EntityTable
            where S : EntityStruct<T>
        {
            public S _current;
            private readonly ushort _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(T entityTable)
            {
                var mut = new DummyEntityStruct
                {
                    Index = ushort.MaxValue,
                    Table = entityTable
                };
                _current = Unsafe.As<DummyEntityStruct, S>(ref mut);
                _count = _current.Table.EntryCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ref DummyEntityStruct mut = ref Unsafe.As<S, DummyEntityStruct>(ref _current);
                return ++mut.Index < _count;
            }

            public S Current => _current;
        }

        internal abstract class Row
        {
            protected readonly EntityTable _table;

            protected Row(EntityTable table)
            {
                _table = table;
            }

            internal virtual bool IsSystemDataRow => false;

            internal abstract void EraseValue(ushort index);
            internal abstract void MoveValue(ushort srcIndex, ushort dstIndex);
            internal abstract void Resize(ushort newSize);
        }

        internal class Row<T> : Row
        {
            protected T[] _data;

            public Row(EntityTable table, int initialColumnCount) : base(table)
            {
                _data = new T[initialColumnCount];
            }

            public ushort CellsUsed => _table.EntryCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected ushort IndexOf(OldEntity entity) => _table.LookupIndex(entity);

            public ref T GetRef(ushort index) => ref _data[index];
            public ref T GetRef(OldEntity key) => ref _data[IndexOf(key)];
            public ReadOnlySpan<T> Enumerate() => new ReadOnlySpan<T>(_data, 0, CellsUsed);

            public void Set(OldEntity key, T value)
            {
                ushort index = IndexOf(key);
                _data[index] = value;
            }

            public void Set(OldEntity key, ref T value)
            {
                ushort index = IndexOf(key);
                _data[index] = value;
            }

            public Span<T> MutateAll()
            {
                return new Span<T>(_data, 0, CellsUsed);
            }

            internal override void EraseValue(ushort index)
            {
                _data[index] = default;
            }

            internal override void MoveValue(ushort srcIndex, ushort dstIndex)
            {
                ref T srcRef = ref _data[srcIndex];
                _data[dstIndex] = srcRef;
                srcRef = default;
            }

            internal override void Resize(ushort newSize)
            {
                Array.Resize(ref _data, newSize);
            }
        }

        internal sealed class SystemDataRow<T> : Row<T> where T : struct
        {
            internal override bool IsSystemDataRow => true;

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

            internal override void EraseValue(ushort index)
            {
                _recycledElements.Add(_data[index]);
                _data[index] = default;
            }

            internal override void MoveValue(ushort srcIndex, ushort dstIndex)
            {
                _recycledElements.Add(_data[dstIndex]);
                ref T srcRef = ref _data[srcIndex];
                _data[dstIndex] = srcRef;
                srcRef = default;
            }
        }
    }
}
