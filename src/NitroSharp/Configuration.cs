using NitroSharp.Media;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    public sealed class Configuration
    {
        public string ProductName { get; set; } = "NitroSharp";
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public string WindowTitle { get; set; }

        public bool EnableVSync { get; set; }
        public GraphicsBackend? PreferredGraphicsBackend { get; set; }
        public AudioBackend? PreferredAudioBackend { get; set; }

        public string ContentRoot { get; set; }
        public bool EnableDiagnostics { get; set; }
        public string StartupScript { get; set; }
        public bool UseUtf8 { get; set; } = false;
        public bool SkipUpToDateCheck { get; set; } = false;

        public string FontFamily { get; set; } = "Noto Sans CJK JP";
        public int FontSize { get; set; } = 28;
    }

    internal sealed class FontConfiguration
    {
        public FontConfiguration(
            FontKey defaultFont,
            FontKey? italicFont,
            PtFontSize defaultFontSize,
            RgbaFloat defaultTextColor,
            RgbaFloat defaultOutlineColor,
            float rubyFontSizeMultiplier)
        {
            DefaultFont = defaultFont;
            ItalicFont = italicFont;
            DefaultFontSize = defaultFontSize;
            DefaultTextColor = defaultTextColor;
            DefaultOutlineColor = defaultOutlineColor;
            RubyFontSizeMultiplier = rubyFontSizeMultiplier;
        }

        public FontKey DefaultFont { get; }
        public FontKey? ItalicFont { get; }
        public PtFontSize DefaultFontSize { get; }
        public RgbaFloat DefaultTextColor { get; }
        public RgbaFloat DefaultOutlineColor { get; }
        public float RubyFontSizeMultiplier { get; }
    }
}
