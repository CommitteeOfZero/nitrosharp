using System.Collections.Generic;
using NitroSharp.Text;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderContext
    {
        private readonly Queue<CommandList> _commandListPool = new Queue<CommandList>();

        public GraphicsDevice Device { get; set; }
        public ResourceFactory ResourceFactory { get; set; }
        public Swapchain MainSwapchain { get; set; }
        public Framebuffer MainFramebuffer { get; set; }
        public CommandList MainCommandList { get; set; }

        public ShaderLibrary ShaderLibrary { get; set; }
        public TexturePool TexturePool { get; set; }
        public ResourceSetCache ResourceSetCache { get; set; }
        public GlyphRasterizer GlyphRasterizer { get; set; }

        public ViewProjection ViewProjection { get; set; }
        public RenderBucket<RenderItemKey> MainBucket { get; set; }
        public QuadBatcher QuadBatcher { get; set; }
       
        public Texture WhiteTexture { get; set; }

        public Size DesignResolution { get; set; }
        public TextureCache TextureCache { get; internal set; }

        public CommandList GetFreeCommandList()
        {
            return _commandListPool.Count > 0
                ? _commandListPool.Dequeue()
                : ResourceFactory.CreateCommandList();
        }

        public void FreeCommandList(CommandList commandList)
        {
            _commandListPool.Enqueue(commandList);
        }

        public QuadBatcher CreateQuadBatcher(RenderBucket<RenderItemKey> bucket, Framebuffer framebuffer)
        {
            return new QuadBatcher(Device, framebuffer, ViewProjection, bucket,
                ResourceSetCache, ShaderLibrary, WhiteTexture);
        }
    }
}
