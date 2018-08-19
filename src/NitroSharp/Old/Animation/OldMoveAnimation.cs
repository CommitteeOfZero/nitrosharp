using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal sealed class OldMoveAnimation : Vector3Animation<OldTransform>
    {
        public OldMoveAnimation(OldTransform transform, Vector3 startPosition, Vector3 destination,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (t, v) => t.Position = v, startPosition, destination, duration, timingFunction)
        {
        }
    }
}
