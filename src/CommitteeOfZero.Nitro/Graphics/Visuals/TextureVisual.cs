using MoeGame.Framework.Content;
using System.Drawing;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class TextureVisual : Visual
    {
        public AssetRef AssetRef { get; set; }
        public RectangleF? SourceRectangle { get; set; }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawTexture(this);
        }

        public override void Free(ICanvas canvas)
        {
            canvas.Free(this);
        }
    }
}
