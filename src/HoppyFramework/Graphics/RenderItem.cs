namespace HoppyFramework.Graphics
{
    public abstract class RenderItem : Component
    {
        public abstract void Render(DXRenderContext renderContext);
    }
}
