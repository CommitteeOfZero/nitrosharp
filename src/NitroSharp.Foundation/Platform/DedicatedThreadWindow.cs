// Based on code from the Veldrid open source library
// https://github.com/mellinoe/veldrid

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NitroSharp.Foundation.Platform
{
    public class DedicatedThreadWindow : GameWindowBase
    {
        private InputSnapshot _snapshotBackBuffer = new InputSnapshot();
        private volatile bool _shouldClose;

        public DedicatedThreadWindow()
        {
        }

        public DedicatedThreadWindow(string title, int desiredWidth, int desiredHeight, WindowState state)
            : base(title, desiredWidth, desiredHeight, state)
        {
        }

        public double PollIntervalInMs { get; set; } = 1000.0 / 120.0;

        protected override void Create(string title, int desiredWidth, int desiredHeight, WindowState state)
        {
            using (ManualResetEvent signal = new ManualResetEvent(false))
            {
                var wp = new WindowParameters(title, desiredWidth, desiredHeight, state, signal);
                Task.Factory.StartNew(WindowOwnerRoutine, wp, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                signal.WaitOne();
            }
        }

        protected override InputSnapshot GetAvailableSnapshot()
        {
            _snapshotBackBuffer.Clear();
            var snapshot = Interlocked.Exchange(ref _currentSnapshot, _snapshotBackBuffer);
            _snapshotBackBuffer = snapshot;
            return snapshot;
        }

        public override void Close()
        {
            _shouldClose = true;
        }

        private void WindowOwnerRoutine(object state)
        {
            var wp = (WindowParameters)state;
            base.Create(wp.Title, wp.Width, wp.Height, wp.WindowState);
            wp.Signal.Set();

            double previousPollTimeMs = 0;
            var sw = Stopwatch.StartNew();

            while (_nativeWindow.Exists)
            {
                if (_shouldClose)
                {
                    _nativeWindow.Close();
                }

                double currentTimeMs = sw.ElapsedTicks * (1000.0d / Stopwatch.Frequency);
                if (currentTimeMs - previousPollTimeMs < PollIntervalInMs)
                {
                    Thread.Sleep(0);
                }
                else
                {
                    previousPollTimeMs = currentTimeMs;
                    //_nativeWindow.CursorVisible = _isCursorVisible;
                    _nativeWindow.ProcessEvents();
                }
            }
        }

        private sealed class WindowParameters
        {
            public WindowParameters(string title, int width, int height, WindowState state, ManualResetEvent signal)
            {
                Title = title;
                Width = width;
                Height = height;
                WindowState = state;
                Signal = signal;
            }

            public string Title { get; }
            public int Width { get; }
            public int Height { get; }
            public WindowState WindowState { get; }
            public ManualResetEvent Signal { get; }
        }
    }
}
