using System;
using System.Collections.Generic;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

namespace NitroSharp;

internal sealed class World : EntityScope
{
    private readonly List<Process> _processes = new();
    private readonly List<Entity> _newEntities = new();
    private readonly List<Entity> _deletedEntities = new();

    public Process CurrentProcess { get; private set; }
    public Process MainProcess { get; private set; }

    public void RegisterProcess(Process process, bool isMain, bool activate)
    {
        _processes.Add(process);
        if (isMain)
        {
            MainProcess = process;
        }

        if (activate)
        {
            CurrentProcess = process;
        }
    }

    public T AddEntity<T>(T entity) where T : Entity
    {
        if (entity.Parent is { } parent)
        {
            if (parent.TryGetChild(entity.Name) is { } existingEntity)
            {
                DestroyEntity(existingEntity);
            }
            ((EntityInternal)parent).AddChild(entity);
        }

        _newEntities.Add(entity);
        if (entity is Process process)
        {
            if (_processes is [])
            {
                MainProcess = process;
            }
            _processes.Add(process);
        }
        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        entity.Disable();
        if (entity.Parent is { } parent)
        {
            ((EntityInternal)parent).RemoveChild(entity);
        }
        _deletedEntities.Add(entity);
    }

    private void CommitDestroyEntity(Entity entity)
    {
        foreach (Entity child in entity.Children)
        {
            CommitDestroyEntity(child);
        }

        entity.Dispose();
        if (entity is Process process)
        {
            _processes.Remove(process);
        }
    }

    public void BeginFrame()
    {
        foreach (Entity entity in _deletedEntities)
        {
            CommitDestroyEntity(entity);
        }

        foreach (Entity entity in _newEntities)
        {
            entity.Enable();
        }

        _deletedEntities.Clear();
        _newEntities.Clear();
    }

    public Entity? Get(EntityPath entityPath)
    {
        if (!TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            return null;
        }

        parent ??= CurrentProcess.CurrentThread;
        return parent.TryGetChild(name);
    }

    public bool IsValidAlias(EntityAlias alias)
    {
        var path = EntityPath.Parse(alias.Value);
        return IsValidPath(path);
    }

    public bool IsValidPath(in EntityPath path) => TryResolvePath(path, out _, out _);

    public bool TryResolvePath(in EntityPath path, out EntityName name, out Entity? parent)
    {
        parent = null;
        name = default;

        if (path.Parts is [var singlePart])
        {
            name = EntityName.Parse(singlePart.Value);
            parent = CurrentProcess.CurrentThread;
            return true;
        }

        ReadOnlySpan<EntityPathPart> remainingParts = path.Parts.AsReadOnlySpan();
        EntityScope scope = CurrentProcess.CurrentThread;
        var results = new SmallList<Entity>();
        while (remainingParts is [var currentPart, _, ..])
        {
            var pattern = new EntityPattern(currentPart.Value, containsWildcard: false);
            scope = currentPart.IsAlias ? CurrentProcess.Aliases : scope;
            scope.Query(pattern, ref results);
            if (results is not [var singleResult]) { return false; }

            scope = parent = singleResult;
            remainingParts = remainingParts[1..];
            results.Clear();
        }

        name = EntityName.Parse(remainingParts[0].Value);
        return true;
    }

    public SmallList<Entity> Query(in EntityQuery query)
    {
        var lastResults = new SmallList<Entity>(CurrentProcess.CurrentThread ?? CurrentProcess.MainThread);
        var currentResults = new SmallList<Entity>();
        foreach (EntityQueryPart queryPart in query.Parts)
        {
            foreach (Entity entity in lastResults)
            {
                EntityScope scope = queryPart.Scope switch
                {
                    EntityQueryScope.Current => entity,
                    EntityQueryScope.MainThread => CurrentProcess.MainThread,
                    EntityQueryScope.CurrentAliases => CurrentProcess.Aliases,
                    EntityQueryScope.AllAliases => this,
                    _ => ThrowHelper.Unreachable<EntityScope>()
                };

                scope.Query(queryPart.Pattern, ref currentResults);
            }

            lastResults.Clear();
            foreach (Entity entity in currentResults)
            {
                lastResults.Add(entity);
            }
            currentResults.Clear();
        }

        return lastResults;
    }

    public void Query(EntityPattern pattern, ref SmallList<Entity> results)
    {
        foreach (Process process in _processes)
        {
            process.Query(pattern, ref results);
            process.Aliases.Query(pattern, ref results);
        }
    }

    public void Update(GameContext ctx)
    {
        CurrentProcess.Update(ctx);
    }

    public void Render(GameContext ctx)
    {
        CurrentProcess.RenderAll(ctx);
    }
}
