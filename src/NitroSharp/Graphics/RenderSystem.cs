using System;
using System.Diagnostics;
using NitroSharp.Content;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : IDisposable
    {
        public RenderSystem(
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
        }

        public RenderContext Context { get; }

        public void Render(World world, in FrameStamp frameStamp)
        {
            Context.BeginFrame(frameStamp);

            ReadOnlySpan<RenderItem> renderItems = world.RenderItems.SortActive();
            foreach (RenderItem ri in renderItems)
            {
                ri.Update(world, Context);
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
            Context.GraphicsDevice.WaitForIdle();
            Context.Dispose();
        }
    }
}
