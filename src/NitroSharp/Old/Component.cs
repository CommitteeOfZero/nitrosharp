namespace NitroSharp
{
    internal abstract class Component
    {
        public OldEntity Entity { get; private set; }
        public bool IsEnabled { get; set; } = true;

        internal bool IsScheduledForRemoval { get; set; }

        internal void AttachToEntity(OldEntity entity)
        {
            Entity = entity;
            OnAttached();
        }

        public virtual void OnAttached()
        {
        }

        public virtual void OnRemoved()
        {
        }
    }
}
