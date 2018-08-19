using NitroSharp.Animation;

namespace NitroSharp.Logic.Components
{
    internal struct FadeAnimation
    {
        public float InitialOpacity;
        public float FinalOpacity;
        public float Duration;
        public float Elapsed;
        public TimingFunction TimingFunction;
    }
}
