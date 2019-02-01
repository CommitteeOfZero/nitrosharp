using System;

namespace NitroSharp.NsScriptNew
{
    internal static class ExceptionUtils
    {
        public static T IllegalValue<T>(string message)
            => throw new ArgumentException(message);

        public static void Unreachable()
            => throw new InvalidOperationException("This program location is expected to be unreachable.");

        public static T Unreachable<T>()
            => throw new InvalidOperationException("This program location is expected to be unreachable.");

        public static void ThrowOutOfRange(string paramName)
            => new ArgumentOutOfRangeException(paramName);

        public static Exception UnexpectedValue(string paramName)
            => new ArgumentException("Unexpected value.", paramName);
    }
}
