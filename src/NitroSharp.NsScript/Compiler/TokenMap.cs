using System;
using System.Collections.Generic;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript.Compiler
{
    internal class TokenMap<T> where T : class
    {
        private readonly Dictionary<T, ushort> _itemToToken;
        private ArrayBuilder<T> _items;

        public TokenMap(uint initialCapacity = 8)
        {
            _itemToToken = new Dictionary<T, ushort>((int)initialCapacity);
            _items = new ArrayBuilder<T>(initialCapacity);
        }

        public uint Count => _items.Count;
        public ReadOnlySpan<T> AsSpan() => _items.AsReadonlySpan();

        public ushort GetOrAddToken(T item)
        {
            if (!TryGetToken(item, out ushort token))
            {
                token = AddToken(item);
            }

            return token;
        }

        public bool TryGetToken(T item, out ushort token)
            => _itemToToken.TryGetValue(item, out token);

        public ushort AddToken(T item)
        {
            ushort token = (ushort)_items.Count;
            _items.Add(item);
            _itemToToken.Add(item, token);
            return token;
        }
    }
}
