using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp
{
    internal abstract class EntityTable
    {
        private readonly List<Row> _rows = new List<Row>(8);
        private readonly Queue<ushort> _freeColumns = new Queue<ushort>();

        protected EntityTable(ushort columnCount)
        {
            ColumnCount = columnCount;
            Parents = AddRow<Entity>();
            IsLocked = AddRow<bool>();
        }

        public ushort ColumnsUsed { get; private set; }
        public ushort ColumnCount { get; private set; }

        public Row<bool> IsLocked { get; }
        public Row<Entity> Parents { get; }

        protected Row<T> AddRow<T>() where T : struct
        {
            var row = new Row<T>(ColumnCount);
            _rows.Add(row);
            return row;
        }

        protected RefTypeRow<T> AddRefTypeRow<T>() where T : class
        {
            var row = new RefTypeRow<T>(ColumnCount);
            _rows.Add(row);
            return row;
        }

        internal ushort ReserveColumn()
        {
            if (_freeColumns.Count > 0)
            {
                return _freeColumns.Dequeue();
            }

            foreach (Row row in _rows)
            {
                row.ReserveCell();
            }

            return ColumnsUsed++;
        }

        internal void FreeColumn(Entity entity, bool eraseCells)
        {
            _freeColumns.Enqueue(entity.Index);
            if (eraseCells)
            {
                foreach (Row row in _rows)
                {
                    row.EraseCell(entity);
                }
            }
        }

        public void CopyChanges(EntityTable other)
        {
            Debug.Assert(_rows.Count == other._rows.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].CopyChanges(other._rows[i]);
            }

            other.ColumnsUsed = ColumnsUsed;
            other._freeColumns.Clear();
            foreach (ushort column in _freeColumns)
            {
                other._freeColumns.Enqueue(column);
            }
        }

        internal abstract class Row
        {
            internal abstract void CopyChanges(Row dstRow);
            internal abstract void ReserveCell();
            internal abstract void EraseCell(Entity entity);
        }

        internal abstract class RowBase<T> : Row
        {
            protected T[] _data;
            protected readonly HashSet<(ushort start, ushort length)> _dirtyColumns;

            protected RowBase(int initialColumnCount)
            {
                _data = new T[initialColumnCount];
                _dirtyColumns = new HashSet<(ushort start, ushort length)>();
            }

            public ushort ColumnsUsed { get; private set; }

            public T GetValue(Entity key) => _data[key.Index];
            public ref readonly T GetReadonlyRef(Entity entity) => ref _data[entity.Index];
            public ReadOnlySpan<T> Enumerate() => new ReadOnlySpan<T>(_data, 0, ColumnsUsed);

            public void Set(Entity key, T value)
            {
                _dirtyColumns.Add((key.Index, 1));
                _data[key.Index] = value;
            }

            public void Set(Entity key, ref T value)
            {
                _dirtyColumns.Add((key.Index, 1));
                _data[key.Index] = value;
            }

            public ref T Mutate(Entity key)
            {
                _dirtyColumns.Add((key.Index, 1));
                return ref _data[key.Index];
            }

            public Span<T> MutateAll()
            {
                if (ColumnsUsed > 0)
                {
                    _dirtyColumns.Add((0, ColumnsUsed));
                }

                return new Span<T>(_data, 0, ColumnsUsed);
            }

            internal override void CopyChanges(Row dstRow)
            {
                var other = (RowBase<T>)dstRow;
                if (_data.Length == other._data.Length)
                {
                    foreach ((ushort start, ushort length) in _dirtyColumns)
                    {
                        if (length == 1)
                        {
                            other._data[start] = _data[start];
                        }
                        else
                        {
                            Array.Copy(_data, start, other._data, start, length);
                        }
                    }
                }
                else
                {
                    var newArray = new T[ColumnsUsed];
                    Array.Copy(_data, 0, newArray, 0, ColumnsUsed);
                    other._data = newArray;
                }

                other.ColumnsUsed = ColumnsUsed;
                _dirtyColumns.Clear();
            }

            internal override void ReserveCell()
            {
                if (_data.Length == ColumnsUsed)
                {
                    Array.Resize(ref _data, ColumnsUsed * 2);
                }

                ColumnsUsed++;
            }

            internal override void EraseCell(Entity entity)
            {
                _dirtyColumns.Add((entity.Index, 1));
                _data[entity.Index] = default;
            }
        }

        internal sealed class Row<T> : RowBase<T> where T : struct
        {
            public Row(int initialColumnCount) : base(initialColumnCount)
            {
            }
        }

        internal sealed class RefTypeRow<T> : RowBase<T> where T : class
        {
            public RefTypeRow(int initialColumnCount) : base(initialColumnCount)
            {
            }
        }
    }
}
