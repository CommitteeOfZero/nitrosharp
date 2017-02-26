namespace SciAdvNet.MediaLayer
{
    public struct Color
    {
        public Color(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(byte r, byte g, byte b)
            : this(r, g, b, 255)
        {
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public static Color Black { get; } = new Color(0, 0, 0);
        public static Color White { get; } = new Color(255, 255, 255);
        public static Color Red { get; } = new Color(255, 0, 0);
        public static Color Green { get; } = new Color(0, 255, 0);
        public static Color Blue { get; } = new Color(0, 0, 255);

        public static Color FromRgba(uint rgba)
        {
            byte r = (byte)((rgba >> 24) & 255);
            byte g = (byte)((rgba >> 16) & 255);
            byte b = (byte)((rgba >> 8 ) & 255);
            byte a = (byte)(rgba & 255);
            return new Color(r, g, b, a);
        }

        public static Color FromRgb(int rgb)
        {
            byte r = (byte)((rgb >> 16) & 255);
            byte g = (byte)((rgb >> 8) & 255);
            byte b = (byte)(rgb & 255);
            return new Color(r, g, b);
        }
    }
}
