using System.Numerics;

namespace SciAdvNet.MediaLayer
{
    public struct RgbaValueF
    {
        private Vector4 _vector;

        public RgbaValueF(float r, float g, float b, float a)
        {
            _vector = new Vector4(r, g, b, a);
        }

        public Vector4 AsVector4() => _vector;

        internal SharpDX.Color AsSharpDXColor() => new SharpDX.Color(R, G, B, A);
        internal SharpDX.Mathematics.Interop.RawColor4 AsRawColor4() => new SharpDX.Mathematics.Interop.RawColor4(R, G, B, A);

        public static implicit operator Vector4(RgbaValueF color)
        {
            return color.AsVector4();
        }

        public static implicit operator RgbaValueF(Vector4 vector4)
        {
            return new RgbaValueF(vector4.X, vector4.Y, vector4.Z, vector4.W);
        }

        public static implicit operator SharpDX.Color(RgbaValueF color)
        {
            return color.AsSharpDXColor();
        }

        public static implicit operator RgbaValueF(SharpDX.Color color)
        {
            return new RgbaValueF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        public static implicit operator SharpDX.Mathematics.Interop.RawColor4(RgbaValueF color)
        {
            return color.AsRawColor4();
        }

        public static implicit operator RgbaValueF(SharpDX.Mathematics.Interop.RawColor4 color)
        {
            return new RgbaValueF(color.R, color.G, color.B, color.A);
        }

        public float R
        {
            get => _vector.X;
            set => _vector.X = value;
        }
        public float G
        {
            get => _vector.Y;
            set => _vector.Y = value;
        }
        public float B
        {
            get => _vector.Z;
            set => _vector.Z = value;
        }
        public float A
        {
            get => _vector.W;
            set => _vector.W = value;
        }

        public static RgbaValueF Black { get; } = new RgbaValueF(0.0f, 0.0f, 0.0f, 1.0f);
        public static RgbaValueF White { get; } = new RgbaValueF(1.0f, 1.0f, 1.0f, 1.0f);
        public static RgbaValueF Red { get; } = new RgbaValueF(1.0f, 0.0f, 0.0f, 1.0f);
        public static RgbaValueF Green { get; } = new RgbaValueF(0.0f, 1.0f, 0.0f, 1.0f);
        public static RgbaValueF Blue { get; } = new RgbaValueF(0.0f, 0.0f, 1.0f, 1.0f);

        //public static Color FromRgba(uint rgba)
        //{
        //    byte r = (byte)((rgba >> 24) & 255);
        //    byte g = (byte)((rgba >> 16) & 255);
        //    byte b = (byte)((rgba >> 8 ) & 255);
        //    byte a = (byte)(rgba & 255);
        //    return new Color(r, g, b, a);
        //}

        //public static Color FromRgb(int rgb)
        //{
        //    byte r = (byte)((rgb >> 16) & 255);
        //    byte g = (byte)((rgb >> 8) & 255);
        //    byte b = (byte)(rgb & 255);
        //    return new Color(r, g, b);
        //}
    }
}
