using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp;

internal readonly struct ResourcePool<T> : IDisposable
    where T : class
{
    private readonly Func<T> _resourceFactory;
    private readonly Action<T> _disposeFunc;
    private readonly List<T> _allResources;
    private readonly Queue<T> _pool;

    public ResourcePool(Func<T> resourceFactory, Action<T> disposeFunc, int initialSize)
    {
        _resourceFactory = resourceFactory;
        _disposeFunc = disposeFunc;
        _allResources = new List<T>(initialSize);
        _pool = new Queue<T>(initialSize);
        for (int i = 0; i < initialSize; i++)
        {
            T resource = resourceFactory();
            _allResources.Add(resource);
            _pool.Enqueue(resource);
        }
    }

    public T Rent()
    {
        if (!_pool.TryDequeue(out T? resource))
        {
            resource = _resourceFactory();
            _allResources.Add(resource);
        }

        Debug.Assert(_allResources.Count >= _pool.Count);
        return resource;
    }

    public void Return(T resource)
    {
        _pool.Enqueue(resource);
        Debug.Assert(_allResources.Count >= _pool.Count);
    }

    public void Dispose()
    {
        _pool.Clear();
        foreach (T resource in _allResources)
        {
            _disposeFunc(resource);
        }
        _allResources.Clear();
    }
}
