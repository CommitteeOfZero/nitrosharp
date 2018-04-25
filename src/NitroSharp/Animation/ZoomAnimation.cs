using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal sealed class ZoomAnimation : Vector3Animation<Transform>
    {
        public ZoomAnimation(
            Transform transform, Vector3 initialScale, Vector3 finalScale,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (t, v) => t.Scale = v, initialScale, finalScale, duration, timingFunction)
        {
        }
    }
}
