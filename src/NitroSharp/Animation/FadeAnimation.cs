using System;
using NitroSharp.Graphics;

namespace NitroSharp.Animation
{
    internal sealed class FadeAnimation : FloatAnimation
    {
        public FadeAnimation(Visual target, float initialOpacity, float finalOpacity,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(target, (_, v) => target.Opacity = v, initialOpacity, finalOpacity, duration, timingFunction)
        {
        }
    }
}
