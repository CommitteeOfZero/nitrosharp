﻿#if !WINDOWS_UWP
using OpenTK.Input;
using MoeKeyboard = MoeGame.Framework.Input.Keyboard;
using MoeKey = MoeGame.Framework.Input.Key;
using MoeButton = MoeGame.Framework.Input.MouseButton;
using MoeMouse = MoeGame.Framework.Input.Mouse;
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
            MoeMouse.Position = new System.Drawing.Point(state.X, state.Y);
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            var mlKey = (MoeKey)e.Key;
            if (MoeKeyboard.PressedKeys.Add(mlKey))
            {
                MoeKeyboard.NewlyPressedKeys.Add(mlKey);
            }
        }

        private void OnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            var mlKey = (MoeKey)e.Key;
            MoeKeyboard.PressedKeys.Remove(mlKey);
            MoeKeyboard.NewlyPressedKeys.Remove(mlKey);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var mlButton = (MoeButton)e.Button;
            if (MoeMouse.PressedButtons.Add(mlButton))
            {
                MoeMouse.NewlyPressedButtons.Add(mlButton);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var mlButton = (MoeButton)e.Button;
            MoeMouse.PressedButtons.Remove(mlButton);
            MoeMouse.NewlyPressedButtons.Remove(mlButton);
        }

        private void ClearState()
        {
            MoeKeyboard.NewlyPressedKeys.Clear();
            MoeKeyboard.PressedKeys.Clear();
            MoeMouse.NewlyPressedButtons.Clear();
            MoeMouse.PressedButtons.Clear();
        }
    }
}
#endif
