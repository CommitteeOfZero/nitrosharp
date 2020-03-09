using NitroSharp.Experimental;
using NitroSharp.NsScript.Primitives;

#nullable enable

namespace NitroSharp.Interactivity
{
    internal struct ChoiceEntities
    {
        public Entity DefaultVisual;
        public Entity MouseOverVisual;
        public Entity MouseDownVisual;
        public Entity MouseEnterThread;
        public Entity MouseLeaveThread;

        public void SetVisualEntity(MouseState mouseState, Entity entity)
        {
            switch (mouseState)
            {
                case MouseState.Normal:
                    DefaultVisual = entity;
                    break;
                case MouseState.Over:
                    MouseOverVisual = entity;
                    break;
                case MouseState.Pressed:
                    MouseDownVisual = entity;
                    break;
            }
        }

        public void SetThreadEntity(MouseState mouseState, Entity entity)
        {
            if (mouseState == MouseState.Over)
            {
                MouseEnterThread = entity;
            }
            else if (mouseState == MouseState.Leave)
            {
                MouseLeaveThread = entity;
            }
        }
    }

    internal sealed class ChoiceStorage : EntityStorage
    {
        public ComponentVec<ChoiceEntities> AssociatedEntities { get; }
        public ComponentVec<MouseState> MouseState { get; }

        public ChoiceStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            AssociatedEntities = AddComponentVec<ChoiceEntities>();
            MouseState = AddComponentVec<MouseState>();
        }
    }
}
