namespace NitroSharp
{
    internal class AttachedBehavior<T> where T : unmanaged
    {
        public Entity Entity;
        public T Behavior;
    }
}
