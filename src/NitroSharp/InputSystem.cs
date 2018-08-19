using Veldrid.Sdl2;
using Veldrid;

namespace NitroSharp
{
    internal sealed class InputSystem : InputTracker
    {
        private readonly GameWindow _window;
        private readonly NsBuiltins _coreLogic;

        public InputSystem(GameWindow window, NsBuiltins coreLogic) : base(window)
        {
            _window = window;
            _coreLogic = coreLogic;
        }

        public override void Update(float deltaMilliseconds)
        {
            base.Update(deltaMilliseconds);

            if (ShouldAdvance())
            {
                _coreLogic.Advance();
            }
        }

        private bool ShouldAdvance()
        {
            return IsMouseButtonDownThisFrame(MouseButton.Left) || IsKeyDownThisFrame(Key.Space)
                || IsKeyDownThisFrame(Key.Enter) || IsKeyDownThisFrame(Key.KeypadEnter);
        }
    }
}
