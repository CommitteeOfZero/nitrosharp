using System;

namespace NitroSharp.NsScriptNew
{
    internal static class ExceptionUtils
    {
        public static void IllegalValue(string message) => throw new ArgumentException(message);
        public static void Unreachable() => throw new InvalidOperationException("This code should be unreachable.");
        public static void ThrowOutOfRange(string paramName) => new ArgumentOutOfRangeException(paramName);
        public static Exception UnexpectedValue(string paramName) => new ArgumentException("Unexpected value.", paramName);
    }
}
