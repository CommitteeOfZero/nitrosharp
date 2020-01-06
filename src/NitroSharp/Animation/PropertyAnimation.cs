using System;
using NitroSharp.Experimental;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Utilities;

namespace NitroSharp.Animation
{
    internal abstract class PropertyAnimation
    {
        private float _elapsed;
        private bool _initialized;
        private bool _completed;

        protected PropertyAnimation(
            Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None,
            bool repeat = false)
        {
            Entity = entity;
            Duration = duration;
            EasingFunction = easingFunction;
            Repeat = repeat;
        }

        public Entity Entity { get; }
        public TimeSpan Duration { get; protected set; }
        public NsEasingFunction EasingFunction { get; }
        public bool Repeat { get; }

        public bool IsBlocking { get; set; }
        public ThreadContext WaitingThread { get; set; }

        public event Action Completed;

        protected float Elapsed => _elapsed;
        public float Progress => MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);
        public bool HasCompleted => _elapsed >= Duration.TotalMilliseconds;

        public World World { get; private set; }

        public bool Update(World world, float deltaMilliseconds)
        {
            if (_initialized)
            {
                _elapsed += deltaMilliseconds;
            }
            else
            {
                Setup(world);
                _initialized = true;
            }

            if (!_completed)
            {
                Advance(deltaMilliseconds);
                PostAdvance(world);
                return !_completed;
            }

            return false;
        }

        protected virtual void Setup(World world)
        {
            World = world;
        }

        protected virtual void Advance(float deltaMilliseconds)
        {
        }

        private void PostAdvance(World world)
        {
            if (HasCompleted)
            {
                OnCompleted(world);
                if (Repeat)
                {
                    _elapsed = 0;
                    _initialized = false;
                }
                else
                {
                    _completed = true;
                    Completed?.Invoke();
                    world.DeactivateAnimation(this);
                }
            }
        }

        protected virtual void OnCompleted(World world)
        {
        }

        protected float CalculateProgress(float elapsed, float duration)
            => MathUtil.Clamp(elapsed / duration, 0.0f, 1.0f);

        protected float CalculateFactor(float progress, NsEasingFunction function)
        {
            return function switch
            {
                NsEasingFunction.QuadraticEaseIn => MathF.Pow(progress, 2),
                NsEasingFunction.CubicEaseIn => MathF.Pow(progress, 3),
                NsEasingFunction.QuarticEaseIn => MathF.Pow(progress, 4),
                NsEasingFunction.QuadraticEaseOut => 1.0f - MathF.Pow(1.0f - progress, 2),
                NsEasingFunction.CubicEaseOut => 1.0f - MathF.Pow(1.0f - progress, 3),
                NsEasingFunction.QuarticEaseOut => 1.0f - MathF.Pow(1.0f - progress, 4),
                NsEasingFunction.SineEaseIn => 1.0f - MathF.Cos(progress * MathF.PI * 0.5f),
                NsEasingFunction.SineEaseOut => MathF.Sin(progress * MathF.PI * 0.5f),
                NsEasingFunction.SineEaseInOut => 0.5f * (1.0f - MathF.Cos(progress * MathF.PI)),
                NsEasingFunction.SineEaseOutIn => MathF.Acos(1.0f - progress * 2.0f) / MathF.PI,
                _ => progress,
            };
        }
    }
}
