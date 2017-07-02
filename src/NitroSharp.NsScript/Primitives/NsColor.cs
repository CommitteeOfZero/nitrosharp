using System;
using System.Globalization;

namespace NitroSharp.NsScript
{
    public struct NsColor
    {
        public NsColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public static NsColor Black { get; } = new NsColor(0, 0, 0);
        public static NsColor White { get; } = new NsColor(255, 255, 255);
        public static NsColor Red { get; } = new NsColor(255, 0, 0);
        public static NsColor Green { get; } = new NsColor(0, 255, 0);
        public static NsColor Blue { get; } = new NsColor(0, 0, 255);

        public static NsColor FromRgb(int rgb)
        {
            byte r = (byte)((rgb >> 16) & 255);
            byte g = (byte)((rgb >> 8) & 255);
            byte b = (byte)(rgb & 255);
            return new NsColor(r, g, b);
        }

        public static NsColor FromString(string colorString)
        {
            if (PredefinedConstants.TryGetColor(colorString, out var color))
            {
                return color;
            }

            if (int.TryParse(colorString.Substring(1), NumberStyles.HexNumber, null, out int colorCode))
            {
                return FromRgb(colorCode);
            }
            else
            {
                throw new ArgumentException("Illegal value", nameof(colorString));
            }
        }
    }
}
