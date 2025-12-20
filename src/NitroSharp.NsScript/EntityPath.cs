using System;
using System.Linq;
using NitroSharp.Common;

namespace NitroSharp.NsScript;

public readonly struct EntityPath
{
    private readonly SmallList<EntityPathPart> _parts;

    private EntityPath(SmallList<EntityPathPart> parts)
    {
        _parts = parts;
    }

    public ReadOnlySpan<EntityPathPart> Parts => _parts.AsReadOnlySpan();

    public static EntityPath Parse(string value)
        => TryParse(value)
            ?? throw new ArgumentException($"Malformed entity path: '{value}'", nameof(value));

    public static EntityPath? TryParse(string value)
        => EntityQuery.TryParse(value) is { } query ? FromQuery(query) : null;

    public static EntityPath? FromQuery(in EntityQuery entityQuery)
    {
        var parts = new SmallList<EntityPathPart>();
        foreach (EntityQueryPart queryPart in entityQuery.Parts)
        {
            if (EntityPathPart.FromQueryPart(queryPart) is not { } pathPart) { return null; }
            parts.Add(pathPart);
        }

        return new EntityPath(parts);
    }

    public override string ToString()
        => string.Join('/', Parts.ToArray().Select(x => x.ToString()));
}

public readonly record struct EntityPathPart(string Value, bool IsAlias)
{
    public static EntityPathPart? FromQueryPart(EntityQueryPart queryPart)
    {
        if (queryPart is not
            {
                Pattern.ContainsWildcard: false,
                Scope: EntityQueryScope.Current or EntityQueryScope.CurrentAliases
            })
        {
            return null;
        }

        bool isAlias = queryPart.Scope == EntityQueryScope.CurrentAliases;
        return new EntityPathPart(queryPart.Pattern.Value, isAlias);
    }

    public override string ToString()
    {
        string prefix = IsAlias ? "@" : "";
        return $"{prefix}{Value}";
    }
}
