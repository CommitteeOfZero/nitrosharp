#if !WINDOWS_UWP
using OpenTK;
using OpenTK.Input;
using MlKeyboard = SciAdvNet.MediaLayer.Input.Keyboard;
using MlKey = SciAdvNet.MediaLayer.Input.Key;
using MlButton = SciAdvNet.MediaLayer.Input.MouseButton;
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
            if (MlKeyboard.PressedKeys.Add(mlKey))
            {
                MlKeyboard.NewlyPressedKeys.Add(mlKey);
            }
        }

        private void OnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            var mlKey = (MlKey)e.Key;
            MlKeyboard.PressedKeys.Remove(mlKey);
            MlKeyboard.NewlyPressedKeys.Remove(mlKey);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var mlButton = (MlButton)e.Button;
            if (MlMouse.PressedButtons.Add(mlButton))
            {
                MlMouse.NewlyPressedButtons.Add(mlButton);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var mlButton = (MlButton)e.Button;
            MlMouse.PressedButtons.Remove(mlButton);
            MlMouse.NewlyPressedButtons.Remove(mlButton);
        }

        private void ClearState()
        {
            MlKeyboard.NewlyPressedKeys.Clear();
            MlKeyboard.PressedKeys.Clear();
            MlMouse.NewlyPressedButtons.Clear();
            MlMouse.PressedButtons.Clear();
        }
    }
}
#endif
