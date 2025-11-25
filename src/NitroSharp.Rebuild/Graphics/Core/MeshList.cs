using System;
using Veldrid;

namespace NitroSharp.Graphics.Core;

internal readonly struct MeshDescription(ushort[] indices, uint verticesPerMesh)
{
    public readonly ushort[] Indices = indices;
    public readonly uint VerticesPerMesh = verticesPerMesh;

    public uint IndicesPerMesh => (uint)Indices.Length;
}

internal readonly ref struct Mesh<TVertex>(
    GpuListSlice<TVertex> vertices,
    GpuListSlice<ushort> indices,
    uint indexBase)
    where TVertex : unmanaged
{
    public readonly GpuListSlice<TVertex> Vertices = vertices;
    public readonly GpuListSlice<ushort> Indices = indices;
    public readonly uint IndexBase = indexBase;
}

internal sealed class MeshList<TVertex> : IDisposable
    where TVertex : unmanaged
{
    private readonly MeshDescription _meshDesc;
    private readonly GpuList<TVertex> _vertices;
    private readonly GpuList<ushort> _indices;

    public MeshList(
        GraphicsDevice graphicsDevice,
        in MeshDescription meshDescription,
        uint initialCapacity)
    {
        _meshDesc = meshDescription;
        _vertices = new GpuList<TVertex>(
            graphicsDevice,
            BufferUsage.VertexBuffer,
            initialCapacity * meshDescription.VerticesPerMesh
        );
        _indices = new GpuList<ushort>(
            graphicsDevice,
            BufferUsage.IndexBuffer,
            initialCapacity * meshDescription.IndicesPerMesh
        );
    }

    private uint Count => _vertices.Count / _meshDesc.VerticesPerMesh;

    public void Begin()
    {
        _vertices.Begin();
        _indices.Begin();
    }

    public Mesh<TVertex> Append(ReadOnlySpan<TVertex> vertices)
    {
        if (vertices.Length != _meshDesc.VerticesPerMesh)
        {
            unexpectedLength();
        }

        uint oldCount = Count;
        GpuListSlice<TVertex> dstVertices = _vertices.Append((uint)vertices.Length);
        vertices.CopyTo(dstVertices.Data);
        GpuListSlice<ushort> dstIndices = _indices.Append(_meshDesc.IndicesPerMesh);
        for (int i = 0; i < dstIndices.Data.Length; i++)
        {
            dstIndices.Data[i] = (ushort)(_meshDesc.Indices[i] + oldCount * vertices.Length);
        }

        return new Mesh<TVertex>(
            dstVertices,
            dstIndices,
            oldCount * _meshDesc.IndicesPerMesh
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
