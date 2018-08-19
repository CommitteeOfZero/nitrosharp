using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderContext
    {
        public GraphicsDevice Device { get; set; }
        public ResourceFactory ResourceFactory { get; set; }
        public Swapchain MainSwapchain { get; set; }
        public Framebuffer MainFramebuffer { get; set; }
        public CommandList MainCommandList { get; set; }

        public ShaderLibrary ShaderLibrary { get; set; }
        public RgbaTexturePool TexturePool { get; set; }
        public ResourceSetCache ResourceSetCache { get; set; }
        public FontService FontService { get; set; }

        public ViewProjection ViewProjection { get; set; }
        public RenderBucket MainBucket { get; set; }
        public QuadGeometryStream QuadGeometryStream { get; set; }
        public QuadBatcher QuadBatcher { get; set; }
       

        public TextureView WhiteTexture { get; set; }

        public Size DesignResolution { get; set; }

        public QuadBatcher CreateQuadBatcher(RenderBucket bucket, Framebuffer framebuffer)
        {
            return new QuadBatcher(Device, framebuffer, ViewProjection, bucket,
                QuadGeometryStream, ResourceSetCache, ShaderLibrary, WhiteTexture);
        }
    }
}
