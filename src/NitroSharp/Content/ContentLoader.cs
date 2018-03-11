using System.IO;

namespace NitroSharp.Content
{
    public abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
