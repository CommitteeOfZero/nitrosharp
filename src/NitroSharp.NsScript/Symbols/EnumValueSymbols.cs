using System;

namespace NitroSharp.NsScript.Symbols
{
    public static class EnumValueSymbols
    {
        public static SymbolTable Symbols { get; }

        static EnumValueSymbols()
        {
            Symbols = new SymbolTable(ignoreCase: true);
            foreach (BuiltInEnumValue constant in Enum.GetValues(typeof(BuiltInEnumValue)))
            {
                Symbols.Declare(new BuiltInEnumValueSymbol(constant));
            }
        }
    }
}
