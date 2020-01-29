using System;
using System.Collections.Generic;
using System.Linq;

namespace NitroSharp.NsScript.CodeGen
{
    internal static class WellKnownSymbols
    {
        private static readonly Dictionary<string, BuiltInFunction> s_builtInFunctions;
        private static readonly Dictionary<string, BuiltInConstant> s_builtInConstants;

        static WellKnownSymbols()
        {
            s_builtInFunctions = new Dictionary<string, BuiltInFunction>();
            foreach (var function in Enum.GetValues(typeof(BuiltInFunction)).Cast<BuiltInFunction>())
            {
                s_builtInFunctions[function.ToString()] = function;
            }

            s_builtInConstants = new Dictionary<string, BuiltInConstant>(StringComparer.OrdinalIgnoreCase);
            foreach (var enumValue in Enum.GetValues(typeof(BuiltInConstant)).Cast<BuiltInConstant>())
            {
                s_builtInConstants[enumValue.ToString()] = enumValue;
            }
        }

        public static BuiltInFunction? LookupBuiltInFunction(string name)
        {
            name = FixKnownTypos(name);
            return s_builtInFunctions.TryGetValue(name, out BuiltInFunction function)
                ? function : (BuiltInFunction?)null;
        }

        public static BuiltInConstant? LookupBuiltInConstant(string name)
        {
            name = FixKnownTypos(name);
            return s_builtInConstants.TryGetValue(name, out BuiltInConstant value)
                ? value : (BuiltInConstant?)null;
        }

        private static string FixKnownTypos(string s)
        {
            switch (s)
            {
                case "Waitkey":
                    return "WaitKey";
                case "Wai":
                case "Wat":
                case "Waite":
                    return "Wait";
                case "Reqiest":
                    return "Request";
                default:
                    return s;
            }
        }
    }
}
