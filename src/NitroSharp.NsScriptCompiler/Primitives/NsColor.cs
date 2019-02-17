using System;
using System.Globalization;

namespace NitroSharp.NsScript
{
    public readonly struct NsColor
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

        public static NsColor FromConstant(BuiltInConstant constant)
        {
            switch (constant)
            {
                case BuiltInConstant.Black: return Black;
                case BuiltInConstant.White: return White;
                case BuiltInConstant.Red: return Red;
                case BuiltInConstant.Green: return Green;
                case BuiltInConstant.Blue: return Blue;

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(constant));
            }
        }

        public static NsColor FromString(string colorString)
        {
            ReadOnlySpan<char> codeStr = colorString.StartsWith('#')
                ? colorString.AsSpan(1)
                : colorString;
            if (int.TryParse(codeStr, NumberStyles.HexNumber, null, out int colorCode))
            {
                return FromRgb(colorCode);
            }
            else
            {
                if (Enum.TryParse<BuiltInConstant>(colorString, true, out var enumValue))
                {
                    return FromConstant(enumValue);
                }
            }

            throw ThrowHelper.UnexpectedValue(colorString);
        }
    }
}
