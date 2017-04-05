using HoppyFramework;

namespace ProjectHoppy.Graphics
{
    public abstract class RenderItem : Component
    {
        public abstract void Render(RenderSystem renderSystem);
    }
}
