namespace NitroSharp.Graphics
{
    internal interface Renderable
    {
        void CreateDeviceObjects(RenderContext renderContext);
        void Render(RenderContext renderContext);
    }
}
