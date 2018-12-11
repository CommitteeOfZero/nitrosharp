using System;
using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScriptNew.Symbols;

namespace NitroSharp.NsScriptNew.Binding
{
    internal sealed class GlobalBinder
    {
        private static readonly Dictionary<string, BuiltInFunctionSymbol> s_builtInFunctions;
        private static readonly Dictionary<string, BuiltInEnumValue> s_builtInEnumValues;

        static GlobalBinder()
        {
            s_builtInFunctions = new Dictionary<string, BuiltInFunctionSymbol>();
            foreach (var function in Enum.GetValues(typeof(BuiltInFunction)).Cast<BuiltInFunction>())
            {
                s_builtInFunctions[function.ToString()] = new BuiltInFunctionSymbol(function);
            }

            s_builtInEnumValues = new Dictionary<string, BuiltInEnumValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var enumValue in Enum.GetValues(typeof(BuiltInEnumValue)).Cast<BuiltInEnumValue>())
            {
                s_builtInEnumValues[enumValue.ToString()] = enumValue;
            }
        }

        public static BuiltInFunctionSymbol LookupBuiltInFunction(string name)
        {
            return s_builtInFunctions.TryGetValue(name, out BuiltInFunctionSymbol symbol)
                ? symbol : null;
        }

        public static BuiltInEnumValue? LookupBuiltInEnumValue(string name)
        {
            return s_builtInEnumValues.TryGetValue(name, out BuiltInEnumValue value)
                ? value : (BuiltInEnumValue?)null;
        }
    }
}
