using System;
using MoeGame.Framework.Content;
using SharpDX.Direct2D1;

namespace CommitteeOfZero.Nitro.Graphics.RenderItems
{
    public class TextureVisual : Visual
    {
        public AssetRef AssetRef { get; set; }

        public override void Render(RenderSystem renderSystem)
        {
            var content = renderSystem.Content;
            var canvas = renderSystem.RenderContext.DeviceContext;

            if (content.TryGetAsset<TextureAsset>(AssetRef, out var texture))
            {
                Width = texture.Width;
                Height = texture.Height;
                canvas.DrawBitmap(texture, Opacity, InterpolationMode.Anisotropic);
            }
        }
    }
}
