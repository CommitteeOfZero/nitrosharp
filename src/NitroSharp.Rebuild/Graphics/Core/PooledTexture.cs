using System;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Core;

internal readonly struct PooledTexture(ResourcePool<Texture> pool, Texture texture) : IDisposable
{
    public Texture Get() => texture;

    public void Dispose()
    {
        pool.Return(texture);
    }
}
