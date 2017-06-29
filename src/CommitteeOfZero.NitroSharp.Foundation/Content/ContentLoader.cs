using System.IO;

namespace CommitteeOfZero.NitroSharp.Foundation.Content
{
    public abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
