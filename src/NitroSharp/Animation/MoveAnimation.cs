using System;
using System.Numerics;
using NitroSharp.Animation;

namespace NitroSharp.Logic.Components
{
    internal struct MoveAnimation
    {
        public Vector3 StartPosition;
        public Vector3 Destination;
        public float Duration;
        public float Elapsed;
        public TimingFunction TimingFunction;
    }
}
