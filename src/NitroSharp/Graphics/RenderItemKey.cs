using System;

namespace NitroSharp.Graphics
{
    internal readonly struct RenderItemKey :
        IEquatable<RenderItemKey>,
        IComparable<RenderItemKey>
    {
        public readonly ushort Priority;
        public readonly ushort EntityId;

        public RenderItemKey(ushort priority, ushort entityId)
        {
            Priority = priority;
            EntityId = entityId;
        }

        public int CompareTo(RenderItemKey other)
        {
            if (Priority > other.Priority) { return 1; }
            if (Priority < other.Priority) { return -1; }

            if (EntityId > other.EntityId) { return 1; }
            return -1;
        }

        public bool Equals(RenderItemKey other)
        {
            return Priority == other.Priority
                && EntityId == other.EntityId;
        }
    }
}
