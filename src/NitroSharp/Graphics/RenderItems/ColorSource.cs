using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ColorSource : Entity
    {
        public ColorSource(in ResolvedEntityPath path, in RgbaFloat color, Size size)
            : base(path)
        {
            Color = color;
            Size = size;
        }

        public RgbaFloat Color { get; }
        public Size Size { get; }

        public override bool IsIdle => true;
    }
}
