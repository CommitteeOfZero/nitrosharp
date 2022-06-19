using System.Numerics;

namespace NitroSharp.Text;

[Persistable]
internal sealed partial record FontSettings
{
    public FontFaceKey DefaultFont { get; init; }
    public FontFaceKey? ItalicFont { get; init; }
    public PtFontSize DefaultFontSize { get; init; }
    public Vector4 DefaultTextColor { get; init; }
    public Vector4? DefaultOutlineColor { get; init; }
    public float RubyFontSizeMultiplier { get; init; }

    public FontSettings()
    {
    }
}
