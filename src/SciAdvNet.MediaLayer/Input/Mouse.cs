using System.Collections.Generic;
using System.Drawing;

namespace SciAdvNet.MediaLayer.Input
{
    public static class Mouse
    {
        internal static readonly HashSet<MouseButton> PressedButtons = new HashSet<MouseButton>();
        internal static readonly HashSet<MouseButton> NewlyPressedButtons = new HashSet<MouseButton>();

        public static Point Position { get; internal set; }
        public static int X => Position.X;
        public static int Y => Position.Y;

        public static bool IsButtonDown(MouseButton button) => PressedButtons.Contains(button);
        public static bool IsButtonUp(MouseButton button) => !PressedButtons.Contains(button);

        public static bool IsButtonDownThisFrame(MouseButton button) => NewlyPressedButtons.Contains(button);
    }
}
