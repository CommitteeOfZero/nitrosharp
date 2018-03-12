using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderContext
    {
        public RenderContext(GraphicsDevice device, ResourceFactory factory, CommandList commandList, Canvas canvas, EffectLibrary effectLibrary)
        {
            Device = device;
            Factory = factory;
            CommandList = commandList;
            Canvas = canvas;
            Effects = effectLibrary;
        }

        public GraphicsDevice Device { get; }
        public ResourceFactory Factory { get; }
        public CommandList CommandList { get; }
        public Canvas Canvas { get; }
        public EffectLibrary Effects { get; }
    }
}
