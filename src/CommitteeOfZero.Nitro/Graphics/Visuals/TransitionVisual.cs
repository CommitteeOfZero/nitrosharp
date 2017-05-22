using MoeGame.Framework.Content;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class TransitionVisual : Visual
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
