using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using NitroSharp.NsScript.Utilities;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript;

public readonly struct EntityQuery
{
    private readonly SmallList<EntityQueryPart> _parts;

    private EntityQuery(SmallList<EntityQueryPart> parts)
    {
        _parts = parts;
    }

    public ReadOnlySpan<EntityQueryPart> Parts => _parts.AsReadOnlySpan();

    public static EntityQuery Parse(string query) => TryParse(query)
        ?? throw new ArgumentException($"Malformed query: '{query}'", nameof(query));

    public static EntityQuery? TryParse(string query)
    {
        EntityQueryScope? prevScope = null;
        bool forceEnd = false;
        SmallList<EntityQueryPart> parts = new();
        foreach (ReadOnlySpan<char> part in query.AsSpan().Split('/'))
        {
            if (EntityQueryPart.TryParse(part) is not { } queryPart) { return null; }
            if (forceEnd) { return null; }

            switch (prevScope, queryPart.Scope)
            {
                // 'foo/<bar'
                // '@foo/<bar'
                // '<@foo/<bar'
                case (not null, EntityQueryScope.MainThread):
                // '<foo/@bar'
                // '<foo/<bar'
                // '<foo/<@bar'
                case (EntityQueryScope.MainThread, not EntityQueryScope.Current):
                    return null;
                default:
                    prevScope = queryPart.Scope;
                    break;
            }

            forceEnd = queryPart.ForceEndsQuery;
            parts.Add(queryPart);
        }

        return parts.Count > 0
            ? new EntityQuery(parts)
            : null;
    }

    public static implicit operator EntityQuery?(string query) => TryParse(query);

    public override string ToString()
        => string.Join('/', Parts.ToArray().Select(x => x.ToString()));
}

public enum EntityQueryScope
{
    // No prefix
    Current,
    // '<' prefix
    MainThread,
    // '@' prefix
    CurrentAliases,
    // '<@' prefix
    AllAliases
}

[DebuggerDisplay("{Value}")]
public readonly struct EntityPattern(string value, bool containsWildcard)
{
    public readonly string Value = value;
    public readonly bool ContainsWildcard = containsWildcard;

    public bool Match(string s)
    {
        string patternText = Value;
        int rowLength = patternText.Length + 1;
        bool[] prevRow = ArrayPool<bool>.Shared.Rent(rowLength);
        bool[] currRow = ArrayPool<bool>.Shared.Rent(rowLength);

        prevRow.AsSpan(..rowLength).Clear();
        prevRow[0] = true;
        for (int j = 1; j <= patternText.Length; j++)
        {
            if (patternText[j - 1] == '*')
            {
                prevRow[j] = prevRow[j - 1];
            }
        }

        for (int i = 1; i <= s.Length; i++)
        {
            currRow.AsSpan(..rowLength).Clear();
            for (int j = 1; j <= patternText.Length; j++)
            {
                char c = patternText[j - 1];
                bool match = false;
                if (c == '*')
                {
                    match = prevRow[j] || currRow[j - 1];
                }
                else if (c == s[i - 1])
                {
                    match = prevRow[j - 1];
                }

                currRow[j] = match;
            }

            (prevRow, currRow) = (currRow, prevRow);
        }

        bool result = prevRow[patternText.Length];
        ArrayPool<bool>.Shared.Return(prevRow);
        ArrayPool<bool>.Shared.Return(currRow);
        return result;
    }

    public static implicit operator EntityPattern(string value)
        => new(value, value.Contains('*'));
}

public readonly record struct EntityQueryPart(EntityPattern Pattern, EntityQueryScope Scope, bool ForceEndsQuery)
{
    public static EntityQueryPart? TryParse(ReadOnlySpan<char> text)
    {
        if (text.Length == 0) { return null; }
        (EntityQueryScope scope, int prefixLength) = text switch
        {
            ['<', '@', ..] => (EntityQueryScope.AllAliases, 2),
            ['<', ..] => (EntityQueryScope.MainThread, 1),
            ['@', ..] => (EntityQueryScope.CurrentAliases, 1),
            _ => (EntityQueryScope.Current, 0)
        };

        ReadOnlySpan<char> patternText = text[prefixLength..];
        bool forceEndsQuery = false;
        if (text[^1] == '>')
        {
            forceEndsQuery = true;
            patternText = patternText[..^1];
        }

        if (patternText.Length == 0) { return null; }

        bool containsWildcard = false;
        foreach (char c in patternText)
        {
            if (c is '<' or '>' or '@' or '/') { return null; }
            containsWildcard |= c == '*';
        }

        var pattern = new EntityPattern(patternText.ToString(), containsWildcard);
        return new EntityQueryPart(pattern, scope, forceEndsQuery);
    }

    public override string ToString()
    {
        string prefix = Scope switch
        {
            EntityQueryScope.AllAliases => "<@",
            EntityQueryScope.MainThread => "<",
            EntityQueryScope.CurrentAliases => "@",
            _ => ""
        };
        string postfix = ForceEndsQuery ? ">" : "";
        return $"{prefix}{Pattern.Value}{postfix}";
    }
}
