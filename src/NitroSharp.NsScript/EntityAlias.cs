using System;

namespace NitroSharp.NsScript;

public readonly record struct EntityAlias
{
    private EntityAlias(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EntityAlias Parse(string alias) => TryParse(alias)
        ?? throw new ArgumentException($"Malformed entity alias: '{alias}'", nameof(alias));

    public static EntityAlias CreateUnsafe(string alias) => new(alias);

    public static EntityAlias? TryParse(string alias)
    {
        if (alias.Length == 0) { return null; }

        foreach (char c in alias)
        {
            if (c is '@' or '*' or '<') { return null; }
        }

        return new EntityAlias(alias);
    }

    public bool Matches(EntityPattern pattern) => pattern.Match(Value);
}
