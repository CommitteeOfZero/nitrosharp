namespace CommitteeOfZero.Nitro.Graphics
{
    public class Screenshot : Visual
    {
        public void Take(ICanvas canvas)
        {
            canvas.CaptureScreen();
        }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawScreenshot(this);
        }
    }
}
