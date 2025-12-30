using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace NitroSharp.Input;

internal record struct Binding(
    Key? Key = null,
    MouseButton? MouseButton = null,
    GamepadButton? GamepadButton = null,
    GamepadAxis? GamepadAxis = null,
    bool WheelUp = false,
    bool WheelDown = false,
    bool Touch = false
);

internal readonly ref struct InputMapBuilder
{
    private readonly Dictionary<InputAction, List<Binding>> _bindings = [];

    public InputMapBuilder()
    {
    }

    public void Bind(InputAction action, Binding binding)
    {
        if (!_bindings.TryGetValue(action, out List<Binding>? bindings))
        {
            bindings = [];
            _bindings[action] = bindings;
        }

        bindings.Add(binding);
    }

    public InputMap Build() => new(_bindings.ToFrozenDictionary());
}

internal sealed class InputMap(FrozenDictionary<InputAction, List<Binding>> bindings)
{
    public float GetValue(InputAction action, InputSnapshot snapshot, Gamepad gamepad)
    {
        if (!bindings.TryGetValue(action, out List<Binding>? value)) { return 0; }

        float max = 0;
        foreach (Binding binding in value)
        {
            float val = 0;
            if (binding.Key is { } key && snapshot.KeyState(key)
                || binding.MouseButton is { } button && snapshot.MouseState(button)
                || binding.GamepadButton is { } gamepadButton && gamepad.ButtonState(gamepadButton)
                || binding.WheelUp && snapshot.WheelDelta.Y > 0
                || binding.WheelDown && snapshot.WheelDelta.Y < 0
                || binding.Touch && snapshot.ActiveTouches.Count > 0)
            {
                val = 1.0f;
            }
            else if (binding.GamepadAxis is { } axis)
            {
                val = gamepad.GetAxis(axis);
            }

            if (Math.Abs(val) > Math.Abs(max)) { max = val; }
        }

        return max;
    }
}
