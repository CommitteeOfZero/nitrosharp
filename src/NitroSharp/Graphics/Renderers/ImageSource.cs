using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal readonly struct ImageSource
    {
        public readonly string Image;
        public readonly RectangleF SourceRectangle;

        public ImageSource(string image, in RectangleF sourceRectangle)
        {
            Image = image;
            SourceRectangle = sourceRectangle;
        }
    }
}
