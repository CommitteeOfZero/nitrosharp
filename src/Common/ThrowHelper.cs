using System;
using System.IO;

namespace NitroSharp
{
    internal static class ThrowHelper
    {
        public static T UnexpectedValue<T>()
            => throw new InvalidOperationException($"Unexpected value of type {typeof(T).Name}.");

        public static void Unreachable()
            => throw new InvalidOperationException("This program location is expected to be unreachable.");

        public static T Unreachable<T>()
            => throw new InvalidOperationException("This program location is expected to be unreachable.");

        public static void ThrowOutOfRange(string paramName)
            => new ArgumentOutOfRangeException(paramName);

        public static Exception UnexpectedValue(string paramName)
            => new ArgumentException("Unexpected value.", paramName);

        public static T UnexpectedValue<T>(string paramName)
           => throw new ArgumentException("Unexpected value.", paramName);

        public static T InvalidData<T>(string message)
            => throw new InvalidDataException(message);

        public static T IllegalValue<T>(string message)
            => throw new ArgumentException(message);
    }
}
