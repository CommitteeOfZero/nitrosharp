namespace NitroSharp.NsScript.Symbols
{
    public sealed class BuiltInEnumValueSymbol : NamedSymbol
    {
        internal BuiltInEnumValueSymbol(BuiltInEnumValue value) : base(value.ToString())
        {
            Value = ConstantValue.Create(value);
        }

        public ConstantValue Value { get; }
        public override SymbolKind Kind => SymbolKind.EnumValue;
    }
}
