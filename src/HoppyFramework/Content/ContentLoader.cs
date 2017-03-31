using System.IO;

namespace HoppyFramework.Content
{
    public abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
