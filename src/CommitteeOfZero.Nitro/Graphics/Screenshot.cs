namespace CommitteeOfZero.Nitro.Graphics
{
    public class Screenshot : Visual
    {
        public void Take(INitroRenderer renderer)
        {
            renderer.CaptureScreen();
        }

        public override void Render(INitroRenderer renderer)
        {
            renderer.DrawScreenshot(this);
        }
    }
}
