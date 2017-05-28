using System.Drawing;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed class TextVisual : Visual
    {
        public TextVisual(string text)
        {
            Text = text;
        }

        public string Text { get; }
        public SizeF LayoutBounds { get; set; }
        public TextRange VisibleRegion { get; set; }
        public TextRange AnimatedRegion { get; set; }
        public float AnimatedOpacity { get; set; }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawText(this);
        }

        public override void Free(ICanvas canvas)
        {
            canvas.Free(this);
        }

        public override SizeF Measure()
        {
            return LayoutBounds;
        }
    }
}
