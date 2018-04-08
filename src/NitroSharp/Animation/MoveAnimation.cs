using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal sealed class MoveAnimation : Vector3Animation<Transform>
    {
        public MoveAnimation(Transform transform, Vector3 startPosition, Vector3 destination,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (t, v) => t.Position = v, startPosition, destination, duration, timingFunction)
        {
        }
    }
}
