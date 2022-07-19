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
        Skip,
        Y,
        Start,
        BumperLeft,
        BumperRight
    }

    internal enum VirtualAxis
    {
        TriggerLeft,
        TriggerRight
    }

    internal sealed class InputContext : IDisposable
    {
        private readonly RawInput _rawInput;

        private static readonly int _vkeysCount = Enum.GetValues<VirtualKey>().Length;
        private readonly bool[] _vkeyState = new bool[_vkeysCount];
        private readonly bool[] _newVkeys = new bool[_vkeysCount];
        private readonly bool[] _oldVkeys = new bool[_vkeysCount];

        public InputContext(GameWindow window)
        {
            _rawInput = new RawInput(window);
        }

        public GameWindow Window => _rawInput.Window;
        public Vector2 MousePosition { get; private set; }
        public float WheelDelta { get; private set; }

        public bool VKeyState(VirtualKey vkey) => _vkeyState[(int)vkey];
        public bool VKeyDown(VirtualKey vkey) => _newVkeys[(int)vkey];
        public bool VKeyUp(VirtualKey vkey) => _oldVkeys[(int)vkey];

        public float GetAxis(VirtualAxis axis)
        {
            return axis switch
            {
                VirtualAxis.TriggerLeft => _rawInput.KeyState(Key.Z)
                    ? 1.0f
                    : _rawInput.Gamepad.GetAxis(SDL_GameControllerAxis.TriggerLeft),
                VirtualAxis.TriggerRight => _rawInput.KeyState(Key.C)
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

            foreach (VirtualKey key in Enum.GetValues<VirtualKey>())
            {
                PollVkey(input, key);
            }

            Gamepad gamepad = input.Gamepad;
            SystemVariableLookup sys = systemVariables;

            setDown(ref sys.X360AButtonDown, VirtualKey.Advance);
            setDown(ref sys.X360BButtonDown, VirtualKey.Back);
            setDownUp(ref sys.X360YButtonDown, VirtualKey.Y);

            setDownUp(ref sys.X360UpButtonDown, VirtualKey.Up);
            setDownUp(ref sys.X360DownButtonDown, VirtualKey.Down);
            setDownUp(ref sys.X360LeftButtonDown, VirtualKey.Left);
            setDownUp(ref sys.X360RightButtonDown, VirtualKey.Right);

            setDownUp(ref sys.X360StartButtonDown, VirtualKey.Start);
            setDownUp(ref sys.X360LbButtonDown, VirtualKey.BumperLeft);
            setDownUp(ref sys.X360RbButtonDown, VirtualKey.BumperRight);

            static void set(ref ConstantValue target, bool value)
            {
                target = ConstantValue.Boolean(value);
            }

            void setDown(ref ConstantValue target, VirtualKey key)
            {
                if (VKeyDown(key))
                {
                    set(ref target, true);
                }
            }

            void setUp(ref ConstantValue target, VirtualKey key)
            {
                if (VKeyUp(key))
                {
                    set(ref target, false);
                }
            }

            void setDownUp(ref ConstantValue target, VirtualKey key)
            {
                setDown(ref target, key);
                setUp(ref target, key);
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
                VirtualKey.Y => input.KeyState(Key.BackSlash) |
                                gamepad.ButtonState(SDL_GameControllerButton.Y),
                VirtualKey.Start => input.MouseState(MouseButton.Left) |
                                    input.KeyState(Key.Enter) |
                                    input.KeyState(Key.KeypadEnter) |
                                    gamepad.ButtonState(SDL_GameControllerButton.Start),
                VirtualKey.BumperLeft => input.KeyState(Key.BracketLeft) |
                                         gamepad.ButtonState(SDL_GameControllerButton.LeftShoulder),
                VirtualKey.BumperRight => input.KeyState(Key.BracketRight) |
                                          gamepad.ButtonState(SDL_GameControllerButton.RightShoulder),
                _ => false
            };
        }

        private void PollVkey(RawInput input, VirtualKey vkey)
        {
            int index = (int)vkey;
            bool down = VKeyState(input, vkey);
            _newVkeys[index] = !_vkeyState[index] & down;
            _oldVkeys[index] = _vkeyState[index] & !down;
            _vkeyState[index] = down;
        }

        public void Dispose()
        {
            _rawInput.Dispose();
        }
    }
}
