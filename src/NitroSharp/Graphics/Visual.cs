using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal abstract class Visual : Component, Renderable
    {
        public virtual void CreateDeviceObjects(RenderContext renderContext)
        {
        }

        public abstract void Render(RenderContext renderContext);

        public int Priority { get; protected set; }
        public abstract SizeF Bounds { get; }
    }
}
