using System;

namespace NitroSharp.Primitives
{
    public readonly struct Size : IEquatable<Size>
    {
        public static readonly Size Zero = new Size(0, 0);

        public readonly uint Width;
        public readonly uint Height;

        public Size(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        public Size(uint value)
        {
            Width = Height = value;
        }

        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Size size && Equals(size);

        public override int GetHashCode()
            => HashCode.Combine(Width, Height);

        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !left.Equals(right);
    }
}
