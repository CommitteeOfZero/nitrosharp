using System;
using System.Numerics;
using NitroSharp.Foundation;
using NitroSharp.Foundation.Animation;

namespace NitroSharp.Animation
{
    public sealed class MoveAnimation : Vector2Animation
    {
        public MoveAnimation(Transform transform, Vector2 startPosition, Vector2 destination,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (_, v) => transform.Margin = v, startPosition, destination, duration, timingFunction)
        {
        }
    }
}
