using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NitroSharp.Common;

namespace NitroSharp.Utilities;

internal interface SmallLookupListEntry<TKey>
{
    TKey Key { get; }
}

internal struct SmallLookupList<TKey, TValue>
    where TKey : IEquatable<TKey>
    where TValue : SmallLookupListEntry<TKey>
{
    private SmallList<TValue> _list;
    private Dictionary<TKey, TValue>? _map;

    public Enumerator GetEnumerator() => new(ref this);

    public ref struct Enumerator
    {
        private Dictionary<TKey, TValue>.ValueCollection.Enumerator _mapEnumerator;
        private Span<TValue>.Enumerator _listEnumerator;
        private readonly bool _usingMap;

        public Enumerator(scoped ref SmallLookupList<TKey, TValue> collection)
        {
            if (collection._map is { } map)
            {
                _mapEnumerator = map.Values.GetEnumerator();
                _usingMap = true;
            }
            else
            {
                _listEnumerator = collection._list.GetEnumerator();
                _usingMap = false;
            }
        }

        public TValue Current => _usingMap ? _mapEnumerator.Current : _listEnumerator.Current;
        public bool MoveNext() => _usingMap ? _mapEnumerator.MoveNext() : _listEnumerator.MoveNext();
    }

    public void Add(TKey key, TValue value)
    {
        if (_list.Count == SmallList<Entity>.MaxFixed)
        {
            SwitchToDictionary();
        }

        if (_map is { } map)
        {
            map.Add(key, value);
        }
        else
        {
            _list.Add(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SwitchToDictionary()
    {
        _map = new Dictionary<TKey, TValue>(capacity: 16);
        foreach (TValue value in _list.AsSpan())
        {
            _map[value.Key] = value;
        }

        _list.Clear();
    }

    public void Remove(TValue value)
    {
        if (_map is { } map)
        {
            map.Remove(value.Key);
        }
        else
        {
            _list.Remove(value);
        }
    }

    public TValue? TryGetValue(TKey key)
    {
        if (_map is { } map)
        {
            return map.GetValueOrDefault(key);
        }

        foreach (TValue entry in _list)
        {
            if (entry.Key.Equals(key))
            {
                return entry;
            }
        }

        return default;
    }
}
