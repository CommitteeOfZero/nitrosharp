using NitroSharp.NsScript;

namespace NitroSharp.Graphics
{
    internal struct HitTarget
    {
        public MouseState MouseState { get; private set; }
        public MouseState PrevMouseState { get; private set; }

        public HitTarget(MouseState initialState)
        {
            MouseState = PrevMouseState = initialState;
        }

        public void Update(bool hovered, bool pressed)
        {
            MouseState newState = hovered switch
            {
                true => (pressed, PrevMouseState) switch
                {
                    (false, MouseState.Down) => MouseState.Clicked,
                    (false, _) => MouseState.Over,
                    (true, _) => MouseState.Down
                },
                _ => MouseState.Normal
            };
            PrevMouseState = MouseState;
            MouseState = newState;
        }
    }
}
