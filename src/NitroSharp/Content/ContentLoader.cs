using System;
using System.IO;

namespace NitroSharp.Content
{
    internal abstract class ContentLoader : IDisposable
    {
        public ContentManager Content { get; }

        protected ContentLoader(ContentManager contentManager)
        {
            Content = contentManager;
        }

        public abstract object Load(Stream stream);

        public virtual void Dispose()
        {
        }
    }
}
