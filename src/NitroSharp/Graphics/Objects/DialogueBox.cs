using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics.Objects
{
    internal class DialogueBox : Visual
    {
        private readonly Size _bounds;
        private Texture _texture;

        public DialogueBox(in Size bounds)
        {
            _bounds = bounds;
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            var factory = renderContext.Factory;
            _texture = factory.CreateTexture(TextureDescription.Texture2D(
                    (uint)_bounds.Width, (uint)_bounds.Height, 1, 1,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        }

        public override void Render(RenderContext renderContext)
        {
            throw new NotImplementedException();
        }
    }
}
