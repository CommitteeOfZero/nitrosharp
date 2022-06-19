using System;
using Veldrid;

namespace NitroSharp.Graphics.Core
{
    internal readonly struct MeshDescription
    {
        public readonly ushort[] Indices;
        public readonly uint VerticesPerMesh;

        public MeshDescription(ushort[] indices, uint verticesPerMesh)
        {
            Indices = indices;
            VerticesPerMesh = verticesPerMesh;
        }

        public uint IndicesPerMesh => (uint)Indices.Length;
    }

    internal readonly ref struct Mesh<TVertex>
        where  TVertex : unmanaged
    {
        public readonly GpuListSlice<TVertex> Vertices;
        public readonly GpuListSlice<ushort> Indices;
        public readonly uint IndexBase;

        public Mesh(
            GpuListSlice<TVertex> vertices,
            GpuListSlice<ushort> indices,
            uint indexBase)
        {
            Vertices = vertices;
            Indices = indices;
            IndexBase = indexBase;
        }
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
            static void unexpectedLength()
            {
                throw new ArgumentException("Unexpected number of vertices.");
            }

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
                dstIndices.Data[i] = (ushort)(_meshDesc.Indices[i]
                    + oldCount * vertices.Length);
            }
            return new Mesh<TVertex>(
                dstVertices,
                dstIndices,
                oldCount * _meshDesc.IndicesPerMesh
            );
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
}
