using System;
using System.Globalization;

namespace NitroSharp.NsScript
{
    public readonly struct NsColor
    {
        private NsColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        private static readonly (string, NsColor)[] ColorTable = {
            ("BLACK", new(0, 0, 0)),
            ("WHITE", new(255, 255, 255)),
            ("RED", new(255, 0, 0)),
            ("GREEN", new(0, 128, 0)),
            ("BLUE", new(0, 0, 255)),
            ("GRAY", new(128, 128, 128)),
        };

        public static NsColor FromRgb(uint rgb)
        {
            byte r = (byte)((rgb >> 16) & 255);
            byte g = (byte)((rgb >> 8) & 255);
            byte b = (byte)(rgb & 255);
            return new NsColor(r, g, b);
        }

        private static bool TryFromName(string name, out NsColor color)
        {
            name = name.ToUpperInvariant();
            foreach ((string key, NsColor value) in ColorTable)
            {
                if (name.StartsWith(key))
                {
                    color = value;
                    return true;
                }
            }
            color = default;
            return false;
        }

        private static uint ParseHexLax(string s)
        {
            s = s.ToUpperInvariant();
            uint result = 0;
            foreach (char c in s)
            {
                switch (c) {
                    case >= '0' and <= '9':
                        result *= 0x10;
                        result += (uint)(c - '0');
                        break;
                    case >= 'A' and <= 'F':
                        result *= 0x10;
                        result += 0xA + (uint)(c - 'A');
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public static NsColor FromString(string colorString)
        {
            if (TryFromName(colorString, out NsColor color))
            {
                return color;
            }
            uint colorCode = ParseHexLax(colorString);
            return FromRgb(colorCode);
        }
    }
}
