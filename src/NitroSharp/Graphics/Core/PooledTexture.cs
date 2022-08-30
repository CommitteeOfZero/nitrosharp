using System;
using Veldrid;

namespace NitroSharp.Graphics.Core;

internal readonly struct PooledTexture : IDisposable
{
    private readonly ResourcePool<Texture> _pool;
    private readonly Texture _texture;

    public PooledTexture(ResourcePool<Texture> pool, Texture texture)
    {
        _pool = pool;
        _texture = texture;
    }

    public Texture Get() => _texture;

    public void Dispose()
    {
        _pool.Return(_texture);
    }
}
