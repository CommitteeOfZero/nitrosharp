using NitroSharp.Graphics;
using NitroSharp.Foundation;
using System;
using NitroSharp.Foundation.Platform;

namespace NitroSharp
{
    public sealed class InputHandler : InputSystemBase
    {
        private readonly Window _window;
        private readonly NitroCore _nitroCore;

        public InputHandler(Window window, NitroCore nitroCore) : base(window)
        {
            _window = window;
            _nitroCore = nitroCore;
        }

        public override void Update(float deltaMilliseconds)
        {
            base.Update(deltaMilliseconds);

            if (ShouldAdvance() && !TrySkipAnimation())
            {
                if (
                    (_nitroCore.MainThread.SleepTimeout == TimeSpan.MaxValue || _nitroCore.WaitingForInput))
                {
                    _nitroCore.Interpreter.ResumeThread(_nitroCore.MainThread);
                    _nitroCore.WaitingForInput = false;
                }
            }

            if (IsKeyDownThisFrame(Key.F))
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

        private bool ShouldAdvance()
        {
            return IsMouseButtonDownThisFrame(MouseButton.Left) || IsKeyDownThisFrame(Key.Space)
                || IsKeyDownThisFrame(Key.Enter) || IsKeyDownThisFrame(Key.KeypadEnter);
        }
    }
}
