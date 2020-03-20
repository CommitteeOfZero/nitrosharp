using System;
using NitroSharp.New;

#nullable enable

namespace NitroSharp.Graphics
{
    internal readonly struct RenderItemKey : IComparable<RenderItemKey>
    {
        public readonly int Priority;
        public readonly int Id;

        public RenderItemKey(int priority, int id)
            => (Priority, Id) = (priority, id);

        public int CompareTo(RenderItemKey other)
        {
            if (Priority > other.Priority) { return 1; }
            if (Priority < other.Priority) { return -1; }
            if (Id > other.Id) { return 1; }
            return -1;
        }
    }

    interface TransitionSource
    {
    }

    internal class RenderItem : Entity, IComparable<RenderItem>
    {
        private static int s_lastId;

        public RenderItem(in ResolvedEntityPath path, int priority)
            : base(path)
        {
            Key = new RenderItemKey(priority, s_lastId++);
        }

        public RenderItemKey Key { get; }

        public int CompareTo(RenderItem other)
            => Key.CompareTo(other.Key);
    }

    internal abstract class RenderItem2D : RenderItem
    {
        protected RenderItem2D(in ResolvedEntityPath path, int priority)
            : base(in path, priority)
        {
        }
    }

    internal sealed class Sprite : RenderItem2D, TransitionSource
    {
        public Sprite(in ResolvedEntityPath path, int priority)
            : base(in path, priority)
        {
        }
    }

    internal sealed class ColorRect : RenderItem2D, TransitionSource
    {
        public ColorRect(in ResolvedEntityPath path, int priority)
            : base(in path, priority)
        {
        }
    }
}
