using System;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using NitroSharp;
using Veldrid;

namespace CowsHead.Android
{
    public partial class MainView : SurfaceView, ISurfaceHolderCallback, GameWindow
    {
        private SimpleInputSnapshot _privateSnapshot = new SimpleInputSnapshot();
        private SimpleInputSnapshot _privateBackbuffer = new SimpleInputSnapshot();
        private SimpleInputSnapshot _publicSnapshot = new SimpleInputSnapshot();

        public event Action<SwapchainSource> Mobile_SurfaceCreated;
        public event Action Mobile_SurfaceDestroyed;
        public event Action Resized;

        public MainView(Context context) : base(context)
        {
            Holder.AddCallback(this);
            Touch += OnTouch;
        }

        private void OnTouch(object sender, TouchEventArgs e)
        {
            switch (e.Event.Action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                    _privateSnapshot.MouseDown[0] = true;
                    var mouseEvent = new MouseEvent(MouseButton.Left, true);
                    _privateSnapshot.MouseEventsList.Add(mouseEvent);
                    break;
                case MotionEventActions.Up:
                    _privateSnapshot.MouseDown[0] = false;
                    mouseEvent = new MouseEvent(MouseButton.Left, false);
                    _privateSnapshot.MouseEventsList.Add(mouseEvent);
                    break;
            }
        }

        public SwapchainSource SwapchainSource { get; private set; }
        public bool Exists { get; private set; }
        public NitroSharp.Primitives.Size Size => new NitroSharp.Primitives.Size((uint)Width, (uint)Height);
        public AutoResetEvent Mobile_HandledSurfaceDestroyed { get; } = new AutoResetEvent(false);

        public InputSnapshot PumpEvents()
        {
            var snapshot = Interlocked.Exchange(ref _privateSnapshot, _privateBackbuffer);
            snapshot.CopyTo(_publicSnapshot);
            snapshot.Clear();
            return _publicSnapshot;
        }

        public void Disable()
        {
            Exists = false;
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            SwapchainSource = SwapchainSource.CreateAndroidSurface(holder.Surface.Handle, JNIEnv.Handle);
            Exists = true;
            Mobile_SurfaceCreated?.Invoke(SwapchainSource);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Mobile_SurfaceDestroyed?.Invoke();
            Mobile_HandledSurfaceDestroyed.WaitOne();
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            Resized?.Invoke();
        }

        public void OnPause()
        {
        }

        public void OnResume()
        {
        }
    }
}
