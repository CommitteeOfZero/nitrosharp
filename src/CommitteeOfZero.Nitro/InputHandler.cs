using MoeGame.Framework;
using MoeGame.Framework.Input;
using System;

namespace CommitteeOfZero.Nitro
{
    public class InputHandler : GameSystem
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
                if (_nitroCore.MainThread.SleepTimeout == TimeSpan.MaxValue)
                {
                    _nitroCore.MainThread.Resume();
                }
            }
        }

        private static bool ShouldAdvance()
        {
            return Mouse.IsButtonDownThisFrame(MouseButton.Left) || Keyboard.IsKeyDownThisFrame(Key.Space)
                || Keyboard.IsKeyDownThisFrame(Key.Enter) || Keyboard.IsKeyDownThisFrame(Key.KeypadEnter);
        }
    }
}
