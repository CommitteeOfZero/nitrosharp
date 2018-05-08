using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using KeyEvent = Veldrid.KeyEvent;

namespace CowsHead.Android
{
    internal sealed class SimpleInputSnapshot : InputSnapshot
    {
        public List<KeyEvent> KeyEventsList { get; private set; } = new List<KeyEvent>();
        public List<MouseEvent> MouseEventsList { get; private set; } = new List<MouseEvent>();
        public List<char> KeyCharPressesList { get; private set; } = new List<char>();

        public IReadOnlyList<KeyEvent> KeyEvents => KeyEventsList;
        public IReadOnlyList<MouseEvent> MouseEvents => MouseEventsList;
        public IReadOnlyList<char> KeyCharPresses => KeyCharPressesList;

        public Vector2 MousePosition { get; set; }

        private bool[] _mouseDown = new bool[13];
        public bool[] MouseDown => _mouseDown;
        public float WheelDelta { get; set; }

        public bool IsMouseDown(MouseButton button)
        {
            return _mouseDown[(int)button];
        }

        internal void Clear()
        {
            KeyEventsList.Clear();
            MouseEventsList.Clear();
            KeyCharPressesList.Clear();
            WheelDelta = 0f;
        }

        public void CopyTo(SimpleInputSnapshot other)
        {
            Debug.Assert(this != other);

            other.MouseEventsList.Clear();
            foreach (var mouseEvent in MouseEventsList)
            {
                other.MouseEventsList.Add(mouseEvent);
            }

            other.KeyEventsList.Clear();
            foreach (var keyEvent in KeyEventsList)
            {
                other.KeyEventsList.Add(keyEvent);
            }

            other.KeyCharPressesList.Clear();
            foreach (var keyPress in KeyCharPressesList)
            {
                other.KeyCharPressesList.Add(keyPress);
            }

            other.MousePosition = MousePosition;
            other.WheelDelta = WheelDelta;
            _mouseDown.CopyTo(other._mouseDown, 0);
        }
    }
}
