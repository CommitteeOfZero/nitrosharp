using NitroSharp.Content;
using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal readonly struct ImageSource
    {
        public readonly AssetId Image;
        public readonly RectangleF SourceRectangle;

        public ImageSource(AssetId image, in RectangleF sourceRectangle)
        {
            Image = image;
            SourceRectangle = sourceRectangle;
        }
    }
}
