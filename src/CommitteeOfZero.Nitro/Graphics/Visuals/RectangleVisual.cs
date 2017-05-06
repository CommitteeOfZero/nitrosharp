namespace CommitteeOfZero.Nitro.Graphics
{
    public class RectangleVisual : Visual
    {
        public override void Render(ICanvas canvas)
        {
            canvas.DrawRectangle(this);
        }
    }
}
