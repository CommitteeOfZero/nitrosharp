using System.IO;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
