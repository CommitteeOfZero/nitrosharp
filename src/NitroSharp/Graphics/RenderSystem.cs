using System;
using NitroSharp.Content;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : IDisposable
    {
        private readonly World _world;

        public RenderSystem(
            World world,
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            GlyphRasterizer glyphRasterizer,
            ContentManager contentManager)
        {
            Context = new RenderContext(
                gameConfiguration,
                graphicsDevice,
                swapchain,
                contentManager,
                glyphRasterizer
            );
            _world = world;
        }

        public RenderContext Context { get; }

        public void Render(in FrameStamp frameStamp)
        {
            Context.BeginFrame(frameStamp);

            ReadOnlySpan<RenderItem> renderItems = _world.RenderItems.SortActive();
            foreach (RenderItem ri in renderItems)
            {
                ri.LayoutPass(_world, Context);
            }
            foreach (RenderItem ri in renderItems)
            {
                ri.Render(Context);
            }

            Context.EndFrame();
            Context.Present();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
