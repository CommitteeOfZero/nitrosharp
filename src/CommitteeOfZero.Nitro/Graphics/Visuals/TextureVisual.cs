using MoeGame.Framework.Content;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Drawing;

namespace CommitteeOfZero.Nitro.Graphics.Visuals
{
    public class TextureVisual : Visual
    {
        private ContentManager _content;
        public AssetRef AssetRef { get; set; }
        public RectangleF? SourceRectangle { get; set; }

        public override void Render(RenderSystem renderSystem)
        {
            _content = renderSystem.Content;
            var canvas = renderSystem.RenderContext.DeviceContext;

            if (_content.TryGetAsset<TextureAsset>(AssetRef, out var texture))
            {
                if (SourceRectangle == null)
                {
                    Width = texture.Width;
                    Height = texture.Height;
                    canvas.DrawBitmap(texture, Opacity, InterpolationMode.Anisotropic);
                }
                else
                {
                    var drawingRect = SourceRectangle.Value;
                    var srcRect = new SharpDX.RectangleF(drawingRect.X, drawingRect.Y, drawingRect.Width, drawingRect.Height);
                    var dst = new SharpDX.RectangleF(0, 0, Width, Height);
                    canvas.DrawBitmap(texture, dst, Opacity, InterpolationMode.Linear, srcRect, null);
                    //canvas.DrawImage(texture, new SharpDX.Vector2(0, 0), srcRect, InterpolationMode.Anisotropic, CompositeMode.SourceOver);
                }
            }
        }
    }
}
