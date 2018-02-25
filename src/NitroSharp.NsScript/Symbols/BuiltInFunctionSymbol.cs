using System;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class BuiltInFunctionSymbol : NamedSymbol
    {
        internal BuiltInFunctionSymbol(string name, Func<EngineImplementationBase, Stack<ConstantValue>, ConstantValue> implementation) : base(name)
        {
            Implementation = implementation;
        }

        public Func<EngineImplementationBase, Stack<ConstantValue>, ConstantValue> Implementation { get; }
        public override SymbolKind Kind => SymbolKind.BuiltInFunction;

        public override string ToString() => $"Built-in function '{Name}'";
    }
}
