namespace SciAdvNet.NSScript
{
    public struct NssColor
    {
        public NssColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public static NssColor Black { get; } = new NssColor(0, 0, 0);
        public static NssColor White { get; } = new NssColor(255, 255, 255);
        public static NssColor Red { get; } = new NssColor(255, 0, 0);
        public static NssColor Green { get; } = new NssColor(0, 255, 0);
        public static NssColor Blue { get; } = new NssColor(0, 0, 255);

        public static NssColor FromRgb(int rgb)
        {
            byte r = (byte)((rgb >> 16) & 255);
            byte g = (byte)((rgb >> 8) & 255);
            byte b = (byte)(rgb & 255);
            return new NssColor(r, g, b);
        }
    }
}
