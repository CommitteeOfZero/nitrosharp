using NitroSharp.NsScript;
using NitroSharp.Foundation;

namespace NitroSharp
{
    public static class EntityExtensions
    {
        public static bool IsLocked(this Entity entity)
        {
            return entity.AdditionalProperties.TryGetValue("Locked", out object value) ? (bool)value : false;
        }

        public static void Lock(this Entity entity)
        {
            entity.AdditionalProperties["Locked"] = true;
        }

        public static void Unlock(this Entity entity)
        {
            entity.AdditionalProperties["Locked"] = false;
        }

        public static Entity WithPosition(this Entity entity, NsCoordinate x, NsCoordinate y)
        {
            NitroCore.SetPosition(entity.Transform, x, y);
            return entity;
        }
    }
}
