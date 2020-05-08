using System;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp
{
    internal readonly struct EntityId : IEquatable<EntityId>
    {
        private readonly int _hashCode;
        private readonly int _nameStart;

        public readonly string Path;
        public readonly MouseState MouseState;

        public EntityId(string path, int nameStart, MouseState mouseState)
        {
            Path = path;
            _hashCode = path.GetHashCode();
            _nameStart = nameStart;
            MouseState = mouseState;
        }

        public static EntityId Invalid => default;

        public ReadOnlySpan<char> Name => Path.AsSpan(_nameStart);
        public bool IsValid => Path != null;

        public override int GetHashCode() => _hashCode;
        public bool Equals(EntityId other) => Path.Equals(other.Path);
        public override string? ToString() => Path;
    }

    internal abstract class Entity : EntityInternal, IDisposable
    {
        private SmallList<EntityId> _children;

        protected Entity(in ResolvedEntityPath path)
        {
            Id = path.Id;
            Parent = path.ParentId;
            _children = new SmallList<EntityId>();
        }

        public EntityId Id { get; }
        public EntityId Parent { get; }
        public EntityPath Alias { get; private set; }
        public bool IsLocked { get; private set; }

        public bool HasParent => Parent.IsValid;

        public ReadOnlySpan<EntityId> Children
            => _children.AsSpan();

        public void Lock() => IsLocked = true;
        public void Unlock() => IsLocked = false;

        public abstract bool IsIdle { get; }

        void EntityInternal.SetAlias(in EntityPath alias)
        {
            Alias = alias;
        }

        void EntityInternal.AddChild(in EntityId child)
        {
            _children.Add(child);
        }

        void EntityInternal.RemoveChild(in EntityId child)
        {
            _children.Remove(child);
        }

        public virtual void Dispose()
        {
        }
    }

    internal interface EntityInternal
    {
        void SetAlias(in EntityPath alias);
        void AddChild(in EntityId child);
        void RemoveChild(in EntityId child);
    }
}
