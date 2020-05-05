using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

#nullable enable

namespace NitroSharp
{
    internal sealed class InputContext
    {
        private readonly GameWindow _window;

        private readonly HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private readonly HashSet<Key> _newKeysThisFrame = new HashSet<Key>();

        private readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();

        private Vector2 _prevSnapshotMousePosition;

        public InputContext(GameWindow window)
        {
            _window = window;
            CurrentSnapshot = null!;
            //window.FocusGained += OnGotFocus;
            //window.FocusLost += OnLostFocus;
        }

        public Vector2 MouseDelta { get; private set; }
        public InputSnapshot CurrentSnapshot { get; private set; }

        public void Update()
        {
            UpdateFrameInput(_window.PumpEvents());
        }

        public bool IsKeyDown(Key key)
            => _currentlyPressedKeys.Contains(key);

        public bool IsKeyDownThisFrame(Key key)
            => _newKeysThisFrame.Contains(key);

        public bool IsMouseButtonDown(MouseButton button)
            => _currentlyPressedMouseButtons.Contains(button);

        public bool IsMouseButtonDownThisFrame(MouseButton button)
            => _newMouseButtonsThisFrame.Contains(button);

        private void UpdateFrameInput(InputSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();

            MouseDelta = CurrentSnapshot.MousePosition - _prevSnapshotMousePosition;
            _prevSnapshotMousePosition = CurrentSnapshot.MousePosition;

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

        private void MouseUp(MouseButton mouseButton)
        {
            _currentlyPressedMouseButtons.Remove(mouseButton);
            _newMouseButtonsThisFrame.Remove(mouseButton);
        }

        private void MouseDown(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(mouseButton))
            {
                _newMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newKeysThisFrame.Remove(key);
        }

        private void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
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
