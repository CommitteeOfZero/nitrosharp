using System.Collections.Immutable;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class MergedSourceFileSymbol : Symbol
    {
        public MergedSourceFileSymbol(SourceFileSymbol symbol, ImmutableArray<SourceFileSymbol> dependencies)
        {
            Symbol = symbol;
            Dependencies = dependencies;
        }

        public override SymbolKind Kind => SymbolKind.MergedSourceFile;

        public SourceFileSymbol Symbol { get; }
        public ImmutableArray<SourceFileSymbol> Dependencies { get; }

        public FunctionSymbol LookupFunction(string name) => LookupMember(name) as FunctionSymbol;
        public InvocableSymbol LookupMember(string name)
        {
            if (Symbol.TryLookupMember(name, out var symbol))
            {
                return symbol;
            }

            foreach (var dependency in Dependencies)
            {
                if (dependency.TryLookupMember(name, out symbol))
                {
                    return symbol;
                }
            }

            return null;
        }

        public bool TryLookupMember(string name, out NamedSymbol symbol)
        {
            symbol = LookupMember(name);
            return symbol == null;
        }
    }
}
