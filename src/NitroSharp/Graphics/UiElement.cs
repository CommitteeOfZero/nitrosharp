#nullable enable

namespace NitroSharp.Graphics
{
    internal interface UiElement
    {
        public bool IsHovered { get; }
        void RecordInput(InputContext inputCtx, RenderContext renderCtx);
        bool HandleEvents();
    }
}
