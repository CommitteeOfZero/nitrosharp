// Based on code from the Veldrid open source library
// https://github.com/mellinoe/veldrid

using System.Collections.Generic;

namespace NitroSharp.Foundation.Platform
{
    public sealed class InputSnapshot
    {
        internal List<KeyEvent> KeyEventsList { get; } = new List<KeyEvent>();
        internal List<MouseEvent> MouseEventsList { get; } = new List<MouseEvent>();
        internal List<char> KeyCharPressesList { get; } = new List<char>();

        public IReadOnlyList<KeyEvent> KeyEvents => KeyEventsList;
        public IReadOnlyList<MouseEvent> MouseEvents => MouseEventsList;
        public IReadOnlyList<char> KeyCharPresses => KeyCharPressesList;

        public System.Numerics.Vector2 MousePosition { get; set; }

        private readonly bool[] _mouseDown = new bool[13];
        public bool[] MouseDown => _mouseDown;

        public bool IsMouseDown(MouseButton button)
        {
            return _mouseDown[(int)button];
        }

        internal void Clear()
        {
            KeyEventsList.Clear();
            MouseEventsList.Clear();
            KeyCharPressesList.Clear();
        }
    }
}
