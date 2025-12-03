using System;
using Veldrid;

namespace NitroSharp.Graphics.Core;

internal readonly struct PrimitiveTemplate(ushort[] indices, uint verticesPerInstance)
{
    public readonly ushort[] Indices = indices;
    public readonly uint VerticesPerInstance = verticesPerInstance;

    public uint IndicesPerInstance => (uint)Indices.Length;
}

internal readonly ref struct PrimitiveSlice<TVertex>(
    GpuListSlice<TVertex> vertices,
    GpuListSlice<ushort> indices,
    uint indexBase)
    where TVertex : unmanaged
{
    public readonly GpuListSlice<TVertex> Vertices = vertices;
    public readonly GpuListSlice<ushort> Indices = indices;
    public readonly uint IndexBase = indexBase;
}

internal sealed class PrimitiveBatch<TVertex> : IDisposable
    where TVertex : unmanaged
{
    private readonly PrimitiveTemplate _template;
    private readonly GpuList<TVertex> _vertices;
    private readonly GpuList<ushort> _indices;

    public PrimitiveBatch(
        GraphicsDevice graphicsDevice,
        in PrimitiveTemplate primitiveTemplate,
        uint initialCapacity)
    {
        _template = primitiveTemplate;
        _vertices = new GpuList<TVertex>(
            graphicsDevice,
            BufferUsage.VertexBuffer,
            initialCapacity * primitiveTemplate.VerticesPerInstance
        );
        _indices = new GpuList<ushort>(
            graphicsDevice,
            BufferUsage.IndexBuffer,
            initialCapacity * primitiveTemplate.IndicesPerInstance
        );
    }

    private uint Count => _vertices.Count / _template.VerticesPerInstance;

    public void Begin()
    {
        _vertices.Begin();
        _indices.Begin();
    }

    public PrimitiveSlice<TVertex> Append(ReadOnlySpan<TVertex> vertices)
    {
        if (vertices.Length != _template.VerticesPerInstance)
        {
            unexpectedLength();
        }

        uint oldCount = Count;
        GpuListSlice<TVertex> dstVertices = _vertices.Append((uint)vertices.Length);
        vertices.CopyTo(dstVertices.Data);
        GpuListSlice<ushort> dstIndices = _indices.Append(_template.IndicesPerInstance);
        for (int i = 0; i < dstIndices.Data.Length; i++)
        {
            dstIndices.Data[i] = (ushort)(_template.Indices[i] + oldCount * vertices.Length);
        }

        return new PrimitiveSlice<TVertex>(
            dstVertices,
            dstIndices,
            oldCount * _template.IndicesPerInstance
        );

        static void unexpectedLength()
        {
            throw new ArgumentException("Unexpected number of vertices.");
        }
    }

    public void End(CommandList cl)
    {
        _vertices.End(cl);
        _indices.End(cl);
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _indices.Dispose();
    }
}
