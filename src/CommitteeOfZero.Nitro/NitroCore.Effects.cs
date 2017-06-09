using CommitteeOfZero.Nitro.Graphics;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Animation;
using System;
using System.Numerics;
using CommitteeOfZero.Nitro.Foundation.Graphics;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        public override void Fade(string entityName, TimeSpan duration, NsRational opacity, bool wait)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                FadeCore(entity, duration, opacity, wait);
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void FadeCore(Entity entity, TimeSpan duration, NsRational opacity, bool wait)
        {
            float adjustedOpacity = opacity.Rebase(1.0f);
            var visual = entity.GetComponent<Visual>();
            if (duration > TimeSpan.Zero)
            {
                Action<Component, float> propertySetter = (c, v) => (c as Visual).Opacity = v;
                var animation = new FloatAnimation(visual, propertySetter, visual.Opacity, adjustedOpacity, duration);
                entity.AddComponent(animation);
            }
            else
            {
                visual.Opacity = adjustedOpacity;
            }
        }

        public override void Move(string entityName, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, bool wait)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                MoveCore(entity, duration, x, y, easingFunction, wait);
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void MoveCore(Entity entity, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, bool wait)
        {
            float targetX = x.Origin == NsCoordinateOrigin.CurrentValue ? entity.Transform.Margin.X + x.Value : x.Value;
            float targetY = y.Origin == NsCoordinateOrigin.CurrentValue ? entity.Transform.Margin.Y + y.Value : y.Value;
            Vector2 destination = new Vector2(targetX, targetY);

            if (duration > TimeSpan.Zero)
            {
                void PropertySetter(Component c, Vector2 v) => (c as Transform).Margin = v;
                var animation = new Vector2Animation(entity.Transform, PropertySetter, entity.Transform.Margin, destination, duration);
                entity.AddComponent(animation);
            }
            else
            {
                entity.Transform.Margin = destination;
            }
        }

        public override void Zoom(string entityName, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, bool wait)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                ZoomCore(entity, duration, scaleX, scaleY, easingFunction, wait);
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void ZoomCore(Entity entity, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, bool wait)
        {
            scaleX = scaleX.Rebase(1.0f);
            scaleY = scaleY.Rebase(1.0f);

            if (duration > TimeSpan.Zero)
            {
                Vector2 final = new Vector2(scaleX, scaleY);
                if (entity.Transform.Scale == final)
                {
                    entity.Transform.Scale = new Vector2(0.0f, 0.0f);
                }

                Action<Component, Vector2> propertySetter = (c, v) => (c as Transform).Scale = v;
                var animation = new Vector2Animation(entity.Transform, propertySetter, entity.Transform.Scale, final, duration, (TimingFunction)easingFunction);
                entity.AddComponent(animation);
            }
            else
            {
                entity.Transform.Scale = new Vector2(scaleX, scaleY);
            }
        }

        public override void DrawTransition(string sourceEntityName, TimeSpan duration, NsRational initialOpacity,
            NsRational finalOpacity, NsRational feather, string maskFileName, bool wait)
        {
            initialOpacity = initialOpacity.Rebase(1.0f);
            finalOpacity = finalOpacity.Rebase(1.0f);

            initialOpacity = initialOpacity.Rebase(1.0f);
            finalOpacity = finalOpacity.Rebase(1.0f);

            foreach (var entity in _entities.Query(sourceEntityName))
            {
                SetupTransition(entity, duration, initialOpacity, finalOpacity, feather, maskFileName);
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void SetupTransition(Entity entity, TimeSpan duration, NsRational initialOpacity,
            NsRational finalOpacity, NsRational feather, string maskFileName)
        {
            var visual = entity.GetComponent<Visual>();
            if (visual != null)
            {
                var transitionSource = visual is Sprite sprite ?
                    (FadeTransition.IPixelSource)new FadeTransition.ImageSource(_content.Get<TextureAsset>(sprite.Source.Id))
                    : new FadeTransition.SolidColorSource(visual.Color);

                var transition = new FadeTransition(transitionSource, _content.Get<TextureAsset>(maskFileName));
                transition.Priority = visual.Priority;
                Action<Component, float> propertySetter = (c, v) => (c as FadeTransition).Opacity = v;
                var animation = new FloatAnimation(transition, propertySetter, initialOpacity, finalOpacity, duration);

                animation.Completed += (o, args) =>
                {
                    if (visual is Sprite originalSprite)
                    {
                        originalSprite.Source = _content.Get<TextureAsset>(originalSprite.Source.Id);
                    }

                    entity.RemoveComponent(transition);
                    entity.AddComponent(visual);
                };

                entity.RemoveComponent(visual);
                entity.AddComponent(transition);
                entity.AddComponent(animation);
            }
        }
    }
}
