using System;
using NitroSharp.Graphics;

namespace NitroSharp.Animation
{
    internal sealed class FadeAnimation : FloatAnimation<Visual>
    {
        public FadeAnimation(Visual target, float initialOpacity, float finalOpacity,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(target, (t, v) => t.Opacity = v, initialOpacity, finalOpacity, duration, timingFunction)
        {
        }
    }
}
