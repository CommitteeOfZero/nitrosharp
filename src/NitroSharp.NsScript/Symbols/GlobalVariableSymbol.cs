namespace NitroSharp.NsScript.Symbols
{
    public sealed class GlobalVariableSymbol : Symbol
    {
        internal static readonly GlobalVariableSymbol Instance = new GlobalVariableSymbol();

        private GlobalVariableSymbol() { }
        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}
