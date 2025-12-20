using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading;
using NitroSharp.Common;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Utilities;

namespace NitroSharp;

internal interface EntityScope
{
    void Query(EntityPattern pattern, ref SmallList<Entity> results);
}

internal interface EntityInternal
{
    void AddChild(Entity child);
    void RemoveChild(Entity child);
}

internal sealed class BlankEntity(EntityName name, Entity? parent) : Entity(name, parent);

[DebuggerDisplay("{GetAbsolutePath()}")]
internal abstract class Entity : EntityScope, EntityInternal, SmallLookupListEntry<EntityName>, IDisposable
{
    private SmallLookupList<EntityName, Entity> _children;

    protected Entity(EntityName name, Entity? parent)
    {
        Parent = parent;
        Name = name;
        IsEnabled = false;
        World = null!;
    }

    void EntityInternal.AddChild(Entity child)
    {
        _children.Add(child.Name, child);
    }

    void EntityInternal.RemoveChild(Entity child)
    {
        _children.Remove(child);
    }

    public EntityName Name { get; }

    public Entity? Parent { get; }

    public EntityName Key => Name;

    public bool IsEnabled { get; private set; }

    public ref readonly SmallLookupList<EntityName, Entity> Children => ref _children;

    public World World { get; internal set; }

    public Process Process
    {
        get
        {
            foreach (Entity entity in AscendantsAndSelf())
            {
                if (entity is Process process) { return process; }
            }

            ThrowHelper.ThrowUnreachable();
            return null;
        }
    }

    protected ChildOfTypeEnumerable<T> GetChildren<T>() where T : Entity => new(ref _children);
    public DescendantsAndSelfEnumerable DescendantsAndSelf(bool selfFirst = true) => new(this, selfFirst);
    public DescendantsAndSelfOfTypeEnumerable<T> DescendantsAndSelf<T>() where T : Entity => new((T)this);
    private AscendantsAndSelfEnumerable AscendantsAndSelf() => new(this);
    public DescendantEnumerable GetDescendants() => new(this);
    protected DescendantOfTypeEnumerable<T> GetDescendants<T>() where T : Entity => new(GetDescendants());

    public void ProcessSubtree<T>(Action<T> action) where T : Entity
    {
        foreach (Entity entity in DescendantsAndSelf())
        {
            if (entity is T entityToProcess)
            {
                action(entityToProcess);
            }
        }
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public virtual void Update(GameContext ctx)
    {
    }

    public void SetAlias(EntityAlias alias)
    {
        Process.Aliases.Set(this, alias);
    }


    public virtual bool Request(NsEntityAction action) => false;

    public Entity? TryGetChild(EntityName name) => _children.TryGetValue(name);

    public void Query(EntityPattern pattern, ref SmallList<Entity> results)
    {
        if (!pattern.ContainsWildcard
            && EntityName.TryParse(pattern.Value) is { } entityName
            && _children.TryGetValue(entityName) is { } singleEntity)
        {
            results.Add(singleEntity);
            return;
        }

        foreach (Entity child in _children)
        {
            if (child.Name.Matches(pattern))
            {
                results.Add(child);
            }
        }
    }

    public string GetAbsolutePath()
    {
        var sb = new StringBuilder();
        foreach (Entity entity in AscendantsAndSelf())
        {
            if (entity is Thread { IsMain: true }) { break; }

            if (entity is Process) { break; }

            if (sb.Length > 0)
            {
                sb.Insert(0, '/');
            }

            sb.Insert(0, entity.Name.Value);
        }

        return sb.ToString();
    }

    public virtual bool IsAnimationActive(AnimationKind animationKind) => false;

    public virtual void Fade(float dstOpacity, TimeSpan duration, NsEaseFunction easeFunction = NsEaseFunction.Linear)
    {
    }

    public virtual void Move(
        RenderContext ctx,
        in NsCoordinate x,
        in NsCoordinate y,
        TimeSpan duration,
        NsEaseFunction easeFunction)
    {
    }

    public virtual void Rotate(
        NsNumeric dstRotationX,
        NsNumeric dstRotationY,
        NsNumeric dstRotationZ,
        TimeSpan duration,
        NsEaseFunction easeFunction)
    {
    }

    public virtual void Scale(in Vector3 dstScale, TimeSpan duration, NsEaseFunction easeFunction)
    {
    }

    public virtual void BezierMove(
        in ProcessedBezierCurve curve,
        TimeSpan duration,
        NsEaseFunction easeFunction)
    {
    }

    public virtual void Render(GameContext ctx)
    {
    }

    public virtual void Dispose()
    {
    }

    internal struct AscendantsAndSelfEnumerable(Entity entity)
    {
        private bool _firstIteration = true;

        public Entity Current { get; private set; } = null!;

        public bool MoveNext()
        {
            if (_firstIteration)
            {
                Current = entity;
                _firstIteration = false;
                return true;
            }

            if (Current.Parent is { } parent)
            {
                Current = parent;
                return true;
            }

            Current = null!;
            return false;
        }

        public AscendantsAndSelfEnumerable GetEnumerator() => this;
    }

    internal struct DescendantsAndSelfEnumerable(Entity root, bool selfFirst)
    {
        private Entity? _self = root;
        private DescendantEnumerable _descendants = new(root);

        public Entity Current { get; private set; } = null!;

        public bool MoveNext()
        {
            if (_self is not null && selfFirst)
            {
                goto ret_self;
            }

            bool result = _descendants.MoveNext();
            Current = _descendants.Current;
            if (!result && _self is not null)
            {
                goto ret_self;
            }
            return result;

        ret_self:
            Current = _self;
            _self = null;
            return true;
        }

        public DescendantsAndSelfEnumerable GetEnumerator() => this;
    }

    internal ref struct DescendantsAndSelfOfTypeEnumerable<T>(T root)
        where T : Entity
    {
        private T? _self = root;
        private DescendantOfTypeEnumerable<T> _descendants = new(new DescendantEnumerable(root));

        public Entity Current { get; private set; } = null!;

        public bool MoveNext()
        {
            if (_self is not null)
            {
                Current = _self;
                _self = null;
                return true;
            }

            bool result = _descendants.MoveNext();
            Current = _descendants.Current;
            return result;
        }

        public DescendantsAndSelfOfTypeEnumerable<T> GetEnumerator() => this;
    }

    internal struct DescendantEnumerable
    {
        private readonly ThreadLocal<Stack<Entity>> _stack
            = new(() => new Stack<Entity>(), trackAllValues: false);

        public DescendantEnumerable(Entity root)
        {
            Stack<Entity> stack = _stack.Value!;
            stack.Clear();

            foreach (Entity child in root.Children)
            {
                stack.Push(child);
            }
        }

        public Entity Current { get; private set; } = null!;

        public bool MoveNext()
        {
            Stack<Entity> stack = _stack.Value!;
            if (!stack.TryPop(out Entity? current))
            {
                Current = null!;
                return false;
            }

            foreach (Entity child in current.Children)
            {
                stack.Push(child);
            }

            Current = current;
            return true;
        }

        public DescendantEnumerable GetEnumerator() => this;
    }

    internal ref struct DescendantOfTypeEnumerable<T>(DescendantEnumerable descendants)
        where T : Entity
    {
        private DescendantEnumerable _descendants = descendants;

        public T Current { get; private set; } = null!;

        public bool MoveNext()
        {
            do
            {
                bool movedNext = _descendants.MoveNext();
                if (!movedNext) { return false; }
            } while (_descendants.Current is not T);

            Current = (T)_descendants.Current;
            return true;
        }

        public DescendantOfTypeEnumerable<T> GetEnumerator() => this;
    }

    internal ref struct ChildOfTypeEnumerable<T> where T : Entity
    {
        private SmallLookupList<EntityName, Entity>.Enumerator _innerEnumerator;

        public ChildOfTypeEnumerable(ref SmallLookupList<EntityName, Entity> children)
        {
            _innerEnumerator = children.GetEnumerator();
            Current = null!;
        }

        public T Current { get; private set; }

        public bool MoveNext()
        {
            do
            {
                bool movedNext = _innerEnumerator.MoveNext();
                if (!movedNext) { return false; }
            } while (_innerEnumerator.Current is not T);

            Current = (T)_innerEnumerator.Current;
            return true;
        }

        public ChildOfTypeEnumerable<T> GetEnumerator() => this;
    }
}
