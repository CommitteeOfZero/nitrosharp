using NitroSharp.NsScript;

namespace NitroSharp
{
    internal static class OldEntityExtensions
    {
        public static bool IsLocked(this OldEntity entity)
        {
            return entity.AdditionalProperties.TryGetValue("Locked", out object value) ? (bool)value : false;
        }

        public static void Lock(this OldEntity entity)
        {
            entity.AdditionalProperties["Locked"] = true;
        }

        public static void Unlock(this OldEntity entity)
        {
            entity.AdditionalProperties["Locked"] = false;
        }

        //public static OldEntity WithPosition(this OldEntity entity, NsCoordinate x, NsCoordinate y)
        //{
        //    CoreLogic.SetPosition(ref entity.Transform, x, y);
        //    return entity;
        //}
    }
}
