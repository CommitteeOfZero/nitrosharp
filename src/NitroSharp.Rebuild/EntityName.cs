using System;
using System.Runtime.CompilerServices;
using NitroSharp.NsScript;

namespace NitroSharp;

public readonly record struct EntityName
{
    public readonly string Value;

    private EntityName(string value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBannedCharacter(char c) => c is '@' or '*' or '<' or '/';

    public static EntityName Parse(string name) => TryParse(name)
        ?? throw new ArgumentException($"Malformed entity name: '{name}'", nameof(name));

    public static EntityName? TryParse(string name)
    {
        if (name.Length == 0) { return null; }

        foreach (char c in name)
        {
            if (IsBannedCharacter(c)) { return null; }
        }

        return new EntityName(name);
    }

    public static EntityName? FromPathPart(EntityPathPart pathPart)
    {
        if (pathPart.IsAlias) { return null; }

        foreach (char c in pathPart.Value)
        {
            if (c == '*') { return null; }
        }

        return new EntityName(pathPart.Value);
    }

    public static implicit operator EntityName(string name) => Parse(name);

    public bool Matches(EntityPattern pattern) => pattern.Match(Value);

    public override string ToString() => Value;
}
