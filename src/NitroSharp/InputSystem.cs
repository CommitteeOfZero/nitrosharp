using System;
using Veldrid.Sdl2;
using Veldrid;

namespace NitroSharp
{
    internal sealed class InputSystem : InputTracker
    {
        private readonly Sdl2Window _window;
        private readonly NitroCore _nitroCore;

        public InputSystem(Sdl2Window window, NitroCore nitroCore) : base(window)
        {
            _window = window;
            _nitroCore = nitroCore;
        }

        public override void Update(float deltaMilliseconds)
        {
            base.Update(deltaMilliseconds);

            if (ShouldAdvance())
            {
                if (_nitroCore.MainThread.SleepTimeout == TimeSpan.MaxValue || _nitroCore.WaitingForInput)
                {
                    _nitroCore.Interpreter.ResumeThread(_nitroCore.MainThread);
                    _nitroCore.WaitingForInput = false;
                }
            }
        }

        private bool ShouldAdvance()
        {
            return IsMouseButtonDownThisFrame(MouseButton.Left) || IsKeyDownThisFrame(Key.Space)
                || IsKeyDownThisFrame(Key.Enter) || IsKeyDownThisFrame(Key.KeypadEnter);
        }
    }
}
