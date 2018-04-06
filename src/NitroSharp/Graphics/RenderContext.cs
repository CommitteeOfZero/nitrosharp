using FreeTypeBindings;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class RenderContext
    {
        internal RenderContext(GraphicsDevice device, ResourceFactory factory,
            CommandList commandList, Canvas canvas, EffectLibrary effectLibrary,
            SharedEffectProperties2D sharedEffectProperties2D,
            SharedEffectProperties3D sharedEffectProperties3D,
            FontService fontService)
        {
            Device = device;
            Factory = factory;
            CommandList = commandList;
            Canvas = canvas;
            Effects = effectLibrary;
            SharedEffectProperties2D = sharedEffectProperties2D;
            SharedEffectProperties3D = sharedEffectProperties3D;
            FontService = fontService;
        }

        public GraphicsDevice Device { get; }
        public ResourceFactory Factory { get; }
        public CommandList CommandList { get; }
        public Canvas Canvas { get; }
        public EffectLibrary Effects { get; }
        public SharedEffectProperties2D SharedEffectProperties2D { get; }
        public SharedEffectProperties3D SharedEffectProperties3D { get; }

        internal FontService FontService { get; }
    }
}
