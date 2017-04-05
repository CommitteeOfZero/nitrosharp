using HoppyFramework.Content;
using SharpDX.Mathematics.Interop;
using SharpDX.Direct2D1;

namespace ProjectHoppy.Graphics.RenderItems
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
                var dest = new RawRectangleF(X, Y, texture.Width, texture.Height);
                canvas.DrawBitmap(texture, dest, Opacity, BitmapInterpolationMode.Linear);
            }
        }
    }
}
