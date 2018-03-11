using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal sealed class MoveAnimation : Vector2Animation
    {
        public MoveAnimation(Transform transform, Vector2 startPosition, Vector2 destination,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (_, v) => transform.Margin = v, startPosition, destination, duration, timingFunction)
        {
        }
    }
}
