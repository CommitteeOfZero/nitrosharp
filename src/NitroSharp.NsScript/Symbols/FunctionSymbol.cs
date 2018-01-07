using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class FunctionSymbol : InvocableSymbol
    {
        internal FunctionSymbol(string name, Function declaration, SymbolTable parameters) : base(name, declaration)
        {
            Parameters = parameters;
        }

        public SymbolTable Parameters { get; }
        public override SymbolKind Kind => SymbolKind.Function;

        public ParameterSymbol LookupParameter(string name) => Parameters.Lookup(name) as ParameterSymbol;
        public bool TryLookupParameter(string name, out ParameterSymbol parameter)
        {
            parameter = LookupParameter(name);
            return parameter != null;
        }

        public override string ToString() => $"Function '{Name}'";
    }
}
