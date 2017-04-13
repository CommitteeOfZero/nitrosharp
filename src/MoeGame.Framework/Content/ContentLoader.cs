using System.IO;

namespace MoeGame.Framework.Content
{
    public abstract class ContentLoader
    {
        public abstract object Load(Stream stream);
    }
}
