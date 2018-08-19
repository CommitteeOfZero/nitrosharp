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

        public ushort UsedColumns { get; private set; }
        public ushort ColumnCount { get; private set; }

        public Row<bool> IsLocked { get; }
        public Row<Entity> Parents { get; }

        protected Row<T> AddRow<T>() where T : struct
        {
            var row = new Row<T>(ColumnCount);
            _rows.Add(row);
            return row;
        }

        internal ushort ReserveColumn()
        {
            if (_freeColumns.Count > 0)
            {
                return _freeColumns.Dequeue();
            }

            ushort col = UsedColumns++;
            foreach (Row row in _rows)
            {
                row.ReserveCell();
            }

            return col;
        }

        internal void FreeColumn(Entity entity, bool eraseCells)
        {
            ushort index = entity.Index;
            _freeColumns.Enqueue(index);
            if (eraseCells)
            {
                foreach (Row row in _rows)
                {
                    row.EraseCell(entity);
                }
            }
        }

        public  void CopyChanges(EntityTable other)
        {
            Debug.Assert(_rows.Count == other._rows.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].CopyChanges(other._rows[i]);
            }
        }

        internal abstract class Row
        {
            internal abstract void CopyChanges(Row dstRow);
            internal abstract void ReserveCell();
            internal abstract void EraseCell(Entity entity);
        }

        internal sealed class Row<T> : Row where T : struct
        {
            private T[] _data;
            private readonly HashSet<(ushort start, ushort length)> _dirtyColumns;

            public Row(int columnCount)
            {
                _data = new T[columnCount];
                _dirtyColumns = new HashSet<(ushort start, ushort length)>();
            }

            public ushort ColumnCount { get; private set; }

            public T Get(Entity key) => _data[key.Index];
            public ReadOnlySpan<T> Enumerate() => new ReadOnlySpan<T>(_data, 0, ColumnCount);

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
                if (ColumnCount > 0)
                {
                    _dirtyColumns.Add((0, ColumnCount));
                }

                return new Span<T>(_data, 0, ColumnCount);
            }

            internal override void CopyChanges(Row dstRow)
            {
                var other = (Row<T>)dstRow;
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
                    var newArray = new T[ColumnCount];
                    Array.Copy(_data, 0, newArray, 0, ColumnCount);
                    other._data = newArray;
                }

                other.ColumnCount = ColumnCount;
                _dirtyColumns.Clear();
            }

            internal override void ReserveCell()
            {
                if (_data.Length == ColumnCount)
                {
                    Array.Resize(ref _data, ColumnCount * 2);
                }

                ColumnCount++;
            }

            internal override void EraseCell(Entity entity)
            {
                _data[entity.Index] = default;
            }
        }
    }
}
