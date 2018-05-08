using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderContext
    {
        internal RenderContext(GraphicsDevice device, ResourceFactory factory,
            CommandList commandList, Canvas canvas, EffectLibrary effectLibrary,
            SharedEffectProperties2D sharedEffectProperties2D,
            SharedEffectProperties3D sharedEffectProperties3D,
            RgbaTexturePool texturePool, FontService fontService)
        {
            Device = device;
            Factory = factory;
            CommandList = commandList;
            Canvas = canvas;
            Effects = effectLibrary;
            SharedEffectProperties2D = sharedEffectProperties2D;
            SharedEffectProperties3D = sharedEffectProperties3D;
            TexturePool = texturePool;
            FontService = fontService;
        }

        public GraphicsDevice Device { get; internal set; }
        public Swapchain MainSwapchain { get; internal set; }
        public ResourceFactory Factory { get; internal set; }
        public CommandList CommandList { get; internal set; }
        public Canvas Canvas { get; internal set; }
        public EffectLibrary Effects { get; internal set; }
        public SharedEffectProperties2D SharedEffectProperties2D { get; internal set; }
        public SharedEffectProperties3D SharedEffectProperties3D { get; internal set; }
        public RgbaTexturePool TexturePool { get; internal set; }
        public FontService FontService { get; }
    }
}
