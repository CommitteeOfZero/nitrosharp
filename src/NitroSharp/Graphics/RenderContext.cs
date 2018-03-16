using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class RenderContext
    {
        public RenderContext(GraphicsDevice device, ResourceFactory factory,
            CommandList commandList, Canvas canvas, EffectLibrary effectLibrary,
            SharedEffectProperties2D sharedEffectProperties2D, SharedEffectProperties3D camera)
        {
            Device = device;
            Factory = factory;
            CommandList = commandList;
            Canvas = canvas;
            Effects = effectLibrary;
            SharedEffectProperties2D = sharedEffectProperties2D;
            Camera = camera;
        }

        public GraphicsDevice Device { get; }
        public ResourceFactory Factory { get; }
        public CommandList CommandList { get; }
        public Canvas Canvas { get; }
        public EffectLibrary Effects { get; }
        public SharedEffectProperties2D SharedEffectProperties2D { get; }
        public SharedEffectProperties3D Camera { get; }
    }
}
