using System;
using System.Linq;
using NitroSharp.Common;

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
