// Based on code from the Veldrid open source library
// https://github.com/mellinoe/veldrid

using NitroSharp.Foundation.Platform;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NitroSharp.Foundation
{
    public abstract class InputSystemBase : GameSystem
    {
        private readonly Window _window;

        private readonly HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private readonly HashSet<Key> _newKeysThisFrame = new HashSet<Key>();

        private readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();

        //private readonly List<Action<InputSystem>> _callbacks = new List<Action<InputSystem>>();

        private Vector2 _previousSnapshotMousePosition;

        //public Vector2 MousePosition
        //{
        //    get
        //    {
        //        return CurrentSnapshot.MousePosition;
        //    }
        //    set
        //    {
        //        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //        {
        //            Point screenPosition = _window.ClientToScreen(new Point((int)value.X, (int)value.Y));
        //            Mouse.SetPosition(screenPosition.X, screenPosition.Y);
        //            var cursorState = Mouse.GetCursorState();
        //            Point windowPoint = _window.ScreenToClient(new Point(cursorState.X, cursorState.Y));
        //            _previousSnapshotMousePosition = new Vector2(windowPoint.X / _window.ScaleFactor.X, windowPoint.Y / _window.ScaleFactor.Y);
        //        }
        //        else
        //        {
        //            Point screenPosition = new Point((int)value.X, (int)value.Y);
        //            Mouse.SetPosition(screenPosition.X, screenPosition.Y);
        //            var cursorState = Mouse.GetCursorState();
        //            _previousSnapshotMousePosition = new Vector2(cursorState.X / _window.ScaleFactor.X, cursorState.Y / _window.ScaleFactor.Y);
        //        }
        //    }
        //}

        protected InputSystemBase(Window window)
        {
            _window = window;
            window.GotFocus += OnGotFocus;
            window.LostFocus += OnLostFocus;
        }

        public Vector2 MouseDelta { get; private set; }
        public InputSnapshot CurrentSnapshot { get; private set; }

        ///// <summary>
        ///// Registers an anonmyous callback which is invoked every time the InputSystem is updated.
        ///// </summary>
        ///// <param name="callback">The callback to invoke.</param>
        //public void RegisterCallback(Action<InputSystem> callback)
        //{
        //    _callbacks.Add(callback);
        //}

        public override void Update(float deltaMilliseconds)
        {
            UpdateFrameInput(_window.GetInputSnapshot());
            //foreach (var callback in _callbacks)
            //{
            //    callback(this);
            //}
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

        private void OnLostFocus(object sender, EventArgs e)
        {
            ClearState();
        }

        private void OnGotFocus(object sender, EventArgs e)
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
