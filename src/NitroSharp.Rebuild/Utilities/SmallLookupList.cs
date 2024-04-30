using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NitroSharp.Utilities;

internal interface SmallLookupListEntry<TKey>
{
    TKey Key { get; }
}

internal struct SmallLookupList<TKey, TValue>
    where TKey : IEquatable<TKey>
    where TValue : SmallLookupListEntry<TKey>
{
    private SmallList<TValue> _entries;
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
                _listEnumerator = collection._entries.GetEnumerator();
                _usingMap = false;
            }
        }

        public TValue Current => _usingMap ? _mapEnumerator.Current : _listEnumerator.Current;
        public bool MoveNext() => _usingMap ? _mapEnumerator.MoveNext() : _listEnumerator.MoveNext();
    }

    public void Add(TKey key, TValue value)
    {
        if (_entries.Count == SmallList<Entity>.MaxFixed)
        {
            SwitchToDictionary();
        }

        if (_map is { } map)
        {
            map.Add(key, value);
        }
        else
        {
            _entries.Add(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SwitchToDictionary()
    {
        _map = new Dictionary<TKey, TValue>(capacity: 16);
        foreach (TValue value in _entries.AsSpan())
        {
            _map[value.Key] = value;
        }

        _entries.Clear();
    }

    public void Remove(TValue value)
    {
        if (_map is { } map)
        {
            map.Remove(value.Key);
        }
        else
        {
            _entries.Remove(value);
        }
    }

    public TValue? TryGetValue(TKey key)
    {
        if (_map is { } map)
        {
            return map.TryGetValue(key, out TValue? value) ? value : default;
        }

        foreach (TValue entry in _entries)
        {
            if (entry.Key.Equals(key))
            {
                return entry;
            }
        }

        return default;
    }
}
