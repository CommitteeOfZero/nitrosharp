using NitroSharp.Graphics;
using NitroSharp.NsScript.Execution;
using NitroSharp.Foundation;
using NitroSharp.Foundation.Input;
using System;
using NitroSharp.Foundation.Platform;

namespace NitroSharp
{
    public sealed class InputHandler : GameSystem
    {
        private readonly Window _window;
        private readonly NitroCore _nitroCore;

        public InputHandler(Window window, NitroCore nitroCore)
        {
            _window = window;
            _nitroCore = nitroCore;
        }

        public override void Update(float deltaMilliseconds)
        {
            if (ShouldAdvance() && !TrySkipAnimation())
            {
                if (_nitroCore.Interpreter.Status != InterpreterStatus.Suspended
                    && (_nitroCore.MainThread.SleepTimeout == TimeSpan.MaxValue || _nitroCore.WaitingForInput))
                {
                    _nitroCore.MainThread.Resume();
                    _nitroCore.WaitingForInput = false;
                }
            }

            if (Keyboard.IsKeyDownThisFrame(Key.F))
            {
                _window.ToggleBorderlessFullscreen();
            }

            //if (Keyboard.IsKeyDownThisFrame(Key.R))
            //{
            //    Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            //}
        }

        private bool TrySkipAnimation()
        {
            var text = _nitroCore.TextEntity;
            if (text != null)
            {
                var animation = text.GetComponent<SmoothTextAnimation>();
                if (animation?.Progress < 1.0f)
                {
                    animation.Stop();
                    text.RemoveComponent(animation);
                    text.AddComponent(new TextSkipAnimation());
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldAdvance()
        {
            return Mouse.IsButtonDownThisFrame(MouseButton.Left) || Keyboard.IsKeyDownThisFrame(Key.Space)
                || Keyboard.IsKeyDownThisFrame(Key.Enter) || Keyboard.IsKeyDownThisFrame(Key.KeypadEnter);
        }
    }
}
