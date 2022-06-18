using System;
using System.Numerics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using Veldrid;
using Veldrid.Sdl2;

namespace NitroSharp
{
    internal enum VirtualKey
    {
        Enter,
        Advance,
        Back,
        Left,
        Up,
        Right,
        Down,
        Skip
    }

    internal enum VirtualAxis
    {
        TriggerLeft,
        TriggerRight
    }

    internal sealed class InputContext : IDisposable
    {
        private readonly RawInput _rawInput;

        private readonly bool[] _vkeyState = new bool[8];
        private readonly bool[] _newVkeys = new bool[8];

        public InputContext(GameWindow window)
        {
            _rawInput = new RawInput(window);
        }

        public GameWindow Window => _rawInput.Window;
        public Vector2 MousePosition { get; private set; }
        public float WheelDelta { get; private set; }

        public bool VKeyState(VirtualKey vkey) => _vkeyState[(int)vkey];
        public bool VKeyDown(VirtualKey vkey) => _newVkeys[(int)vkey];

        public float GetAxis(VirtualAxis axis)
        {
            return axis switch
            {
                VirtualAxis.TriggerLeft => _rawInput.IsKeyDown(Key.Left)
                    ? 1.0f
                    : _rawInput.Gamepad.GetAxis(SDL_GameControllerAxis.TriggerLeft),
                VirtualAxis.TriggerRight => _rawInput.IsKeyDown(Key.Right)
                    ? 1.0f
                    : _rawInput.Gamepad.GetAxis(SDL_GameControllerAxis.TriggerRight),
                _ => 0.0f
            };
        }

        public void Update(SystemVariableLookup systemVariables)
        {
            RawInput input = _rawInput;
            input.Update();
            MousePosition = input.Snapshot.MousePosition;
            WheelDelta = input.WheelDelta;

            PollVkey(input, VirtualKey.Enter);
            PollVkey(input, VirtualKey.Advance);
            PollVkey(input, VirtualKey.Back);
            PollVkey(input, VirtualKey.Left);
            PollVkey(input, VirtualKey.Up);
            PollVkey(input, VirtualKey.Right);
            PollVkey(input, VirtualKey.Down);
            PollVkey(input, VirtualKey.Skip);

            Gamepad gamepad = input.Gamepad;
            SystemVariableLookup sys = systemVariables;
            set(ref sys.RightButtonDown, VKeyState(VirtualKey.Back));
            pollController(SDL_GameControllerButton.Start, ref sys.X360StartButtonDown);
            set(ref sys.X360AButtonDown, VKeyState(VirtualKey.Advance));
            set(ref sys.X360BButtonDown, VKeyState(VirtualKey.Back));
            pollController(SDL_GameControllerButton.Y, ref sys.X360YButtonDown);
            set(ref sys.X360LeftButtonDown, VKeyState(VirtualKey.Left));
            set(ref sys.X360UpButtonDown, VKeyState(VirtualKey.Down));
            set(ref sys.X360RightButtonDown, VKeyState(VirtualKey.Right));
            set(ref sys.X360DownButtonDown, VKeyState(VirtualKey.Down));
            pollController(SDL_GameControllerButton.LeftShoulder, ref sys.X360LbButtonDown);
            pollController(SDL_GameControllerButton.RightShoulder, ref sys.X360RbButtonDown);

            static void set(ref ConstantValue target, bool value)
            {
                target = ConstantValue.Boolean(value);
            }

            void pollController(SDL_GameControllerButton button, ref ConstantValue val)
            {
                bool down = gamepad.ButtonState(button);
                set(ref val, down);
            }
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
                VirtualKey.Back => input.MouseState(MouseButton.Right) |
                                   input.KeyState(Key.BackSpace) |
                                   input.KeyState(Key.Escape) |
                                   gamepad.ButtonState(SDL_GameControllerButton.B),
                VirtualKey.Enter => input.MouseState(MouseButton.Left) |
                                    input.KeyState(Key.Enter) |
                                    input.KeyState(Key.KeypadEnter) |
                                    gamepad.ButtonState(SDL_GameControllerButton.A),
                VirtualKey.Left => input.KeyState(Key.Left) |
                                   gamepad.ButtonState(SDL_GameControllerButton.DPadLeft),
                VirtualKey.Up => input.KeyState(Key.Up) |
                                 input.WheelDelta > 0 |
                                 gamepad.ButtonState(SDL_GameControllerButton.DPadUp),
                VirtualKey.Right => input.KeyState(Key.Right) |
                                    gamepad.ButtonState(SDL_GameControllerButton.DPadRight),
                VirtualKey.Down => input.KeyState(Key.Down) |
                                   input.WheelDelta < 0 |
                                   gamepad.ButtonState(SDL_GameControllerButton.DPadDown),
                VirtualKey.Skip => input.KeyState(Key.ControlLeft) | input.KeyState(Key.ControlRight),
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
