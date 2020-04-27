using System;
using Veldrid;

#nullable enable

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

        public MeshList(
            GraphicsDevice graphicsDevice,
            in MeshDescription meshDescription,
            uint initialCapacity)
        {
            _meshDesc = meshDescription;
            Vertices = new GpuList<TVertex>(
                graphicsDevice,
                BufferUsage.VertexBuffer,
                initialCapacity * meshDescription.VerticesPerMesh
            );
            Indices = new GpuList<ushort>(
                graphicsDevice,
                BufferUsage.IndexBuffer,
                initialCapacity * meshDescription.IndicesPerMesh
            );
        }

        public GpuList<TVertex> Vertices { get; }
        public GpuList<ushort> Indices { get; }

        public uint Count => Vertices.Count / _meshDesc.VerticesPerMesh;

        public void Begin()
        {
            Vertices.Begin();
            Indices.Begin();
        }

        public Mesh<TVertex> Append(ReadOnlySpan<TVertex> vertices)
        {
            static void unexpectedLength()
                => throw new ArgumentException("Unexpected number of vertices.");

            if (vertices.Length != _meshDesc.VerticesPerMesh)
            {
                unexpectedLength();
            }

            uint oldCount = Count;
            GpuListSlice<TVertex> dstVertices = Vertices.Append((uint)vertices.Length);
            vertices.CopyTo(dstVertices.Data);
            GpuListSlice<ushort> dstIndices = Indices.Append(_meshDesc.IndicesPerMesh);
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
            Vertices.End(cl);
            Indices.End(cl);
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Indices.Dispose();
        }
    }
}
