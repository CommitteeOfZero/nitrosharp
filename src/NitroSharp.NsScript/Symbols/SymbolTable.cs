using System;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class SymbolTable
    {
        public static readonly SymbolTable Empty = new SymbolTable();

        private readonly Dictionary<string, NamedSymbol> _symbols;

        public SymbolTable(bool ignoreCase = false)
        {
            var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _symbols = new Dictionary<string, NamedSymbol>(comparer);
        }

        public void Declare(IEnumerable<NamedSymbol> symbols)
        {
            ThrowIfEmptyInstance();
            foreach (var symbol in symbols)
            {
                _symbols[symbol.Name] = symbol;
            }
        }

        public void Declare(NamedSymbol symbol)
        {
            ThrowIfEmptyInstance();
            _symbols[symbol.Name] = symbol;
        }

        public NamedSymbol Lookup(string name) => _symbols.TryGetValue(name, out var symbol) ? symbol : null;
        public bool TryLookup(string name, out NamedSymbol symbol) => _symbols.TryGetValue(name, out symbol);

        private void ThrowIfEmptyInstance()
        {
            if (ReferenceEquals(this, Empty))
            {
                throw new InvalidOperationException();
            }
        }

        public int Count => _symbols.Count;
    }
}
