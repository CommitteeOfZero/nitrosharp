using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace NitroSharp
{
    internal abstract class InputTracker : OldGameSystem
    {
        private readonly GameWindow _window;

        private readonly HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private readonly HashSet<Key> _newKeysThisFrame = new HashSet<Key>();

        private readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();

        private Vector2 _previousSnapshotMousePosition;

        protected InputTracker(GameWindow window)
        {
            _window = window;
            //window.FocusGained += OnGotFocus;
            //window.FocusLost += OnLostFocus;
        }

        public Vector2 MouseDelta { get; private set; }
        public InputSnapshot CurrentSnapshot { get; private set; }

        public override void Update(float deltaMilliseconds)
        {
            UpdateFrameInput(_window.PumpEvents());
        }

        public bool IsKeyDown(Key Key) => _currentlyPressedKeys.Contains(Key);
        public bool IsKeyDownThisFrame(Key Key) => _newKeysThisFrame.Contains(Key);
        public bool IsMouseButtonDown(MouseButton button) => _currentlyPressedMouseButtons.Contains(button);
        public bool IsMouseButtonDownThisFrame(MouseButton button) => _newMouseButtonsThisFrame.Contains(button);

        private void UpdateFrameInput(InputSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();

            MouseDelta = CurrentSnapshot.MousePosition - _previousSnapshotMousePosition;
            _previousSnapshotMousePosition = CurrentSnapshot.MousePosition;

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent ke = keyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }

            IReadOnlyList<MouseEvent> mouseEvents = snapshot.MouseEvents;
            for (int i = 0; i < mouseEvents.Count; i++)
            {
                MouseEvent me = mouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }
        }

        private void MouseUp(MouseButton MouseButton)
        {
            _currentlyPressedMouseButtons.Remove(MouseButton);
            _newMouseButtonsThisFrame.Remove(MouseButton);
        }

        private void MouseDown(MouseButton MouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(MouseButton))
            {
                _newMouseButtonsThisFrame.Add(MouseButton);
            }
        }

        private void KeyUp(Key Key)
        {
            _currentlyPressedKeys.Remove(Key);
            _newKeysThisFrame.Remove(Key);
        }

        private void KeyDown(Key Key)
        {
            if (_currentlyPressedKeys.Add(Key))
            {
                _newKeysThisFrame.Add(Key);
            }
        }

        private void OnLostFocus()
        {
            ClearState();
        }

        private void OnGotFocus()
        {
            ClearState();
        }

        private void ClearState()
        {
            _currentlyPressedKeys.Clear();
            _newKeysThisFrame.Clear();
            _currentlyPressedMouseButtons.Clear();
            _newMouseButtonsThisFrame.Clear();
        }
    }
}
