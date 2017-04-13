using MoeGame.Framework;
using MoeGame.Framework.Input;

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
            if (Mouse.IsButtonDownThisFrame(MouseButton.Left))
            {
                _nitroCore.CurrentThread.Resume();
            }
        }
    }
}
