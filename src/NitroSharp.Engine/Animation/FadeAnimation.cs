using System;
using NitroSharp.Foundation.Animation;
using NitroSharp.Graphics;

namespace NitroSharp.Animation
{
    public sealed class FadeAnimation : FloatAnimation
    {
        public FadeAnimation(Visual target, float initialOpacity, float finalOpacity,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(target, (_, v) => target.Opacity = v, initialOpacity, finalOpacity, duration, timingFunction)
        {
        }
    }
}
