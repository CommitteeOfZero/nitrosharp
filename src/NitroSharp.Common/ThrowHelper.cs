using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NitroSharp;

public static class ThrowHelper
{
    public static Exception UnexpectedValueOf<T>()
        => new($"Unexpected value of type {typeof(T).Name}.");

    [DoesNotReturn]
    public static void ThrowUnreachable()
        => throw Unreachable();

    public static Exception Unreachable()
        => new InvalidOperationException("This program location is expected to be unreachable.");

    public static Exception ArgumentInvalid(string paramName)
        => new ArgumentException($"Argument for parameter '{paramName}' is invalid.", paramName);

    [DoesNotReturn]
    public static void ThrowArgumentInvalid(string paramName)
        => throw ArgumentInvalid(paramName);

    public static Exception ArgumentOutOfRange(string paramName)
        => new ArgumentOutOfRangeException(paramName);

    [DoesNotReturn]
    public static void ThrowArgumentOutOfRange(string paramName)
        => throw new ArgumentOutOfRangeException(paramName);

    [DoesNotReturn]
    public static T ThrowInvalidData<T>(string message)
        => throw new InvalidDataException(message);
}
