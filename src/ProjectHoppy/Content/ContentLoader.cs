using System.Collections.Generic;
using System.IO;

namespace ProjectHoppy.Content
{
    public abstract class ContentLoader
    {
        public abstract IEnumerable<string> FileExtensions { get; }
        public abstract IEnumerable<string> FileSignatures { get; }
        public abstract object Load(Stream stream);
    }
}
