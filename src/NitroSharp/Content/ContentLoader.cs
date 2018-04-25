using System.IO;

namespace NitroSharp.Content
{
    internal abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
