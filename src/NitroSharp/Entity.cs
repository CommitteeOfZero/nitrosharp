using System;

namespace NitroSharp
{
    internal enum EntityKind : ushort
    {
        Thread,
        Sprite,
        Rectangle,
        TextBlock,
        AudioClip,
        VideoClip,
        Choice
    }

    internal readonly struct Entity : IEquatable<Entity>
    {
        public bool IsValid => Id > 0;

        public static Entity Invalid => default;

        public Entity(ushort id, EntityKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public readonly ushort Id;
        public readonly EntityKind Kind;

        public bool IsVisual
        {
            get
            {
                switch (Kind)
                {
                    case EntityKind.Sprite:
                    case EntityKind.Rectangle:
                    case EntityKind.VideoClip:
                    case EntityKind.TextBlock:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public override bool Equals(object obj) => obj is Entity other && Equals(other);
        public bool Equals(Entity other)
            => Id == other.Id && Kind == other.Kind;

        public override int GetHashCode() => HashCode.Combine(Id, Kind);
        public override string ToString() => Id > 0 ? $"Entity #{Id.ToString()}" : "InvalidEntityHandle";

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);
        public static bool operator !=(Entity left, Entity right) => !(left == right);
    }

    internal interface EntityStruct
    {
    }

    internal interface MutEntityStruct
    {
    }

    internal interface EntityStruct<T> : EntityStruct
        where T : class
    {
        T Table { get; }
        ushort Index { get; }
    }

    internal interface MutEntityStruct<T> : MutEntityStruct
        where T : class
    {
        T Table { get; }
        Entity Entity { get; }
        ushort Index { get; }
    }
}
