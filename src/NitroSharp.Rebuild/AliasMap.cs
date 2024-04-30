using System.Collections.Generic;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

namespace NitroSharp;

internal sealed class AliasMap : EntityScope
{
    private readonly Process _process;
    private readonly Dictionary<EntityAlias, Entity> _aliases = new();

    public AliasMap(Process process)
    {
        _process = process;
    }

    public void Set(Entity entity, EntityAlias alias)
    {
        _aliases[alias] = entity;
    }

    public void Query(EntityPattern pattern, ref SmallList<Entity> results)
    {
        if (!pattern.ContainsWildcard)
        {
            var alias = EntityAlias.CreateUnsafe(pattern.Value);
            if (_aliases.TryGetValue(alias, out Entity? entity))
            {
                results.Add(entity);
            }
            return;
        }

        foreach ((EntityAlias alias, Entity id) in _aliases)
        {
            if (alias.Matches(pattern))
            {
                results.Add(id);
            }
        }
    }
}
