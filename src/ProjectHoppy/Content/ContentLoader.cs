using System.IO;

namespace ProjectHoppy.Content
{
    public abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
