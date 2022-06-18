using System.Numerics;
using NitroSharp.NsScript;

namespace NitroSharp.Graphics
{
    [Persistable]
    internal partial struct UiElementFocusData
    {
        public EntityId Left;
        public EntityId Up;
        public EntityId Right;
        public EntityId Down;
    }

    internal interface UiElement
    {
        public EntityId Id { get; }
        public ref UiElementFocusData FocusData { get; }
        public bool IsFocused { get; }
        public RenderItem2D? RenderItem { get; }

        bool HandleEvents(GameContext ctx);

        public void Focus(RenderContext renderContext)
        {
            if (RenderItem is { } visual)
            {
                Size bounds = visual.GetUnconstrainedBounds(renderContext);
                var center = new Vector2(bounds.Width / 2.0f, bounds.Height / 2.0f);
                renderContext.Window.SetMousePosition(visual.Transform.Position.XY() + center);
            }
        }

        public EntityId GetNextFocus(NsFocusDirection direction) => direction switch
        {
            NsFocusDirection.Left => FocusData.Left,
            NsFocusDirection.Up => FocusData.Up,
            NsFocusDirection.Right => FocusData.Right,
            NsFocusDirection.Down => FocusData.Down,
            _ => ThrowHelper.Unreachable<EntityId>()
        };

        public void SetNextFocus(NsFocusDirection direction, in EntityId entity)
        {
            switch (direction)
            {
                case NsFocusDirection.Left:
                    FocusData.Left = entity;
                    break;
                case NsFocusDirection.Up:
                    FocusData.Up = entity;
                    break;
                case NsFocusDirection.Right:
                    FocusData.Right = entity;
                    break;
                case NsFocusDirection.Down:
                    FocusData.Down = entity;
                    break;
            }
        }
    }
}
