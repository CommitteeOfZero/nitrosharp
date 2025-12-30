using System;
using System.Numerics;
using NitroSharp.Input;
using Veldrid;

namespace NitroSharp;

public interface GameWindow : IDisposable
{
    SwapchainSource SwapchainSource { get; }
    ScreenSizeU Size { get; }
    bool Exists { get; }

    event Action? CloseRequested;
    event Action Resized;
    //event Action Destroyed;

    InputSnapshot PumpEvents();
    void SetMousePosition(Vector2 pos);

    void SetCursor(SystemCursor cursor);
}

public enum SystemCursor
{
    Arrow,
    Hand,
    Wait
}
