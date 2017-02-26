#if !WINDOWS_UWP
using OpenTK;
using OpenTK.Input;
using MlKeyboard = SciAdvNet.MediaLayer.Input.Keyboard;
using MlKey = SciAdvNet.MediaLayer.Input.Key;
using MlMouse = SciAdvNet.MediaLayer.Input.Mouse;

namespace SciAdvNet.MediaLayer.Platform
{
    public partial class GameWindow
    {
        private void SubsribeToInputEvents()
        {
            _nativeWindow.KeyDown += OnKeyDown;
            _nativeWindow.KeyUp += OnKeyUp;
            _nativeWindow.MouseDown += OnMouseDown;
            _nativeWindow.MouseUp += OnMouseUp;
        }

        private void RefreshMouseState()
        {
            var state = OpenTK.Input.Mouse.GetCursorState();
            MlMouse.Position = new System.Drawing.Point(state.X, state.Y);
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            var mlKey = (MlKey)e.Key;
            if (!MlKeyboard.PressedKeys.Contains(mlKey))
            {
                MlKeyboard.PressedKeys.Add(mlKey);
            }
        }

        private void OnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            var mlKey = (MlKey)e.Key;
            if (MlKeyboard.PressedKeys.Contains(mlKey))
            {
                MlKeyboard.PressedKeys.Remove(mlKey);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            int button = (int)e.Button;
            MlMouse.PressedButtons[button] = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            int button = (int)e.Button;
            MlMouse.PressedButtons[button] = false;
        }

        private void ClearState()
        {
            MlKeyboard.PressedKeys.Clear();
        }
    }
}
#endif
