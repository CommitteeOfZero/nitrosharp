using System;
using System.IO;

namespace NitroSharp.Content
{
    internal abstract class ContentLoader : IDisposable
    {
        public abstract object Load(Stream stream);

        public virtual void Dispose()
        {
        }
    }
}
