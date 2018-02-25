using System;

namespace NitroSharp.NsScript
{
    internal static class ExceptionUtils
    {
        public static Exception UnexpectedValue(string paramName) => new ArgumentException("Unexpected value.", paramName);
    }
}
