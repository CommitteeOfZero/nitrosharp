using NitroSharp.Media;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    public sealed class SystemScripts
    {
        public string Startup { get; set; } = "boot.nss";
        public string Menu { get; set; } = "sys_menu.nss";
        public string Save { get; set; } = "sys_save.nss";
        public string Load { get; set; } = "sys_load.nss";
        public string Config { get; set; } = "sys_config.nss";
        public string Backlog { get; set; } = "sys_backlog.nss";
        public string ExitConfirmation { get; set; } = "sys_close.nss";
        public string ReturnToMenu { get; set; } = "sys_reset.nss";
    }

    public readonly struct IconPathPattern
    {
        private readonly string _pattern;

        public readonly string FormatString;
        public readonly uint IconCount;

        public IconPathPattern(string pattern)
        {
            int fmtEnd = pattern.IndexOf('#');
            FormatString = pattern[..fmtEnd];
            IconCount = uint.Parse(pattern[(fmtEnd + 1)..]);
            _pattern = pattern;
        }

        public IconPathEnumerable EnumeratePaths()
            => new IconPathEnumerable(this);
    }

    public struct IconPathEnumerable
    {
        private readonly IconPathPattern _pattern;
        private int _i;

        public IconPathEnumerable(IconPathPattern pattern)
        {
            _pattern = pattern;
            _i = 1;
            Current = string.Empty;
        }

        public IconPathEnumerable GetEnumerator() => this;

        public string Current { get; private set; }

        public bool MoveNext()
        {
            if (_i == _pattern.IconCount) { return false; }
            Current = CRuntime.sprintf(_pattern.FormatString, _i);
            _i++;
            return true;
        }
    }

    public sealed class IconPathPatterns
    {
        public IconPathPatterns(
            IconPathPattern waitLine,
            IconPathPattern waitPage,
            IconPathPattern waitAuto,
            IconPathPattern backlogVoice)
        {
            WaitLine = waitLine;
            WaitPage = waitPage;
            WaitAuto = waitAuto;
            BacklogVoice = backlogVoice;
        }

        public IconPathPattern WaitLine { get; }
        public IconPathPattern WaitPage { get; }
        public IconPathPattern WaitAuto { get; }
        public IconPathPattern BacklogVoice { get; }
    }

    public sealed class Configuration
    {
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;
        public string WindowTitle { get; set; } = "Sample Text";

        public bool EnableVSync { get; set; } = true;
        public GraphicsBackend? PreferredGraphicsBackend { get; set; }
        public AudioBackend? PreferredAudioBackend { get; set; }

        public string ContentRoot { get; set; } = "Content";
        public string ScriptRoot { get; set; } = "nss";
        public bool EnableDiagnostics { get; set; }
        public SystemScripts SysScripts { get; } = new SystemScripts();

        public bool UseUtf8 { get; set; } = false;
        public bool SkipUpToDateCheck { get; set; } = false;

        public string FontFamily { get; set; } = "MS Gothic";
        public int FontSize { get; set; } = 20;

        public IconPathPatterns IconPathPatterns { get; }

        public Configuration(IconPathPatterns iconPathPatterns)
        {
            IconPathPatterns = iconPathPatterns;
        }
    }

    internal sealed class FontConfiguration
    {
        public FontConfiguration(
            FontFaceKey defaultFont,
            FontFaceKey? italicFont,
            PtFontSize defaultFontSize,
            RgbaFloat defaultTextColor,
            RgbaFloat? defaultOutlineColor,
            float rubyFontSizeMultiplier)
        {
            DefaultFont = defaultFont;
            ItalicFont = italicFont;
            DefaultFontSize = defaultFontSize;
            DefaultTextColor = defaultTextColor;
            DefaultOutlineColor = defaultOutlineColor;
            RubyFontSizeMultiplier = rubyFontSizeMultiplier;
        }

        public FontFaceKey DefaultFont { get; }
        public FontFaceKey? ItalicFont { get; }
        public PtFontSize DefaultFontSize { get; private set; }
        public RgbaFloat DefaultTextColor { get; private set; }
        public RgbaFloat? DefaultOutlineColor { get; private set; }
        public float RubyFontSizeMultiplier { get; }

        public FontConfiguration Clone() => new FontConfiguration(
            DefaultFont, ItalicFont,
            DefaultFontSize, DefaultTextColor,
            DefaultOutlineColor, RubyFontSizeMultiplier
        );

        public FontConfiguration WithDefaultSize(PtFontSize size)
        {
            DefaultFontSize = size;
            return this;
        }

        public FontConfiguration WithDefaultColor(in RgbaFloat color)
        {
            DefaultTextColor = color;
            return this;
        }

        public FontConfiguration WithOutlineColor(in RgbaFloat? color)
        {
            DefaultOutlineColor = color;
            return this;
        }
    }
}
