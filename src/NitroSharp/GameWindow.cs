using System;
using System.Numerics;
using System.Threading;
using Veldrid;

namespace NitroSharp
{
    public interface GameWindow : IDisposable
    {
        SwapchainSource SwapchainSource { get; }
        AutoResetEvent Mobile_HandledSurfaceDestroyed { get; }
        PhysicalSize Size { get; }
        Scale<DesignPixel, ScreenPixel> ScaleFactor { get; }
        bool Exists { get; }

        event Action Resized;
        event Action<SwapchainSource>? Mobile_SurfaceCreated;
        event Action? Mobile_SurfaceDestroyed;
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
}
