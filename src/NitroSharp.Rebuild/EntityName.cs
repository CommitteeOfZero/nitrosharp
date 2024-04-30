using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.NsScript;

namespace NitroSharp;

[DebuggerDisplay("{Value}")]
public readonly struct EntityName : IEquatable<EntityName>
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

    public bool Equals(EntityName other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is EntityName other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(EntityName left, EntityName right) => left.Equals(right);
    public static bool operator !=(EntityName left, EntityName right) => !(left == right);
}
