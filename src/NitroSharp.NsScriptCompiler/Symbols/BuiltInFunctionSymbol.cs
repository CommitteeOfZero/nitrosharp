namespace NitroSharp.NsScriptNew.Symbols
{
    public sealed class BuiltInFunctionSymbol : Symbol
    {
        internal BuiltInFunctionSymbol(BuiltInFunction function)
        {
            Function = function;
        }

        public BuiltInFunction Function { get; }
        public override SymbolKind Kind => SymbolKind.BuiltInFunction;

        public override string ToString() => $"Built-in function '{Function.ToString()}'";
    }
}
