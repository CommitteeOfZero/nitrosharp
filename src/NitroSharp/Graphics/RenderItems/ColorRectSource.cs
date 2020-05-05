using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ColorRectSource : Entity
    {
        public ColorRectSource(in ResolvedEntityPath path, in RgbaFloat color) : base(path)
        {
            Color = color;
        }

        public RgbaFloat Color { get; }
    }
}
