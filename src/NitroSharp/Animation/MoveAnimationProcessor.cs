using System.Numerics;
using NitroSharp.Graphics;

namespace NitroSharp.Logic.Systems
{
    internal class MoveAnimationProcessor : AnimationProcessor
    {
        public static void Process(World world, float deltaTime)
        {
            var animations = world.GetMoveAnimations();
            foreach (var beh in animations)
            {
                Entity entity = beh.Entity;
                ref var anim = ref beh.Behavior;
                Visuals table = world.GetTable<Visuals>(entity);

                ref var transform = ref table.TransformComponents.Mutate(entity);
                float progress = CalculateProgress(anim.Elapsed, anim.Duration);
                Vector3 delta = anim.Destination - anim.StartPosition;
                transform.Position = anim.StartPosition + delta * CalculateFactor(progress, anim.TimingFunction);

                anim.Elapsed += deltaTime;

                if (progress >= 1.0f)
                {
                    world.DetachMoveAnimation(entity);
                }
            }
        }

    }
}
