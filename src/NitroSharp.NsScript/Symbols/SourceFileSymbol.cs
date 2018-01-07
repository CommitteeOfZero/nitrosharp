using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public class SourceFileSymbol : NamedSymbol
    {
        internal SourceFileSymbol(string name, SourceFile rootNode, SymbolTable members) : base(name)
        {
            Declaration = rootNode;
            Members = members;
        }

        public override SymbolKind Kind => SymbolKind.SourceFile;
        public SourceFile Declaration { get; }
        public SymbolTable Members { get; }

        public InvocableSymbol LookupMember(string name) => Members.Lookup(name) as InvocableSymbol;
        public bool TryLookupMember(string name, out InvocableSymbol symbol)
        {
            symbol = LookupMember(name);
            return symbol != null;
        }

        public override string ToString() => $"File {Name}";
    }
}
