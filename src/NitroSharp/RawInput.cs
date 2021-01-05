using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

using static Veldrid.Sdl2.Sdl2Native;

#nullable enable

namespace NitroSharp
{
    internal sealed class RawInput : IDisposable
    {
        private readonly bool _desktopOS;

        private readonly HashSet<Key> _keyboardState = new();
        private readonly List<Key> _newKeys = new();

        private readonly bool[] _mouseState = new bool[13];
        private readonly bool[] _newMouseButtons = new bool[13];
        private Vector2 _prevMousePosition;

        public RawInput(GameWindow window)
        {
            Window = window;
            Snapshot = null!;
            _desktopOS = Window is DesktopWindow;
            Gamepad = new DebugGamepad(this);
            if (_desktopOS)
            {
                Sdl2Events.Subscribe(ProcessEvent);
            }
        }

        public GameWindow Window { get; }
        public Vector2 MouseDelta { get; private set; }
        public float WheelDelta { get; private set; }
        public InputSnapshot Snapshot { get; private set; }
        public Gamepad Gamepad { get; private set; }

        public void Update()
        {
            ProcessSnapshot(Window.PumpEvents());
            Sdl2Events.ProcessEvents();
        }

        public bool KeyState(Key key)
            => _keyboardState.Contains(key);

        public bool IsKeyDown(Key key)
            => _newKeys.Contains(key);

        public bool MouseState(MouseButton button)
            => _mouseState[(int)button];

        public bool IsMouseDown(MouseButton button)
            => _newMouseButtons[(int)button];

        private void ProcessEvent(ref SDL_Event evt)
        {
            switch (evt.type)
            {
                case SDL_EventType.ControllerDeviceRemoved:
                    Gamepad.Dispose();
                    Gamepad = Gamepad.Default(_desktopOS);
                    break;
                case SDL_EventType.ControllerDeviceAdded:
                    if (Gamepad.IsNullGamepad)
                    {
                        Gamepad = Gamepad.Default(_desktopOS);
                    }
                    break;
            }
        }

        private void ProcessSnapshot(InputSnapshot snapshot)
        {
            Snapshot = snapshot;
            _newKeys.Clear();
            _newMouseButtons.AsSpan().Clear();

            MouseDelta = snapshot.MousePosition - _prevMousePosition;
            _prevMousePosition = snapshot.MousePosition;
            WheelDelta = snapshot.WheelDelta;

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent evt = keyEvents[i];
                if (evt.Down)
                {
                    if (_keyboardState.Add(evt.Key))
                    {
                        _newKeys.Add(evt.Key);
                    }
                }
                else
                {
                    _keyboardState.Remove(evt.Key);
                    _newKeys.Remove(evt.Key);
                }
            }

            IReadOnlyList<MouseEvent> mouseEvents = snapshot.MouseEvents;
            for (int i = 0; i < mouseEvents.Count; i++)
            {
                MouseEvent evt = mouseEvents[i];
                int index = (int)evt.MouseButton;
                _newMouseButtons[index] = !_mouseState[index] & evt.Down;
                _mouseState[index] = evt.Down;
            }
        }

        private void ClearState()
        {
            _keyboardState.Clear();
            _newKeys.Clear();
            _mouseState.AsSpan().Clear();
            _newMouseButtons.AsSpan().Clear();
        }

        public void Dispose()
        {
            Gamepad.Dispose();
        }
    }

    internal abstract class Gamepad : IDisposable
    {
        public abstract string Name { get; }
        public abstract bool IsNullGamepad { get; }

        public abstract float GetAxis(SDL_GameControllerAxis axis);
        public abstract bool ButtonState(SDL_GameControllerButton button);
        public abstract bool IsButtonDown(SDL_GameControllerButton button);

        public static Gamepad Default(bool desktopOS)
        {
            if (desktopOS)
            {
                return SdlGamepad.SelectFirst() ?? (Gamepad)new NullGamepad();
            }

            return new NullGamepad();
        }

        public virtual void Dispose()
        {
        }
    }

    internal sealed class NullGamepad : Gamepad
    {
        public override string Name => "Null Gamepad";
        public override bool IsNullGamepad => true;

        public override float GetAxis(SDL_GameControllerAxis axis) => 0.0f;
        public override bool ButtonState(SDL_GameControllerButton button) => false;
        public override bool IsButtonDown(SDL_GameControllerButton button) => false;
    }

    internal sealed class DebugGamepad : Gamepad
    {
        private readonly RawInput _rawInput;

        public DebugGamepad(RawInput rawInput)
        {
            _rawInput = rawInput;
        }

        private static Key MapButton(SDL_GameControllerButton button) => button switch
        {
            SDL_GameControllerButton.X => Key.L,
            SDL_GameControllerButton.Y => Key.P,
            SDL_GameControllerButton.A => Key.Semicolon,
            SDL_GameControllerButton.B => Key.Quote,
            SDL_GameControllerButton.Start => Key.X,
            SDL_GameControllerButton.Back => Key.Z,
            SDL_GameControllerButton.LeftShoulder => Key.Number1,
            SDL_GameControllerButton.RightShoulder => Key.Number3,
            SDL_GameControllerButton.DPadLeft => Key.Left,
            SDL_GameControllerButton.DPadUp => Key.Up,
            SDL_GameControllerButton.DPadRight => Key.Right,
            SDL_GameControllerButton.DPadDown => Key.Down,
            _ => Key.Unknown
        };

        public override string Name => "Debug Gamepad";
        public override bool IsNullGamepad => false;

        public override float GetAxis(SDL_GameControllerAxis axis) => 0.0f;


        public override bool ButtonState(SDL_GameControllerButton button)
            => _rawInput.KeyState(MapButton(button));

        public override bool IsButtonDown(SDL_GameControllerButton button)
            => _rawInput.IsKeyDown(MapButton(button));
    }

    internal sealed class SdlGamepad : Gamepad
    {
        private readonly SDL_GameController _controller;
        private readonly int _instanceId;

        private readonly float[] _axisValues = new float[7];
        private readonly bool[] _buttonState = new bool[16];
        private readonly bool[] _newButtons = new bool[16];

        public unsafe SdlGamepad(int index)
        {
            _controller = SDL_GameControllerOpen(index);
            SDL_Joystick joystick = SDL_GameControllerGetJoystick(_controller);
            _instanceId = SDL_JoystickInstanceID(joystick);
            Name = Marshal.PtrToStringUTF8((IntPtr)SDL_GameControllerName(_controller)) ?? "";
            Sdl2Events.Subscribe(ProcessEvent);
        }

        public override string Name { get; }
        public override bool IsNullGamepad => false;

        public override float GetAxis(SDL_GameControllerAxis axis)
            => _axisValues[(int)axis];

        public override bool ButtonState(SDL_GameControllerButton button)
            => _buttonState[(int)button];

        public override bool IsButtonDown(SDL_GameControllerButton button)
            => _newButtons[(int)button];

        public static SdlGamepad? SelectFirst()
        {
            int numJoysticks = SDL_NumJoysticks();
            for (int i = 0; i < numJoysticks; i++)
            {
                if (SDL_IsGameController(i))
                {
                    return new SdlGamepad(i);
                }
            }

            return null;
        }

        private void ProcessEvent(ref SDL_Event evt)
        {
            static float normalize(short value)
            {
                return value < 0
                    ? -(value / (float)short.MinValue)
                    : (value / (float)short.MaxValue);
            }

            switch (evt.type)
            {
                case SDL_EventType.ControllerAxisMotion:
                    SDL_ControllerAxisEvent axisEvent = Unsafe.As<SDL_Event, SDL_ControllerAxisEvent>(ref evt);
                    if (axisEvent.which == _instanceId)
                    {
                        _axisValues[(int)axisEvent.axis] = normalize(axisEvent.value);
                    }
                    break;
                case SDL_EventType.ControllerButtonDown:
                case SDL_EventType.ControllerButtonUp:
                    SDL_ControllerButtonEvent buttonEvent = Unsafe.As<SDL_Event, SDL_ControllerButtonEvent>(ref evt);
                    if (buttonEvent.which == _instanceId)
                    {
                        int index = (int)buttonEvent.button;
                        bool down = buttonEvent.state == 1;
                        _newButtons[index] = !_buttonState[index] && down;
                        _buttonState[index] = down;
                    }
                    break;
            }
        }

        public override void Dispose()
        {
            Sdl2Events.Unsubscribe(ProcessEvent);
            SDL_GameControllerClose(_controller);
        }
    }
}
