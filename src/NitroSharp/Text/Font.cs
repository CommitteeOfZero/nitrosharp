using System;
using FreeTypeBindings;
using MessagePack;

namespace NitroSharp.Text
{
    [Persistable]
    internal readonly partial record struct FontFaceKey(string FamilyName, FontStyle Style);

    [Flags]
    internal enum FontStyle
    {
        None = 0,
        Regular = 1 << 0,
        Italic = 1 << 1,
        Bold = 1 << 2
    }

    public readonly record struct PtFontSize(Fixed26Dot6 Value)
    {
        public PtFontSize(int value) : this(Fixed26Dot6.FromInt32(value))
        {
        }

        public PtFontSize(ref MessagePackReader reader)
            : this(Fixed26Dot6.FromRawValue(reader.ReadInt32()))
        {
        }

        public void Serialize(ref MessagePackWriter writer)
        {
            writer.WriteInt32(Value.Value);
        }

        public float ToFloat() => Value.ToSingle();
        public int ToInt32() => Value.ToInt32();

        public override string? ToString() => $"{Value.ToInt32()}pt";
    }

    internal readonly record struct VerticalMetrics(float Ascender, float Descender, float LineHeight)
    {
        /// <summary>
        /// The highest point that any glyph in the font extends to above
        /// the baseline. Typically positive.
        /// </summary>
        public readonly float Ascender = Ascender;
        /// <summary>
        /// The lowest point that any glyph in the font extends to below
        /// the baseline. Typically negative.
        /// </summary>
        public readonly float Descender = Descender;
        /// <summary>
        /// The recommended distance between two consecutive baselines.
        /// </summary>
        public readonly float LineHeight = LineHeight;

        /// <summary>
        /// The recommended gap between two lines of text,
        /// not including ascender and descender.
        /// </summary>
        public float LineGap => LineHeight - Ascender + Descender;

        public VerticalMetrics Max(in VerticalMetrics other)
            => LineHeight > other.LineHeight ? this : other;
    }

    internal readonly record struct GlyphDimensions(int Top, int Left, uint Width, uint Height, float Advance);
}
