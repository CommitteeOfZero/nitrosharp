namespace MoeGame.Framework
{
    public abstract class Component
    {
        public Entity Entity { get; private set; }
        public bool IsEnabled { get; set; } = true;

        internal void AttachToEntity(Entity entity)
        {
            Entity = entity;
        }

        //public virtual void OnAttached()
        //{
        //}

        //public virtual void OnRemoved()
        //{
        //}
    }
}
