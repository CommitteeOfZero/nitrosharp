using System;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Utilities;

namespace NitroSharp.Logic.Systems
{
    internal abstract class AnimationProcessor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float CalculateProgress(float elapsed, float duration)
            => MathUtil.Clamp(elapsed / duration, 0.0f, 1.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float CalculateFactor(float progress, TimingFunction function)
        {
            switch (function)
            {
                case TimingFunction.QuadraticEaseIn:
                    return (float)Math.Pow(progress, 2);

                case TimingFunction.CubicEaseIn:
                    return (float)Math.Pow(progress, 3);

                case TimingFunction.QuarticEaseIn:
                    return (float)Math.Pow(progress, 4);

                case TimingFunction.QuadraticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 2);

                case TimingFunction.CubicEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 3);

                case TimingFunction.QuarticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 4);

                case TimingFunction.SineEaseIn:
                    return 1.0f - (float)Math.Cos(progress * Math.PI * 0.5f);

                case TimingFunction.SineEaseOut:
                    return (float)Math.Sin(progress * Math.PI * 0.5f);

                case TimingFunction.SineEaseInOut:
                    return 0.5f * (1.0f - (float)Math.Cos(progress * Math.PI));

                case TimingFunction.SineEaseOutIn:
                    return (float)(Math.Acos(1.0f - progress * 2.0f) / Math.PI);

                case TimingFunction.Linear:
                default:
                    return progress;
            }
        }
    }
}
