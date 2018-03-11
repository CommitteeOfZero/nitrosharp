using System;
using NitroSharp.Utilities;

namespace NitroSharp.Primitives
{
    public readonly struct Size : IEquatable<Size>
    {
        public static readonly Size Zero = new Size(0, 0);

        public readonly int Width;
        public readonly int Height;

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public Size(int value)
        {
            Width = Height = value;
        }

        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Size size && Equals(size);

        public override int GetHashCode() => HashHelper.Combine(Width.GetHashCode(), Height.GetHashCode());

        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !left.Equals(right);
    }
}
