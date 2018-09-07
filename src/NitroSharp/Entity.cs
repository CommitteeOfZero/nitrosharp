using System;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal readonly struct Entity : IEquatable<Entity>
    {
        public bool IsValid => UniqueId > 0;

        public Entity(uint id, EntityKind kind, ushort column)
        {
            UniqueId = id;
            Kind = kind;
            Index = column;
        }

        public readonly uint UniqueId;
        public readonly EntityKind Kind;
        public readonly ushort Index;

        public override bool Equals(object obj) => obj is Entity other && Equals(other);

        public bool Equals(Entity other)
        {
            return UniqueId == other.UniqueId
                && Kind == other.Kind
                && Index == other.Index;
        }

        public override int GetHashCode() => HashHelper.Combine((int)UniqueId, (int)Kind, Index);
        public override string ToString() => UniqueId > 0 ? $"Entity #{UniqueId.ToString()}" : "InvalidEntityHandle";

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);
        public static bool operator !=(Entity left, Entity right) => !(left == right);
    }
}
