using System;
using FreeTypeBindings;
using MessagePack;

namespace NitroSharp.Text
{
    [Persistable]
    internal readonly partial struct FontFaceKey : IEquatable<FontFaceKey>
    {
        public string FamilyName { get; init; }
        public FontStyle Style { get; init; }

        public FontFaceKey(string familyName, FontStyle style)
        {
            FamilyName = familyName;
            Style = style;
        }

        public bool Equals(FontFaceKey other)
            => FamilyName.Equals(other.FamilyName) && Style == other.Style;

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

        public PtFontSize(ref MessagePackReader reader)
        {
            Value = Fixed26Dot6.FromRawValue(reader.ReadInt32());
        }

        public void Serialize(ref MessagePackWriter writer)
        {
            writer.WriteInt32(Value.Value);
        }

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
        /// The recommended distance between two consecutive baselines.
        /// </summary>
        public readonly float LineHeight;
        /// <summary>
        /// The recommended gap between two lines of text,
        /// not including ascender and descender.
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
