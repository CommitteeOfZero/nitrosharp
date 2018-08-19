namespace NitroSharp.Logic.Systems
{
    internal sealed class AnimationSystem : GameSystem
    {
        private readonly World _world;

        public AnimationSystem(World world)
        {
            _world = world;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            MoveAnimationProcessor.Process(_world, deltaTime);
            FadeAnimationProcessor.Process(_world, deltaTime);
            ZoomAnimationProcessor.Process(_world, deltaTime);
        }
    }
}
