#if !WINDOWS_UWP
using OpenTK;
using OpenTK.Input;
using MlKeyboard = MoeGame.Framework.Input.Keyboard;
using MlKey = MoeGame.Framework.Input.Key;
using MlButton = MoeGame.Framework.Input.MouseButton;
using MlMouse = MoeGame.Framework.Input.Mouse;
using System;

namespace MoeGame.Framework.Platform
{
    public partial class GameWindow
    {
        private void SubsribeToInputEvents()
        {
            _nativeWindow.KeyDown += OnKeyDown;
            _nativeWindow.KeyUp += OnKeyUp;
            _nativeWindow.MouseDown += OnMouseDown;
            _nativeWindow.MouseUp += OnMouseUp;

            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
        }

        private void OnGotFocus(object sender, EventArgs e)
        {
            ClearState();
        }

        private void OnLostFocus(object sender, EventArgs e)
        {
            ClearState();
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
