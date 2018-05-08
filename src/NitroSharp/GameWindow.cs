using System;
using System.Threading;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp
{
    public interface GameWindow
    {
        SwapchainSource SwapchainSource { get; }
        AutoResetEvent Mobile_HandledSurfaceDestroyed { get; }
        Size Size { get; }
        bool Exists { get; }

        event Action Resized;
        event Action<SwapchainSource> Mobile_SurfaceCreated;
        event Action Mobile_SurfaceDestroyed;
        //event Action Destroyed;

        InputSnapshot PumpEvents();
    }
}
