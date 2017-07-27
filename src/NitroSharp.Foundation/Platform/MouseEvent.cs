// Based on code from the Veldrid open source library
// https://github.com/mellinoe/veldrid

namespace NitroSharp.Foundation.Platform
{
    public struct MouseEvent
    {
        public MouseButton MouseButton { get; }
        public bool Down { get; }

        public MouseEvent(MouseButton button, bool down)
        {
            MouseButton = button;
            Down = down;
        }
    }

    public enum MouseButton
    {
        Left = 0,
        Middle = 1,
        Right = 2,
        Button1 = 3,
        Button2 = 4,
        Button3 = 5,
        Button4 = 6,
        Button5 = 7,
        Button6 = 8,
        Button7 = 9,
        Button8 = 10,
        Button9 = 11,
        LastButton = 12
    }
}
