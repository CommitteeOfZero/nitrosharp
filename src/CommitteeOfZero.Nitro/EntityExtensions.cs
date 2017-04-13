using MoeGame.Framework;

namespace CommitteeOfZero.Nitro
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
    }
}
