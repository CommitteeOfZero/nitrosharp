using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NitroSharp.Utilities;

internal ref struct TimingScope : IDisposable
{
    private readonly ref TimeSpan _result;
    private readonly long _startTimestamp;
    private long _stopTimestamp;

    private TimingScope(long startTimestamp, ref TimeSpan elapsed)
    {
        _startTimestamp = startTimestamp;
        _stopTimestamp = 0;
        _result = ref elapsed;
    }

    public static TimingScope Start([UnscopedRef] out TimeSpan elapsed)
    {
        elapsed = TimeSpan.Zero;
        return new TimingScope(Stopwatch.GetTimestamp(), ref elapsed);
    }

    public void Dispose()
    {
        _stopTimestamp = Stopwatch.GetTimestamp();
        _result = Stopwatch.GetElapsedTime(_startTimestamp, _stopTimestamp);
    }
}
