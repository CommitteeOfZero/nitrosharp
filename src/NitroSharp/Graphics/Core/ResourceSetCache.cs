using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrid;

namespace NitroSharp.Graphics.Core
{
    internal readonly struct ResourceSetKey : IEquatable<ResourceSetKey>
    {
        public readonly ResourceLayout ResourceLayout;

        private readonly BindableResource _resource0;
        private readonly BindableResource? _resource1;
        private readonly BindableResource? _resource2;
        private readonly BindableResource? _resource3;

        public ResourceSetKey(ResourceLayout layout, BindableResource res) : this()
        {
            (ResourceLayout, _resource0) = (layout, res);
        }

        public ResourceSetKey(
             ResourceLayout layout,
             BindableResource res0,
             BindableResource res1) : this()
        {
            (ResourceLayout, _resource0, _resource1) = (layout, res0, res1);
        }

        public ResourceSetKey(
             ResourceLayout layout,
             BindableResource res0,
             BindableResource res1,
             BindableResource res2) : this()
        {
            (ResourceLayout, _resource0, _resource1, _resource2) = (layout, res0, res1, res2);
        }

        public ResourceSetKey(
            ResourceLayout layout,
            BindableResource res0,
            BindableResource? res1,
            BindableResource? res2,
            BindableResource? res3)
        {
            ResourceLayout = layout;
            _resource0 = res0;
            _resource1 = res1;
            _resource2 = res2;
            _resource3 = res3;
        }

        public ResourceSetKey(ResourceLayout layout, ReadOnlySpan<BindableResource> resources)
            : this()
        {
            ResourceLayout = layout;
            if (resources.Length > 4)
            {
                throw new ArgumentException($"{nameof(ResourceSetCache)} can only contain" +
                    "ResourceSets that include up to 4 resources."
                );
            }
            _resource0 = resources[0];
            if (resources.Length > 1)
            {
                _resource1 = resources[1];
            }
            if (resources.Length > 2)
            {
                _resource2 = resources[2];
            }
            if (resources.Length > 3)
            {
                _resource3 = resources[3];
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                ResourceLayout,
                _resource0,
                _resource1,
                _resource2,
                _resource3
            );
        }

        public bool Equals(ResourceSetKey other)
        {
            return ReferenceEquals(ResourceLayout, other.ResourceLayout)
                && ReferenceEquals(_resource0, other._resource0)
                && ReferenceEquals(_resource1, other._resource1)
                && ReferenceEquals(_resource2, other._resource2)
                && ReferenceEquals(_resource3, other._resource3);
        }

        public BindableResource GetResource(int index)
        {
            Debug.Assert(index < GetResourceCount());
            return index switch
            {
                0 => _resource0,
                1 => _resource1!,
                2 => _resource2!,
                3 => _resource3!,
                _ => ThrowHelper.Unreachable<BindableResource>()
            };
        }

        public uint GetResourceCount()
        {
            return (Resource1: _resource1, Resource2: _resource2, Resource3: _resource3) switch
            {
                (null, _, _) => 1u,
                ({ }, null, _) => 2u,
                ({ }, { }, null) => 3u,
                _ => 4u
            };
        }
    }

    internal sealed class ResourceSetCache : IDisposable
    {
        private readonly struct CacheEntry
        {
            public readonly ResourceSet ResourceSet;
            public readonly FrameStamp LastAccess;

            public CacheEntry(ResourceSet resourceSet, FrameStamp lastAccess)
            {
                (ResourceSet, LastAccess) = (resourceSet, lastAccess);
            }
        }

        private readonly Dictionary<ResourceSetKey, CacheEntry> _cache;
        private readonly List<ResourceSetKey> _entriesToEvict;
        private readonly ResourceFactory _factory;
        private FrameStamp _lastGC;
        private FrameStamp _now;

        private ResourceSetDescription _desc;
        private readonly BindableResource[] _array1, _array2, _array3, _array4;

        public ResourceSetCache(ResourceFactory resourceFactory)
        {
            _factory = resourceFactory;
            _cache = new Dictionary<ResourceSetKey, CacheEntry>(512);
            _entriesToEvict = new List<ResourceSetKey>();
            _array1 = new BindableResource[1];
            _array2 = new BindableResource[2];
            _array3 = new BindableResource[3];
            _array4 = new BindableResource[4];
        }

        public void BeginFrame(in FrameStamp frameStamp)
        {
            _now = frameStamp;
            if (SecondsElapsed(_lastGC, _now) >= 5)
            {
                foreach (KeyValuePair<ResourceSetKey, CacheEntry> entry in _cache)
                {
                    if (SecondsElapsed(entry.Value.LastAccess, frameStamp) >= 5)
                    {
                        _entriesToEvict.Add(entry.Key);
                    }
                }

                foreach (ResourceSetKey key in _entriesToEvict)
                {
                    _cache.Remove(key);
                }

                _entriesToEvict.Clear();
                _lastGC = frameStamp;
            }
        }

        public void EndFrame()
        {
        }

        private static float SecondsElapsed(in FrameStamp t1, in FrameStamp t2)
        {
            return (t2.StopwatchTicks - t1.StopwatchTicks) / (float)Stopwatch.Frequency;
        }

        public ResourceSet GetResourceSet(in ResourceSetKey key)
        {
            if (!_cache.TryGetValue(key, out CacheEntry cacheEntry))
            {
                uint nbResources = key.GetResourceCount();
                BindableResource[] resources = GetArray(nbResources);
                for (int i = 0; i < nbResources; i++)
                {
                    resources[i] = key.GetResource(i);
                }

                _desc.Layout = key.ResourceLayout;
                _desc.BoundResources = resources;
                ResourceSet rs = _factory.CreateResourceSet(ref _desc);
                _cache[key] = cacheEntry = new CacheEntry(rs, _now);
            }

            return cacheEntry.ResourceSet;
        }

        private BindableResource[] GetArray(uint length) => length switch
        {
            1 => _array1,
            2 => _array2,
            3 => _array3,
            4 => _array4,
            _ => ThrowHelper.Unreachable<BindableResource[]>()
        };

        private void Clear()
        {
            foreach (KeyValuePair<ResourceSetKey, CacheEntry> entry in _cache)
            {
                entry.Value.ResourceSet.Dispose();
            }
            _cache.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
