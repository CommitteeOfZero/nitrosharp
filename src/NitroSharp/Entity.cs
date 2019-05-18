using System;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal enum EntityKind : ushort
    {
        Thread,
        Sprite,
        Rectangle,
        Text,
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
                    case EntityKind.Text:
                    case EntityKind.VideoClip:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public override bool Equals(object obj) => obj is Entity other && Equals(other);

        public bool Equals(Entity other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode() => HashHelper.Combine((int)Id, (int)Kind);
        public override string ToString() => Id > 0 ? $"Entity #{Id.ToString()}" : "InvalidEntityHandle";

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);
        public static bool operator !=(Entity left, Entity right) => !(left == right);
    }

    internal interface EntityStruct
    {
    }

    internal interface MutableEntityStruct
    {
    }

    internal interface EntityStruct<T> : EntityStruct
        where T : class
    {
        T Table { get; }
        ushort Index { get; }
    }

    internal interface MutableEntityStruct<T> : MutableEntityStruct
        where T : class
    {
        T Table { get; }
        Entity Entity { get; }
        ushort Index { get; }
    }
}
