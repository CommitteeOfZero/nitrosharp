using NitroSharp.Graphics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Logic.Systems
{
    internal class FadeAnimationProcessor : AnimationProcessor
    {
        public static void Process(World world, float deltaTime)
        {
            var animations = world.GetFadeAnimations();
            foreach (var beh in animations)
            {

                Entity entity = beh.Entity;
                ref var anim = ref beh.Behavior;
                Visuals table = world.GetTable<Visuals>(entity);

                ref RgbaFloat color = ref table.Colors.Mutate(entity);
                float progress = CalculateProgress(anim.Elapsed, anim.Duration);
                float delta = anim.FinalOpacity - anim.InitialOpacity;
                color.SetAlpha(anim.InitialOpacity + delta * CalculateFactor(progress, anim.TimingFunction));

                anim.Elapsed += deltaTime;

                if (progress >= 1.0f)
                {
                    world.DetachFadeAnimation(entity);
                }
            }
        }

    }
}
