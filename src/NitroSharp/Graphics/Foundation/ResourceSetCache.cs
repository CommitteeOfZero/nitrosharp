using System;
using System.Collections.Generic;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ResourceSetCache : IDisposable
    {
        private readonly Dictionary<ResourceSetDescription, ResourceSet> _cache;
        private readonly ResourceFactory _factory;

        public ResourceSetCache(ResourceFactory resourceFactory)
        {
            _factory = resourceFactory;
            _cache = new Dictionary<ResourceSetDescription, ResourceSet>(256);
        }

        public ResourceSet GetResourceSet(ref ResourceSetDescription desc)
        {
            if (!_cache.TryGetValue(desc, out ResourceSet resourceSet))
            {
                resourceSet = _factory.CreateResourceSet(ref desc);
                _cache[desc] = resourceSet;
            }

            return resourceSet;
        }

        public void Dispose()
        {
            foreach (ResourceSet resourceSet in _cache.Values)
            {
                resourceSet.Dispose();
            }
        }
    }
}
