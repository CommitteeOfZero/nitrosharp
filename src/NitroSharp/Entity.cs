using System;
using System.Diagnostics;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Saving;
using NitroSharp.Utilities;

namespace NitroSharp;

internal abstract class Entity : EntityInternal, IDisposable
{
    private ArrayBuilder<Entity> _children;
    private Choice? _choice;

    protected Entity(in ResolvedEntityPath path)
    {
        Id = path.Id;
        Parent = path.Parent;
        _children = new ArrayBuilder<Entity>(0);
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
        _children.Add(child);
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

    internal readonly ref struct ChildEnumerable<T>
        where T : Entity
    {
        private readonly ReadOnlySpan<Entity> _children;

        public ChildEnumerable(ReadOnlySpan<Entity> children)
        {
            _children = children;
        }

        public ChildEnumerator<T> GetEnumerator() => new(_children);

        public T? SingleItem()
        {
            ChildEnumerator<T> enumerator = GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }
    }

    internal ref struct ChildEnumerator<T>
        where T : Entity
    {
        private readonly ReadOnlySpan<Entity> _children;
        private int _pos;
        private T? _current;

        public ChildEnumerator(ReadOnlySpan<Entity> children)
        {
            _children = children;
            _pos = 0;
            _current = null;
        }

        public T Current => _current!;

        public bool MoveNext()
        {
            _current = null;
            while (_pos < _children.Length && _current is null)
            {
                _current = _children[_pos++] as T;
            }
            return _current is not null;
        }
    }
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
        => entity is { Id.Name: MouseUsual or MouseClick  or MouseOver or MouseLeave };
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
