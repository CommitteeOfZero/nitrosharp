using System;

namespace NitroSharp.Input;

internal abstract class Gamepad : IDisposable
{
    private readonly float[] _axisValues = new float[7];
    private readonly bool[] _buttonState = new bool[20];
    private readonly bool[] _newButtons = new bool[20];

    public abstract string Name { get; }

    public virtual float GetAxis(GamepadAxis axis) => _axisValues[(int)axis];
    public virtual bool ButtonState(GamepadButton button) => _buttonState[(int)button];
    public virtual bool IsButtonDown(GamepadButton button) => _newButtons[(int)button];

    public virtual void HandleEvent(in GamepadEvent ev)
    {
        switch (ev.Type)
        {
            case GamepadEventType.ButtonDown:
            case GamepadEventType.ButtonUp:
                int btnIndex = (int)ev.Button;
                if (btnIndex >= 0 && btnIndex < _buttonState.Length)
                {
                    bool down = ev.Type == GamepadEventType.ButtonDown;
                    _newButtons[btnIndex] = !_buttonState[btnIndex] && down;
                    _buttonState[btnIndex] = down;
                }
                break;
            case GamepadEventType.AxisMotion:
                int axisIndex = (int)ev.Axis;
                if (axisIndex >= 0 && axisIndex < _axisValues.Length)
                {
                    _axisValues[axisIndex] = ev.AxisValue;
                }
                break;
        }
    }

    public static Gamepad Default(bool useSdl)
    {
        if (useSdl)
        {
            return Sdl3Gamepad.SelectFirst() ?? (Gamepad)new NullGamepad();
        }

        return new NullGamepad();
    }

    public virtual void Dispose()
    {
    }

    public void Refresh()
    {
        _newButtons.AsSpan().Clear();
    }
}

internal sealed class NullGamepad : Gamepad
{
    public override string Name => "Null Gamepad";

    public override float GetAxis(GamepadAxis axis) => 0.0f;
    public override bool ButtonState(GamepadButton button) => false;
    public override bool IsButtonDown(GamepadButton button) => false;
}

internal sealed class DebugGamepad(InputSnapshot input) : Gamepad
{
    private static Key MapButton(GamepadButton button) => button switch
    {
        GamepadButton.WestX => Key.L,
        GamepadButton.NorthY => Key.P,
        GamepadButton.SouthA => Key.Semicolon,
        // SDL_GameControllerButton.B => Key.Quote,
        GamepadButton.Start => Key.X,
        GamepadButton.Back => Key.Z,
        // SDL_GameControllerButton.LeftShoulder => Key.Number1,
        // SDL_GameControllerButton.RightShoulder => Key.Number3,
        GamepadButton.DPadLeft => Key.Left,
        GamepadButton.DPadUp => Key.Up,
        GamepadButton.DPadRight => Key.Right,
        GamepadButton.DPadDown => Key.Down,
        _ => Key.Unknown
    };

    public override string Name => "Debug Gamepad";

    public override float GetAxis(GamepadAxis axis) => 0.0f;

    public override bool ButtonState(GamepadButton button)
        => input.KeyState(MapButton(button));

    public override bool IsButtonDown(GamepadButton button)
        => input.IsKeyDown(MapButton(button));
}
