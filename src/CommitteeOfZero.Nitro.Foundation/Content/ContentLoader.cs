using System.IO;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public abstract class ContentLoader
    {
        public abstract bool IsSupportedContentType(BinaryReader reader);
        public abstract object Load(Stream stream);
    }
}
