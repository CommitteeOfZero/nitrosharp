using CommitteeOfZero.Nitro.Graphics;
using CommitteeOfZero.NsScript.Execution;
using MoeGame.Framework;
using MoeGame.Framework.Input;
using System;

namespace CommitteeOfZero.Nitro
{
    public sealed class InputHandler : GameSystem
    {
        private readonly NitroCore _nitroCore;

        public InputHandler(NitroCore n2system)
        {
            _nitroCore = n2system;
        }

        public override void Update(float deltaMilliseconds)
        {
            if (ShouldAdvance())
            {
                if (TrySkipAnimation()) return;
                if (_nitroCore.Interpreter.Status == InterpreterStatus.Active && _nitroCore.MainThread.SleepTimeout == TimeSpan.MaxValue)
                {
                    _nitroCore.MainThread.Resume();
                }
            }

            if (Keyboard.IsKeyDownThisFrame(Key.R))
            {
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            }
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
