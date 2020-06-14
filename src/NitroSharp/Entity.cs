using System;
using NitroSharp.NsScript;
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
        public bool Equals(EntityId other) => string.Equals(Path, other.Path);
        public override string? ToString() => Path;
    }

    internal abstract class Entity : EntityInternal, IDisposable
    {
        private ArrayBuilder<Entity> _children;

        protected Entity(in ResolvedEntityPath path)
        {
            Id = path.Id;
            Parent = path.Parent;
            _children = new ArrayBuilder<Entity>(2);
        }

        public EntityId Id { get; }
        public Entity? Parent { get; }
        public EntityPath Alias { get; private set; }
        public bool IsLocked { get; private set; }

        public void Lock() => IsLocked = true;
        public void Unlock() => IsLocked = false;

        public abstract bool IsIdle { get; }

        protected ChildEnumerable<T> GetChildren<T>() where T : Entity
            => new ChildEnumerable<T>(_children.AsSpan());

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

        ref ArrayBuilder<Entity> EntityInternal.GetChildrenMut()
            => ref _children;

        public virtual void Dispose()
        {
        }

        internal readonly ref struct ChildEnumerable<T>
            where T : Entity
        {
            private readonly ReadOnlySpan<Entity> _children;

            public ChildEnumerable(ReadOnlySpan<Entity> children)
                => _children = children;

            public ChildEnumerator<T> GetEnumerator()
                => new ChildEnumerator<T>(_children);
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
                return _current is object;
            }
        }
    }

    internal interface EntityInternal
    {
        ref ArrayBuilder<Entity> GetChildrenMut();
        void SetAlias(in EntityPath alias);
        void AddChild(Entity child);
        void RemoveChild(Entity child);
    }
}
