using System.Numerics;
using NitroSharp.Graphics;

namespace NitroSharp.Logic.Systems
{
    internal class ZoomAnimationProcessor : AnimationProcessor
    {
        public static void Process(World world, float deltaTime)
        {
           var animations = world.GetZoomAnimations();
            foreach (var beh in animations)
            {
                Entity entity = beh.Entity;
                ref var anim = ref beh.Behavior;
                Visuals table = world.GetTable<Visuals>(entity);

                ref var transform = ref table.TransformComponents.Mutate(entity);
                float progress = CalculateProgress(anim.Elapsed, anim.Duration);
                Vector3 delta = anim.FinalScale - anim.InitialScale;
                transform.Scale = anim.InitialScale + delta * CalculateFactor(progress, anim.TimingFunction);

                anim.Elapsed += deltaTime;

                if (progress >= 1.0f)
                {
                    world.DetachZoomAnimation(entity);
                }
            }
        }

    }
}
