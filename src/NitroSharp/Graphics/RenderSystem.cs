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
        private readonly InputContext _input;

        public RenderSystem(
            World world,
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            GlyphRasterizer glyphRasterizer,
            ContentManager contentManager,
            InputContext input)
        {
            Context = new RenderContext(
                gameConfiguration,
                graphicsDevice,
                swapchain,
                contentManager,
                glyphRasterizer
            );
            _world = world;
            _input = input;
        }

        public RenderContext Context { get; }

        public void Render(in FrameStamp frameStamp, float dt)
        {
            Context.BeginFrame(frameStamp);

            ReadOnlySpan<RenderItem> renderItems = _world.RenderItems.SortActive();
            foreach (RenderItem ri in renderItems)
            {
                ri.Update(_world, Context, dt);
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
