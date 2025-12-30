using System;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

namespace NitroSharp.Input;

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

internal sealed class InputContext(GameWindow window) : IDisposable
{
    private readonly bool[] _vkeyState = new bool[8];
    private readonly bool[] _newVkeys = new bool[8];
    private bool _advance;

    public InputSnapshot Snapshot { get; private set; } = null!;
    public Gamepad Gamepad { get; private set; } = Gamepad.Default(window is Sdl3Window);

    public bool VKeyState(VirtualKey vkey) => _vkeyState[(int)vkey];
    public bool VKeyDown(VirtualKey vkey) => _newVkeys[(int)vkey];

    public bool ConsumeAdvance()
    {
        bool result = _advance;
        _advance = false;
        return result;
    }

    public float GetAxis(VirtualAxis axis)
    {
        return axis switch
        {
            VirtualAxis.TriggerLeft => Snapshot.IsKeyDown(Key.Left)
                ? 1.0f
                : Gamepad.GetAxis(GamepadAxis.LeftTrigger),
            VirtualAxis.TriggerRight => Snapshot.IsKeyDown(Key.Right)
                ? 1.0f
                : Gamepad.GetAxis(GamepadAxis.RightTrigger),
            _ => 0.0f
        };
    }

    public void Update(GameWindow window, SystemVariableLookup systemVariables)
    {
        Snapshot = window.PumpEvents();
        ProcessGamepadEvents();

        PollVKey(VirtualKey.Enter);
        PollVKey(VirtualKey.Advance);
        PollVKey(VirtualKey.Back);
        PollVKey(VirtualKey.Left);
        PollVKey(VirtualKey.Up);
        PollVKey(VirtualKey.Right);
        PollVKey(VirtualKey.Down);
        PollVKey(VirtualKey.Skip);

        SystemVariableLookup sys = systemVariables;
        set(ref sys.RightButtonDown, VKeyState(VirtualKey.Back));
        pollController(GamepadButton.Start, ref sys.X360StartButtonDown);
        set(ref sys.X360AButtonDown, VKeyState(VirtualKey.Advance));
        set(ref sys.X360BButtonDown, VKeyState(VirtualKey.Back));
        pollController(GamepadButton.NorthY, ref sys.X360YButtonDown);
        set(ref sys.X360LeftButtonDown, VKeyState(VirtualKey.Left));
        set(ref sys.X360UpButtonDown, VKeyState(VirtualKey.Down));
        set(ref sys.X360RightButtonDown, VKeyState(VirtualKey.Right));
        set(ref sys.X360DownButtonDown, VKeyState(VirtualKey.Down));
        pollController(GamepadButton.LeftShoulder, ref sys.X360LbButtonDown);
        pollController(GamepadButton.RightShoulder, ref sys.X360RbButtonDown);

        _advance = VKeyDown(VirtualKey.Advance);

        static void set(ref ConstantValue target, bool value)
        {
            target = ConstantValue.Boolean(value);
        }

        void pollController(GamepadButton button, ref ConstantValue val)
        {
            bool down = Gamepad.ButtonState(button);
            set(ref val, down);
        }
    }

    private void ProcessGamepadEvents()
    {
        Gamepad.Refresh();
        foreach (GamepadEvent ev in Snapshot.GamepadEvents)
        {
            switch (ev.Type)
            {
                case GamepadEventType.Added:
                {
                    if (Gamepad is NullGamepad)
                    {
                        Gamepad = new Sdl3Gamepad(ev.GamepadId);
                    }
                    break;
                }
                case GamepadEventType.Removed:
                {
                    if (Gamepad is Sdl3Gamepad gamepad && gamepad.InstanceId == ev.GamepadId)
                    {
                        Gamepad.Dispose();
                        Gamepad = new NullGamepad();
                    }
                    break;
                }
                case GamepadEventType.ButtonDown:
                case GamepadEventType.ButtonUp:
                case GamepadEventType.AxisMotion:
                {
                    Gamepad.HandleEvent(ev);
                    break;
                }
            }
        }
    }

    private bool VKeyState(InputSnapshot input, VirtualKey key)
    {
        return key switch
        {
            VirtualKey.Advance => input.MouseState(MouseButton.Left) |
                input.KeyState(Key.Return) |
                input.KeyState(Key.KeypadEnter) |
                input.KeyState(Key.Space) |
                Gamepad.ButtonState(GamepadButton.SouthA),
            VirtualKey.Back => input.MouseState(MouseButton.Right) |
                input.KeyState(Key.Backspace) |
                input.KeyState(Key.Escape) |
                Gamepad.ButtonState(GamepadButton.EastB),
            VirtualKey.Enter => input.MouseState(MouseButton.Left) |
                input.KeyState(Key.Return) |
                input.KeyState(Key.KeypadEnter) |
                Gamepad.ButtonState(GamepadButton.SouthA),
            VirtualKey.Left => input.KeyState(Key.Left) |
                Gamepad.ButtonState(GamepadButton.DPadLeft),
            VirtualKey.Up => input.KeyState(Key.Up) |
                input.WheelDelta.Y > 0 |
                Gamepad.ButtonState(GamepadButton.DPadUp),
            VirtualKey.Right => input.KeyState(Key.Right) |
                Gamepad.ButtonState(GamepadButton.DPadRight),
            VirtualKey.Down => input.KeyState(Key.Down) |
                input.WheelDelta.Y < 0 |
                Gamepad.ButtonState(GamepadButton.DPadDown),
            VirtualKey.Skip => input.KeyState(Key.LeftControl) | input.KeyState(Key.RightControl),
            _ => false
        };
    }

    private void PollVKey(VirtualKey vkey)
    {
        int index = (int)vkey;
        bool down = VKeyState(Snapshot, vkey);
        _newVkeys[index] = !_vkeyState[index] & down;
        _vkeyState[index] = down;
    }

    public void Dispose()
    {
        Gamepad.Dispose();
    }
}
