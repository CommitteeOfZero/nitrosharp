using System;
using Veldrid.Sdl2;
using Veldrid;
using NitroSharp.Dialogue;

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

            if (ShouldAdvance() && !TrySkipAnimation())
            {
                if (_coreLogic.MainThread.SleepTimeout == TimeSpan.MaxValue || _coreLogic.WaitingForInput)
                {
                    _coreLogic.Interpreter.ResumeThread(_coreLogic.MainThread);
                    _coreLogic.WaitingForInput = false;
                }
            }
        }

        private bool TrySkipAnimation()
        {
            var text = _coreLogic.TextEntity;
            if (text != null)
            {
                var reveal = text.GetComponent<TextRevealAnimation>();
                if (reveal?.IsAllTextVisible == false)
                {
                    reveal.Stop();
                    text.RemoveComponent(reveal);
                    text.AddComponent(new RevealSkipAnimation(reveal.CurrentGlyphIndex));
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
