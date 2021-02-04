using System;
using System.Runtime.InteropServices;
using MessagePack;
using NitroSharp.NsScript;
using NitroSharp.Saving;
using NitroSharp.Utilities;

namespace NitroSharp
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct EntityId : IEquatable<EntityId>
    {
        private readonly int _hashCode;
        private readonly int _nameStart;

        public readonly string Path;
        public readonly uint Context;
        public readonly MouseState MouseState;

        public EntityId(ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            Context = reader.ReadUInt32();
            string? value = reader.ReadString();
            if (value is not null)
            {
                var path = new EntityPath(value);
                _nameStart = 0;
                MouseState = path.MouseState;
                Path = path.Value;
                _hashCode = HashCode.Combine(Path.GetHashCode(), Context);
            }
            else
            {
                Path = null!;
                _nameStart = 0;
                Context = 0;
                _hashCode = 0;
                MouseState = default;
            }
        }

        public EntityId(
            uint context,
            string path,
            int nameStart,
            MouseState mouseState)
        {
            Context = context;
            Path = path;
            _hashCode = HashCode.Combine(path.GetHashCode(), context);
            _nameStart = nameStart;
            MouseState = mouseState;
        }

        public static EntityId Invalid => default;

        public ReadOnlySpan<char> Name => Path.AsSpan(_nameStart);
        public bool IsValid => Path is not null;

        public override int GetHashCode() => HashCode.Combine(_hashCode, Context);
        public bool Equals(EntityId other) => string.Equals(Path, other.Path);
        public override string ToString() => Path;

        public void Serialize(ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(2);
            writer.Write(Context);
            writer.Write(Path);
        }
    }

    internal abstract class Entity : EntityInternal, IDisposable
    {
        private ArrayBuilder<Entity> _children;

        protected Entity(in ResolvedEntityPath path)
        {
            Id = path.Id;
            Parent = path.Parent;
            _children = new ArrayBuilder<Entity>(0);
        }

        protected Entity(in ResolvedEntityPath path, in EntitySaveData saveData)
            : this(path)
        {
            IsLocked = saveData.IsLocked;
        }

        public EntityId Id { get; }
        public Entity? Parent { get; }
        public EntityPath Alias { get; private set; }
        public bool IsLocked { get; private set; }
        public abstract EntityKind Kind { get; }

        public void Lock() => IsLocked = true;
        public void Unlock() => IsLocked = false;

        public abstract bool IsIdle { get; }

        protected ChildEnumerable<T> GetChildren<T>() where T : Entity
            => new(_children.AsSpan());

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

        public EntitySaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Id = Id,
            Parent = Parent?.Id ?? EntityId.Invalid,
            IsEnabled = ctx.World.IsEnabled(this),
            IsLocked = IsLocked
        };

        internal readonly ref struct ChildEnumerable<T>
            where T : Entity
        {
            private readonly ReadOnlySpan<Entity> _children;

            public ChildEnumerable(ReadOnlySpan<Entity> children)
                => _children = children;

            public ChildEnumerator<T> GetEnumerator()
                => new(_children);
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
        Choice,
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
}
