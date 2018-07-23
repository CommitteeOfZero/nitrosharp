using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderContext
    {
        internal RenderContext(GraphicsDevice device, ResourceFactory factory,
            CommandList commandList, PrimitiveBatcher primitiveBatch,
            RgbaTexturePool texturePool, FontService fontService)
        {
            Device = device;
            Factory = factory;
            CommandList = commandList;
            PrimitiveBatch = primitiveBatch;
            TexturePool = texturePool;
            FontService = fontService;
        }

        public GraphicsDevice Device { get; internal set; }
        public Size DesignResolution { get; internal set; }
        public Swapchain MainSwapchain { get; internal set; }
        public ResourceFactory Factory { get; internal set; }
        public CommandList CommandList { get; internal set; }
        public PrimitiveBatcher PrimitiveBatch { get; internal set; }
        public RgbaTexturePool TexturePool { get; internal set; }
        public FontService FontService { get; }

        public ShaderLibrary ShaderLibrary { get; set; }
        public SharedResources SharedConstants { get; set; }

        public QuadGeometryStream QuadGeometryStream { get; set; }

        public RenderBucket MainBucket { get; set; }
    }
}
