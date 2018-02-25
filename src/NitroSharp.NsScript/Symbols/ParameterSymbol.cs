using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class ParameterSymbol : SourceSymbol
    {
        internal ParameterSymbol(string name, Parameter declaration) : base(name, declaration)
        {
        }

        public override SymbolKind Kind => SymbolKind.Parameter;
        public override string ToString() => $"Parameter '{Name}'";
    }
}
