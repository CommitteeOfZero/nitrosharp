using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp.Media
{
    internal struct Clock
    {
        private readonly Stopwatch _sw;
        private double _lastUpdated;

        public Clock(Stopwatch sw)
        {
            _sw = sw;
            LastPresentationTimestamp = 0;
            _lastUpdated = 0;
        }

        public double LastPresentationTimestamp { get; private set; }
        public double Drift => LastPresentationTimestamp - _lastUpdated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(double lastPresentationTimestamp)
        {
            LastPresentationTimestamp = lastPresentationTimestamp;
            _lastUpdated = GetTime();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Get()
        {
            double time = GetTime();
            return Drift + time - (time - _lastUpdated);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncTo(Clock other)
        {
            Set(other.Get());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetTime() => _sw.ElapsedTicks / (double)Stopwatch.Frequency;
    }
}
