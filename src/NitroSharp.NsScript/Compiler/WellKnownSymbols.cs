using System;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Compiler;

internal static class WellKnownSymbols
{
    private static readonly Dictionary<string, BuiltInFunction> s_builtInFunctions;
    private static readonly Dictionary<string, BuiltInConstant> s_builtInConstants;

    static WellKnownSymbols()
    {
        s_builtInFunctions = new Dictionary<string, BuiltInFunction>();
        foreach (BuiltInFunction function in Enum.GetValues<BuiltInFunction>())
        {
            s_builtInFunctions[function.ToString()] = function;
        }

        s_builtInConstants = new Dictionary<string, BuiltInConstant>(StringComparer.OrdinalIgnoreCase);
        foreach (BuiltInConstant enumValue in Enum.GetValues<BuiltInConstant>())
        {
            s_builtInConstants[enumValue.ToString()] = enumValue;
        }
    }

    public static BuiltInFunction? LookupBuiltInFunction(string name)
    {
        name = FixKnownTypos(name);
        return s_builtInFunctions.TryGetValue(name, out BuiltInFunction function)
            ? function : null;
    }

    public static BuiltInConstant? LookupBuiltInConstant(string name)
    {
        name = FixKnownTypos(name);
        return s_builtInConstants.TryGetValue(name, out BuiltInConstant value)
            ? value : null;
    }

    private static string FixKnownTypos(string s)
    {
        return s;
        // TODO: check if N2S actually understands these
        return s switch
        {
            "Waitkey" => "WaitKey",
            "Wai" or "Wat" or "Waite" => "Wait",
            "Reqiest" => "Request",
            _ => s,
        };
    }
}
