using System;
using System.Diagnostics.CodeAnalysis;

namespace NitroSharp.Common;

public static class Extensions
{
    public static T NotNull<T>([NotNull] this T? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(message: "Non-nullable reference expected", innerException: null);
        }

        return value;
    }
}
