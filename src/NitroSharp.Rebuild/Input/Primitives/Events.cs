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

public enum GamepadEventType
{
    Added,
    Removed,
    ButtonDown,
    ButtonUp,
    AxisMotion
}

public readonly record struct GamepadEvent(
    GamepadEventType Type,
    uint GamepadId,
    GamepadButton Button,
    GamepadAxis Axis,
    float AxisValue);
