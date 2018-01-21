using System;
using System.Numerics;
using NitroSharp.Foundation;
using NitroSharp.Foundation.Animation;

namespace NitroSharp.Animation
{
    public sealed class ZoomAnimation : Vector2Animation
    {
        public ZoomAnimation(Transform transform, Vector2 initialScale, Vector2 finalScale,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (_, v) => transform.Scale = v, initialScale, finalScale, duration, timingFunction)
        {
        }
    }
}
