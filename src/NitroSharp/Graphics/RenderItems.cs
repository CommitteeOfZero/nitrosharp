using System;
using NitroSharp.New;

namespace NitroSharp.Graphics
{
    internal class RenderItem : Entity, IComparable<RenderItem>
    {
        public RenderItem(in ResolvedEntityPath path) : base(path)
        {
        }

        public int CompareTo(RenderItem other)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class Quad : RenderItem
    {
        public Quad(in ResolvedEntityPath path) : base(path)
        {
        }
    }
}
