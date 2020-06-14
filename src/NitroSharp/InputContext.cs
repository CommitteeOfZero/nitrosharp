using System;
using System.Numerics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using Veldrid;
using Veldrid.Sdl2;

#nullable enable

namespace NitroSharp
{
    internal enum VirtualKey
    {
        Enter,
        Advance,
        Left,
        Up,
        Right,
        Down
    }

    internal sealed class InputContext : IDisposable
    {
        private readonly RawInput _rawInput;

        private readonly bool[] _vkeyState = new bool[6];
        private readonly bool[] _newVkeys = new bool[6];

        public InputContext(GameWindow window)
        {
            _rawInput = new RawInput(window);
        }

        public GameWindow Window => _rawInput.Window;
        public Vector2 MousePosition { get; private set; }

        public bool VKeyState(VirtualKey vkey) => _vkeyState[(int)vkey];
        public bool VKeyDown(VirtualKey vkey) => _newVkeys[(int)vkey];

        public void Update(NsScriptVM vm)
        {
            RawInput input = _rawInput;
            input.Update();
            MousePosition = input.CurrentSnapshot.MousePosition;

            PollVkey(input, VirtualKey.Advance);
            PollVkey(input, VirtualKey.Enter);
            PollVkey(input, VirtualKey.Left);
            PollVkey(input, VirtualKey.Up);
            PollVkey(input, VirtualKey.Right);
            PollVkey(input, VirtualKey.Down);

            Gamepad gamepad = input.Gamepad;
            SystemVariableLookup sys = vm.SystemVariables;
            set(ref sys.RightButtonDown, input.MouseState(MouseButton.Right));
            poll(SDL_GameControllerButton.Start, ref sys.X360StartButtonDown);
            poll(SDL_GameControllerButton.A, ref sys.X360AButtonDown);
            poll(SDL_GameControllerButton.B, ref sys.X360BButtonDown);
            poll(SDL_GameControllerButton.Y, ref sys.X360YButtonDown);
            poll(SDL_GameControllerButton.DPadLeft, ref sys.X360LeftButtonDown);
            poll(SDL_GameControllerButton.DPadUp, ref sys.X360UpButtonDown);
            poll(SDL_GameControllerButton.DPadRight, ref sys.X360RightButtonDown);
            poll(SDL_GameControllerButton.DPadDown, ref sys.X360DownButtonDown);
            poll(SDL_GameControllerButton.LeftShoulder, ref sys.X360LbButtonDown);
            poll(SDL_GameControllerButton.RightShoulder, ref sys.X360RbButtonDown);

            static void set(ref ConstantValue target, bool value)
                => target = ConstantValue.Boolean(value);

            void poll(SDL_GameControllerButton button, ref ConstantValue val)
                => set(ref val, gamepad.ButtonState(button));
        }

        private static bool VKeyState(RawInput input, VirtualKey key)
        {
            Gamepad gamepad = input.Gamepad;
            return key switch
            {
                VirtualKey.Advance => input.MouseState(MouseButton.Left) |
                                      input.KeyState(Key.Enter) |
                                      input.KeyState(Key.KeypadEnter) |
                                      input.KeyState(Key.Space) |
                                      gamepad.ButtonState(SDL_GameControllerButton.A),
                VirtualKey.Enter => input.MouseState(MouseButton.Left) |
                                    input.KeyState(Key.Enter) |
                                    input.KeyState(Key.KeypadEnter) |
                                    gamepad.ButtonState(SDL_GameControllerButton.A),
                VirtualKey.Left => input.KeyState(Key.Left) |
                                   gamepad.ButtonState(SDL_GameControllerButton.DPadLeft),
                VirtualKey.Up => input.KeyState(Key.Up) |
                                 gamepad.ButtonState(SDL_GameControllerButton.DPadUp),
                VirtualKey.Right => input.KeyState(Key.Right) |
                                    gamepad.ButtonState(SDL_GameControllerButton.DPadRight),
                VirtualKey.Down => input.KeyState(Key.Down) |
                                   gamepad.ButtonState(SDL_GameControllerButton.DPadDown),
                _ => false
            };
        }

        private void PollVkey(RawInput input, VirtualKey vkey)
        {
            int index = (int)vkey;
            bool down = VKeyState(input, vkey);
            _newVkeys[index] = !_vkeyState[index] & down;
            _vkeyState[index] = down;
        }

        public void Dispose()
        {
            _rawInput.Dispose();
        }
    }
}
