using CommitteeOfZero.Nitro.Foundation.Content;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class Transition : Visual
    {
        public Visual Source { get; set; }
        public AssetRef MaskAsset { get; set; }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawTransition(this);
        }

        public override void Free(ICanvas canvas)
        {
            canvas.Free(this);
        }
    }
}
