using System;
using FreeTypeBindings;

namespace NitroSharp.Text
{
    internal readonly struct FontKey : IEquatable<FontKey>
    {
        public readonly string FamilyName;
        public readonly FontStyle Style;

        public FontKey(string familyName, FontStyle style)
        {
            FamilyName = familyName;
            Style = style;
        }

        public bool Equals(FontKey other) =>
            FamilyName.Equals(other.FamilyName) && Style == other.Style;

        public override int GetHashCode() => HashCode.Combine(FamilyName, Style);
        public override string ToString() => $"{FamilyName} {Style}";
    }

    [Flags]
    internal enum FontStyle
    {
        None = 0,
        Regular = 1 << 0,
        Italic = 1 << 1,
        Bold = 1 << 2
    }

    internal readonly struct PtFontSize : IEquatable<PtFontSize>
    {
        public readonly Fixed26Dot6 Value;

        public PtFontSize(int value) => Value = Fixed26Dot6.FromInt32(value);
        public float ToFloat() => Value.ToSingle();
        public int ToInt32() => Value.ToInt32();
        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(PtFontSize other) => Value.Equals(other.Value);

        public override string? ToString() => $"{Value.ToInt32()}pt";
    }

    internal readonly struct VerticalMetrics
    {
        public VerticalMetrics(float ascender, float descender, float lineHeight)
            => (Ascender, Descender, LineHeight) = (ascender, descender, lineHeight);

        /// <summary>
        /// The highest point that any glyph in the font extends to above
        /// the baseline. Typically positive.
        /// </summary>
        public readonly float Ascender;
        /// <summary>
        /// The lowest point that any glyph in the font extends to below
        /// the baseline. Typically negative.
        /// </summary>
        public readonly float Descender;
        /// <summary>
        /// The distance between two consecutive baselines.
        /// </summary>
        public readonly float LineHeight;
        /// <summary>
        /// The recommended distance between two lines of text.
        /// This value does not include ascender and descender.
        /// </summary>
        public float LineGap => LineHeight - Ascender + Descender;
    }

    internal readonly struct GlyphDimensions
    {
        public readonly int Top;
        public readonly int Left;
        public readonly uint Width;
        public readonly uint Height;
        public readonly float Advance;

        public GlyphDimensions(int top, int left, uint width, uint height, float advance)
        {
            Top = top;
            Left = left;
            Width = width;
            Height = height;
            Advance = advance;
        }
    }
}
