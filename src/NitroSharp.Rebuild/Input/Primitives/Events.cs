using System.Numerics;

namespace NitroSharp.Input;

public readonly record struct KeyEvent(
    uint Timestamp,
    uint WindowId,
    bool Down,
    bool Repeat,
    Key Physical,
    VKey Virtual,
    ModifierKeys Modifiers);

public readonly record struct MouseButtonEvent(
    uint Timestamp,
    uint WindowID,
    MouseButton MouseButton,
    bool Down,
    byte Clicks);

public enum GamepadEventKind
{
    Added,
    Removed,
    ButtonDown,
    ButtonUp,
    AxisMotion
}

public readonly record struct GamepadEvent(
    GamepadEventKind Kind,
    uint GamepadId,
    GamepadButton Button,
    GamepadAxis Axis,
    float AxisValue);

public enum TouchEventKind
{
    Down,
    Up,
    Motion
}

public readonly record struct TouchEvent(
    uint Timestamp,
    TouchEventKind Kind,
    ulong FingerId,
    Vector2 Position,
    float Pressure);

public readonly record struct TouchPoint(Vector2 Position, float Pressure);
