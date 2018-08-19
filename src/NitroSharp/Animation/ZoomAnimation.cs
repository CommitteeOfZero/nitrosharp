using System.Numerics;
using NitroSharp.Animation;

namespace NitroSharp.Logic.Components
{
    internal struct ZoomAnimation
    {
        public Vector3 InitialScale;
        public Vector3 FinalScale;
        public float Duration;
        public float Elapsed;
        public TimingFunction TimingFunction;
    }
}
