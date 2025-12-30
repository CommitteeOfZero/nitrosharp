using System;
using static SDL.SDL_Keymod;

namespace NitroSharp.Input;

[Flags]
public enum ModifierKeys : ushort
{
    None = SDL_KMOD_NONE,
    LeftShift = SDL_KMOD_LSHIFT,
    RightShift = SDL_KMOD_RSHIFT,
    LeftControl = SDL_KMOD_LCTRL,
    RightControl = SDL_KMOD_RCTRL,
    LeftAlt = SDL_KMOD_LALT,
    RightAlt = SDL_KMOD_RALT,
    LeftGui = SDL_KMOD_LGUI,
    RightGui = SDL_KMOD_RGUI,
    Num = SDL_KMOD_NUM,
    Caps = SDL_KMOD_CAPS,
    Mode = SDL_KMOD_MODE
}
