using System.Drawing;

namespace SciAdvNet.MediaLayer.Input
{
    public static class Mouse
    {
        internal static bool[] PressedButtons = new bool[3];

        public static Point Position { get; internal set; }
        public static int X => Position.X;
        public static int Y => Position.Y;

        public static bool IsButtonDown(MouseButton button) => PressedButtons[(int)button];
        public static bool IsButtonUp(MouseButton button) => !PressedButtons[(int)button];
    }
}
