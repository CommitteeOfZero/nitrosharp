using System;
using Veldrid.Sdl2;
using Veldrid;

namespace NitroSharp
{
    internal sealed class InputSystem : InputTracker
    {
        private readonly Sdl2Window _window;
        private readonly CoreLogic _coreLogic;

        public InputSystem(Sdl2Window window, CoreLogic coreLogic) : base(window)
        {
            _window = window;
            _coreLogic = coreLogic;
        }

        public override void Update(float deltaMilliseconds)
        {
            base.Update(deltaMilliseconds);

            if (ShouldAdvance())
            {
                if (_coreLogic.MainThread.SleepTimeout == TimeSpan.MaxValue || _coreLogic.WaitingForInput)
                {
                    _coreLogic.Interpreter.ResumeThread(_coreLogic.MainThread);
                    _coreLogic.WaitingForInput = false;
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
