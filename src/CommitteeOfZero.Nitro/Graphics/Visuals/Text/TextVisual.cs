namespace CommitteeOfZero.Nitro.Graphics
{
    public class TextVisual : Visual
    {
        public string Text { get; set; }
        public float AnimatedOpacity { get; set; }

        public TextRange VisibleRegion { get; set; }
        public TextRange AnimatedRegion { get; set; }

        public void Reset()
        {
            AnimatedOpacity = 0.0f;
        }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawText(this);
        }

        public override void Free(ICanvas canvas)
        {
            canvas.Free(this);
        }
    }
}
