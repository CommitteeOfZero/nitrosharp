namespace NitroSharp
{
    internal abstract class AttachedBehavior
    {
        protected AttachedBehavior(Entity entity)
        {
            Entity = entity;
        }

        public Entity Entity { get; }

        public abstract void Update(World world, float deltaTime);
    }
}
