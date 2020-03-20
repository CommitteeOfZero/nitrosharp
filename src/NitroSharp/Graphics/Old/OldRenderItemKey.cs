using System;

namespace NitroSharp.Graphics
{
    internal readonly struct OldRenderItemKey :
        IEquatable<OldRenderItemKey>,
        IComparable<OldRenderItemKey>
    {
        public readonly ushort Priority;
        public readonly ushort EntityId;

        public OldRenderItemKey(ushort priority, ushort entityId)
        {
            Priority = priority;
            EntityId = entityId;
        }

        public int CompareTo(OldRenderItemKey other)
        {
            if (Priority > other.Priority) { return 1; }
            if (Priority < other.Priority) { return -1; }
            if (EntityId > other.EntityId) { return 1; }
            return -1;
        }

        public bool Equals(OldRenderItemKey other)
        {
            return Priority == other.Priority
                   && EntityId == other.EntityId;
        }
    }
}
