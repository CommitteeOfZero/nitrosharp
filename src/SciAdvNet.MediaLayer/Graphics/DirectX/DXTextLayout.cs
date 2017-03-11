using SciAdvNet.MediaLayer.Graphics.Text;
using System.Drawing;
using System;
using SharpDX.Direct2D1;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXTextLayout : TextLayout
    {
        private DXRenderContext _rc;

        private SolidColorBrush _colorBrush;
        internal SharpDX.DirectWrite.TextLayout DWriteLayout;

        internal DXTextLayout(DXRenderContext renderContext, string text, TextFormat format, float requestedWidth, float requestedHeight)
            : base(renderContext)
        {
            _rc = renderContext;
            Text = text;
            RequestedSize = new SizeF(requestedWidth, requestedHeight);

            var dwriteFormat = MlToDxTextFormat(format);
            DWriteLayout = new SharpDX.DirectWrite.TextLayout(_rc.DWriteFactory, text, dwriteFormat, requestedWidth, requestedHeight);
            _colorBrush = new SolidColorBrush(_rc.DeviceContext, SharpDX.Color.Red);
        }

        private static SharpDX.Color MlColorToDxColor(Color color) => new SharpDX.Color(color.R, color.G, color.B, color.A);

        public override string Text { get; }
        public override SizeF RequestedSize { get; }

        public override float LineSpacing
        {
            get
            {
                DWriteLayout.GetLineSpacing(out var spacingMethod, out float spacing, out float baseline);
                return spacing;
            }

            set
            {
                DWriteLayout.GetLineSpacing(out var spacingMethod, out float spacing, out float baseline);
                DWriteLayout.SetLineSpacing(spacingMethod, value, baseline);
            }
        }

        private bool set = false;
        public override void SetGlyphColor(int glyphIndex, Color color)
        {
            _colorBrush.Color = MlColorToDxColor(color);
            if (!set)
            {
                DWriteLayout.SetDrawingEffect(_colorBrush, new SharpDX.DirectWrite.TextRange(0, 1));
                set = true;
            }
            else
            {
                _colorBrush.Color = MlColorToDxColor(color);
            }
        }

        public override void Dispose()
        {
            DWriteLayout.Dispose();
            base.Dispose();
        }

        private SharpDX.DirectWrite.TextFormat MlToDxTextFormat(TextFormat format)
        {
            return new SharpDX.DirectWrite.TextFormat(_rc.DWriteFactory, format.FontFamily, MlToDxFontWeight(format.FontWeight),
                SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, format.FontSize)
            {
                WordWrapping = MlToDxWordWrapping(format.WordWrapping)
            };
        }

        private static SharpDX.DirectWrite.WordWrapping MlToDxWordWrapping(WordWrapping wordWrapping)
        {
            switch (wordWrapping)
            {
                case WordWrapping.NoWrap:
                    return SharpDX.DirectWrite.WordWrapping.NoWrap;
                case WordWrapping.Wrap:
                    return SharpDX.DirectWrite.WordWrapping.Wrap;
                case WordWrapping.WholeWord:
                default:
                    return SharpDX.DirectWrite.WordWrapping.WholeWord;
            }
        }

        private static SharpDX.DirectWrite.FontWeight MlToDxFontWeight(FontWeight fontWeight)
        {
            switch (fontWeight)
            {
                case FontWeight.Thin:
                    return SharpDX.DirectWrite.FontWeight.Thin;
                case FontWeight.Light:
                    return SharpDX.DirectWrite.FontWeight.Light;
                case FontWeight.SemiLight:
                    return SharpDX.DirectWrite.FontWeight.SemiLight;
                case FontWeight.ExtraLight:
                    return SharpDX.DirectWrite.FontWeight.ExtraLight;
                case FontWeight.Medium:
                    return SharpDX.DirectWrite.FontWeight.Medium;
                case FontWeight.Normal:
                    return SharpDX.DirectWrite.FontWeight.Normal;
                case FontWeight.Bold:
                    return SharpDX.DirectWrite.FontWeight.Bold;
                case FontWeight.SemiBold:
                    return SharpDX.DirectWrite.FontWeight.SemiBold;
                case FontWeight.ExtraBold:
                default:
                    return SharpDX.DirectWrite.FontWeight.ExtraBold;
            }
        }
    }
}
