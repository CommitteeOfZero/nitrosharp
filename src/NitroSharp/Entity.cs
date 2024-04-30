using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Saving;
using NitroSharp.Utilities;

namespace NitroSharp;

internal abstract class Entity : EntityInternal, IDisposable
{
    private struct ChildCollection
    {
        private SmallList<Entity> _children;
        private Dictionary<string, Entity>? _childrenMap;

        public Enumerator GetEnumerator() => new(ref this);

        public ref struct Enumerator
        {
            private Dictionary<string, Entity>.ValueCollection.Enumerator _mapEnumerator;
            private Span<Entity>.Enumerator _listEnumerator;
            private readonly bool _usingMap;

            public Enumerator(ref ChildCollection collection)
            {
                if (collection._childrenMap is { } map)
                {
                    _mapEnumerator = map.Values.GetEnumerator();
                    _usingMap = true;
                }
                else
                {
                    _listEnumerator = collection._children.GetEnumerator();
                    _usingMap = true;
                }
            }

            public Entity Current => _usingMap ? _mapEnumerator.Current : _listEnumerator.Current;
            public bool MoveNext() => _usingMap ? _mapEnumerator.MoveNext() : _listEnumerator.MoveNext();
        }

        public void Add(Entity child)
        {
            if (_children.Count == SmallList<Entity>.MaxFixed)
            {
                SwitchToDictionary();
            }

            if (_childrenMap is { } map)
            {
                map.Add(child.Id.Name.ToString(), child);
            }
            else
            {
                _children.Add(child);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SwitchToDictionary()
        {
            _childrenMap = new Dictionary<string, Entity>(capacity: 16);
            foreach (Entity entity in _children.AsSpan())
            {
                _childrenMap[entity.Id.Name.ToString()] = entity;
            }

            _children.Clear();
        }

        public void Remove(Entity child)
        {
            if (_childrenMap is { } map)
            {
                map.Remove(child.Id.Name.ToString());
            }
            else
            {
                _children.Remove(child);
            }
        }

        public Entity? TryLookupFast(string name)
        {
            if (_childrenMap is { } map)
            {
                return map.TryGetValue(name, out Entity? entity) ? entity : null;
            }

            return null;
        }
    }

    private ChildCollection _children;
    private Choice? _choice;

    protected Entity(in ResolvedEntityPath path)
    {
        Id = path.Id;
        Parent = path.Parent;
        if (Parent is { } parent && this.IsMouseStateEntity())
        {
            parent.EnsureHasChoice();
        }
    }

    protected Entity(in ResolvedEntityPath path, in EntitySaveData saveData)
        : this(path)
    {
        IsLocked = saveData.IsLocked;
    }

    public EntityId Id { get; }
    public Entity? Parent { get; }
    public EntityPath Alias { get; private set; }
    public abstract EntityKind Kind { get; }
    public bool IsLocked { get; private set; }
    public abstract bool IsIdle { get; }
    public virtual UiElement? UiElement => _choice;

    public void Query(EntityQueryPart queryPart, List<Entity> results)
    {
        string pattern = queryPart.Value.ToString();
        if (_children.TryLookupFast(pattern) is { } singleEntity)
        {
            results.Add(singleEntity);
            return;
        }

        int rowLength = pattern.Length + 1;
        bool[] prevRow = ArrayPool<bool>.Shared.Rent(rowLength);
        bool[] currRow = ArrayPool<bool>.Shared.Rent(rowLength);

        foreach (Entity candidate in _children)
        {
            string name = candidate.Id.Name.ToString();

            prevRow.AsSpan(..rowLength).Fill(false);
            prevRow[0] = true;
            for (int j = 1; j <= pattern.Length; j++)
            {
                if (pattern[j - 1] == '*')
                {
                    prevRow[j] = prevRow[j - 1];
                }
            }

            for (int i = 1; i <= name.Length; i++)
            {
                currRow.AsSpan(..rowLength).Fill(false);
                for (int j = 1; j <= pattern.Length; j++)
                {
                    char c = pattern[j - 1];
                    bool match = false;
                    if (c == '*')
                    {
                        match = prevRow[j] || currRow[j - 1];
                    }
                    else if (c == name[i - 1])
                    {
                        match = prevRow[j - 1];
                    }

                    currRow[j] = match;
                }

                (prevRow, currRow) = (currRow, prevRow);
            }

            if (prevRow[pattern.Length])
            {
                results.Add(candidate);
            }
        }

        ArrayPool<bool>.Shared.Return(prevRow);
        ArrayPool<bool>.Shared.Return(currRow);
    }

    private void EnsureHasChoice()
    {
        _choice ??= new Choice(Id);
    }

    protected Entity? TryGetOwningChoice()
    {
        if (!Parent.IsMouseStateEntity()) { return null; }
        Entity result = Parent!.Parent!;
        Debug.Assert(result._choice is not null);
        return result;
    }

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;

    protected ReadOnlySpan<Entity> GetChildren() => _children.AsSpan();

    protected ChildEnumerable<T> GetChildren<T>() where T : Entity
        => new(_children.AsSpan());

    public T? GetSingleChild<T>() where T : Entity
        => new ChildEnumerable<T>(_children.AsSpan()).SingleItem();

    void EntityInternal.SetAlias(in EntityPath alias)
    {
        Alias = alias;
    }

    void EntityInternal.AddChild(Entity child)
    {



    }

    void EntityInternal.RemoveChild(Entity child)
    {
        _children.Remove(child);
    }

    ref ArrayBuilder<Entity> EntityInternal.GetChildrenMut() => ref _children;

    public virtual void Dispose()
    {
    }

    public EntitySaveData ToSaveData(GameSavingContext ctx) => new()
    {
        Id = Id,
        Parent = Parent?.Id ?? EntityId.Invalid,
        IsEnabled = ctx.World.IsEnabled(this),
        IsLocked = IsLocked,
        NextFocus = _choice?.FocusData ?? default
    };
}

internal sealed class BasicEntity : Entity
{
    public BasicEntity(in ResolvedEntityPath path) : base(path)
    {
    }

    public BasicEntity(in ResolvedEntityPath path, in BasicEntitySaveData saveData)
        : base(path, saveData.Data)
    {
    }

    public override EntityKind Kind => EntityKind.Basic;
    public override bool IsIdle => true;

    public new BasicEntitySaveData ToSaveData(GameSavingContext ctx) => new()
    {
        Data = base.ToSaveData(ctx)
    };
}

internal enum EntityKind
{
    Basic,
    Image,
    Sprite,
    AlphaMask,
    BacklogView,
    ColorSource,
    Cube,
    DialogueBox,
    DialoguePage,
    Scrollbar,
    TextBlock,
    VmThread,
    Sound,
    Video
}

internal static class MouseStateEntities
{
    public const string MouseUsual = "MouseUsual";
    public const string MouseClick = "MouseClick";
    public const string MouseOver = "MouseOver";
    public const string MouseLeave = "MouseLeave";

    public static readonly string[] All = { MouseUsual, MouseClick, MouseOver, MouseLeave };

    public static bool IsMouseStateEntity(this Entity? entity)
        => entity is { Id.Name: MouseUsual or MouseClick or MouseOver or MouseLeave };
}

internal interface EntityInternal
{
    ref ArrayBuilder<Entity> GetChildrenMut();
    void SetAlias(in EntityPath alias);
    void AddChild(Entity child);
    void RemoveChild(Entity child);
}

[Persistable]
internal readonly partial struct EntitySaveData
{
    public EntityId Id { get; init; }
    public EntityId Parent { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsLocked { get; init; }
    public UiElementFocusData NextFocus { get; init; }
}

[Persistable]
internal readonly partial struct BasicEntitySaveData : IEntitySaveData
{
    public EntitySaveData Data { get; init; }
    public EntitySaveData CommonEntityData => Data;
}

internal interface IEntitySaveData
{
    EntitySaveData CommonEntityData { get; }
}
