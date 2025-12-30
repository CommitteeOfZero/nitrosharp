using SDL;

namespace NitroSharp.Input;

internal sealed unsafe class Sdl3Gamepad : Gamepad
{
    public static Sdl3Gamepad? SelectFirst()
    {
        int count;
        SDL_JoystickID* gamepads = SDL3.SDL_GetGamepads(&count);
        if (gamepads != null)
        {
            if (count > 0)
            {
                var gamepad = new Sdl3Gamepad((uint)gamepads[0]);
                SDL3.SDL_free(gamepads);
                return gamepad;
            }
            SDL3.SDL_free(gamepads);
        }
        return null;
    }

    private readonly SDL_Gamepad* _gamepad;

    public Sdl3Gamepad(uint instanceId)
    {
        _gamepad = SDL3.SDL_OpenGamepad((SDL_JoystickID)instanceId);
        InstanceId = instanceId;
        Name = SDL3.SDL_GetGamepadName(_gamepad) ?? "";
    }

    public uint InstanceId { get; }
    public override string Name { get; }

    public override void HandleEvent(in GamepadEvent ev)
    {
        if (ev.GamepadId == InstanceId)
        {
            base.HandleEvent(ev);
        }
    }

    public override void Dispose()
    {
        SDL3.SDL_CloseGamepad(_gamepad);
    }
}
