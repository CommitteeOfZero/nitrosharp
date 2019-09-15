using NitroSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        internal struct DummyMutEntityStruct
        {
            public EntityTable Table;
            public Entity Entity;
            public ushort Index;
        }

        private enum LossyOperationKind
        {
            Erase,
            Move
        }

        private struct LossyOperation
        {
            public ushort Column;
            public ushort DstColumn;
            public LossyOperationKind Kind;
        }

        private readonly List<Row> _rows = new List<Row>(8);
        private readonly Dictionary<ushort, ushort> _idToIndex;
        private readonly List<LossyOperation> _lossyOperations;

        private Entity[] _entities;

        private ushort _capacity;
        private ushort _nextFreeColumn;

        private readonly HashSet<Entity> _newEntities;
        public ReadOnlyHashSet<Entity> NewEntities { get; }

        protected EntityTable(World world, ushort initialColumnCount)
        {
            World = world;
            _capacity = initialColumnCount;
            Parents = AddRow<Entity>();
            IsLocked = AddRow<bool>();
            _idToIndex = new Dictionary<ushort, ushort>(initialColumnCount);
            _entities = new Entity[initialColumnCount];
            _lossyOperations = new List<LossyOperation>();
            _newEntities = new HashSet<Entity>();
            NewEntities = new ReadOnlyHashSet<Entity>(_newEntities);
        }

        public World World { get; }

        public uint Capacity => _capacity;
        public ushort EntryCount => _nextFreeColumn;

        public Row<bool> IsLocked { get; }
        public Row<Entity> Parents { get; }

        public ReadOnlySpan<Entity> Entities => _entities.AsSpan(0, _nextFreeColumn);

        public bool TryLookupIndex(Entity entity, out ushort index)
        {
            return _idToIndex.TryGetValue(entity.Id, out index);
        }

        public ushort LookupIndex(Entity entity)
        {
            return _idToIndex[entity.Id];
        }

        public T Get<T>(Entity entity) where T : EntityStruct
        {
            var entityStruct = new DummyEntityStruct
            {
                Table = this,
                Index = LookupIndex(entity)
            };
            return Unsafe.As<DummyEntityStruct, T>(ref entityStruct);
        }

        public T GetMutable<T>(Entity entity) where T : MutEntityStruct
        {
            var entityStruct = new DummyMutEntityStruct
            {
                Table = this,
                Entity = entity,
                Index = LookupIndex(entity)
            };
            return Unsafe.As<DummyMutEntityStruct, T>(ref entityStruct);
        }

        public void RearrangeSystemComponents<T>(ref T[] componentArray, List<T> recycledComponents)
        {
            if (componentArray.Length < _capacity)
            {
                Array.Resize(ref componentArray, _capacity);
            }

            recycledComponents.Clear();
            foreach (LossyOperation op in _lossyOperations)
            {
                if (op.Kind == LossyOperationKind.Erase)
                {
                    recycledComponents.Add(componentArray[op.Column]);
                    componentArray[op.Column] = default;
                }
                else if (op.Kind == LossyOperationKind.Move)
                {
                    recycledComponents.Add(componentArray[op.DstColumn]);
                    componentArray[op.DstColumn] = componentArray[op.Column];
                    componentArray[op.Column] = default;
                }
            }
        }

        internal void FlushFrameEvents()
        {
            _newEntities.Clear();
            _lossyOperations.Clear();
        }

        protected Row<T> AddRow<T>() where T : struct
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

        protected RefTypeRow<T> AddRefTypeRow<T>() where T : class
        {
            var row = new RefTypeRow<T>(this, _capacity);
            _rows.Add(row);
            return row;
        }

        internal void Insert(Entity entity)
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

            if (World.IsPrimary)
            {
                _newEntities.Add(entity);
            }
        }

        internal void Remove(Entity entity)
        {
            ushort index = _idToIndex[entity.Id];
            ushort lastIndex = (ushort)(_nextFreeColumn - 1);
            if (index < lastIndex)
            {
                Entity lastEntity = _entities[lastIndex];
                _idToIndex[lastEntity.Id] = index;
                _entities[index] = lastEntity;
                _lossyOperations.Add(new LossyOperation
                {
                    Column = lastIndex,
                    DstColumn = index,
                    Kind = LossyOperationKind.Move
                });
                foreach (Row row in _rows)
                {
                    if (row.IsSystemDataRow) continue;
                    row.MoveValue(srcIndex: lastIndex, dstIndex: index);
                }
            }
            else
            {
                _lossyOperations.Add(new LossyOperation
                {
                    Column = index,
                    Kind = LossyOperationKind.Erase
                });
                foreach (Row row in _rows)
                {
                    if (row.IsSystemDataRow) continue;
                    row.EraseValue(index);
                }
            }

            _idToIndex.Remove(entity.Id);
            _entities[lastIndex] = Entity.Invalid;
            _nextFreeColumn--;

            if (World.IsPrimary)
            {
                _newEntities.Remove(entity);
            }
        }

        public void MergeChanges(EntityTable target)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].MergeChangesInto(target._rows[i]);
            }

            Debug_CompareIdMaps(this, target);
        }

        internal bool EntityExists(Entity entity)
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

            internal abstract void MergeChangesInto(Row dstRow);
            internal abstract void EraseValue(ushort index);
            internal abstract void MoveValue(ushort srcIndex, ushort dstIndex);
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

            public ushort CellsUsed => _table.EntryCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected ushort IndexOf(Entity entity) => _table.LookupIndex(entity);

            public ref readonly T GetValue(ushort index) => ref _data[index];
            public ref readonly T GetValue(Entity key) => ref _data[IndexOf(key)];
            public ReadOnlySpan<T> Enumerate() => new ReadOnlySpan<T>(_data, 0, CellsUsed);

            public void Set(Entity key, T value)
            {
                ushort index = IndexOf(key);
                _data[index] = value;
                ValueChanged(key.Id);
            }

            public void Set(Entity key, ref T value)
            {
                ushort index = IndexOf(key);
                _data[index] = value;
                ValueChanged(key.Id);
            }

            public ref T Mutate(ushort entityId, ushort column)
            {
                ValueChanged(entityId);
                return ref _data[column];
            }

            public ref T Mutate(Entity key)
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

            internal override void MergeChangesInto(Row dstRow)
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
                        Dictionary<ushort, ushort> map = _table._idToIndex;
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
            Debug.Assert(left._entities.SequenceEqual(right._entities));

            static void validateMaps(EntityTable table)
            {
                foreach (var kvp in table._idToIndex)
                {
                    Debug.Assert(table._entities[kvp.Value].Id == kvp.Key);
                }
            }

            static void compareMaps<K, V>(Dictionary<K, V> l, Dictionary<K, V> r) where V : IEquatable<V>
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
