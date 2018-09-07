namespace NitroSharp
{
    internal sealed class AttachedBehaviorProcessor : GameSystem
    {
        private readonly World _world;

        public AttachedBehaviorProcessor(World world)
        {
            _world = world;
        }

        public override void Update(float deltaTime)
        {
            foreach (AttachedBehavior behavior in _world.AttachedBehaviors)
            {
                behavior.Update(_world, deltaTime);
            }
        }
    }
}
