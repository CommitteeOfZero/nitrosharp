using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Graphics.Core;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct Draw
    {
        public Pipeline Pipeline;
        public ResourceBindings ResourceBindings;
        public BufferBindings BufferBindings;
        public DrawParams Params;
        public RectangleU? ScissorRect;

        public bool IsValid => Pipeline is not null;
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct DrawParams
    {
        private readonly DrawMethod _method;
        public readonly (uint start, uint count) Vertices;
        public readonly (uint start, uint count) Indices;
        public readonly (uint start, uint count) Instances;

        public bool IsIndexed => _method switch
        {
            DrawMethod.DrawIndexed => true,
            DrawMethod.DrawIndexedInstanced => true,
            _ => false
        };

        private bool IsInstanced => _method switch
        {
            DrawMethod.DrawInstanced => true,
            DrawMethod.DrawIndexedInstanced => true,
            _ => false
        };

        private DrawParams(
            uint vertexBase,
            uint vertexCount,
            uint indexBase,
            uint indexCount,
            uint instanceBase,
            uint instanceCount)
        {
            Vertices = (vertexBase, vertexCount);
            Indices = (indexBase, indexCount);
            Instances = (instanceBase, instanceCount);
            _method = (Indices, Instances) switch
            {
                ((0, 0), (0, 1)) => DrawMethod.Draw,
                ((0, 0), _) => DrawMethod.DrawInstanced,
                (_, (0, 1)) => DrawMethod.DrawIndexed,
                _ => DrawMethod.DrawIndexedInstanced
            };
        }

        public static DrawParams Regular(
            uint vertexBase,
            uint vertexCount,
            uint instanceBase = 0,
            uint instanceCount = 1)
            => new(vertexBase, vertexCount, 0, 0, instanceBase, instanceCount);

        public static DrawParams Indexed(
            uint vertexBase,
            uint indexBase,
            uint indexCount,
            uint instanceBase = 0,
            uint instanceCount = 1)
            => new(vertexBase, 0, indexBase, indexCount, instanceBase, instanceCount);

        private static bool CanMerge(in DrawParams a, in DrawParams b)
        {
            if (a._method != b._method) { return false; }
            return a._method switch
            {
                DrawMethod.Draw => areConsecutive(a.Vertices, b.Vertices),
                DrawMethod.DrawInstanced => areConsecutive(a.Vertices, b.Vertices)
                                         && areConsecutive(a.Instances, b.Instances),
                DrawMethod.DrawIndexed => a.Vertices.start == b.Vertices.start
                                       && areConsecutive(a.Indices, b.Indices),
                _ => a.Vertices.start == b.Vertices.start
                                      && areConsecutive(a.Indices, b.Indices)
                                      && areConsecutive(a.Instances, b.Instances)
            };

            static bool areConsecutive(
                (uint start, uint count) a,
                (uint start, uint count) b)
                => b.start == a.start + a.count;
        }

        public static bool TryMerge(ref DrawParams cur, in DrawParams next)
        {
            if (CanMerge(cur, next))
            {
                cur = Merge(cur, next);
                return true;
            }
            return false;
        }

        private static DrawParams Merge(in DrawParams a, in DrawParams b)
        {
            Debug.Assert(CanMerge(a, b));
            return new DrawParams(
                a.Vertices.start,
                a.Vertices.count + b.Vertices.count,
                a.Indices.start,
                a.Indices.count + b.Indices.count,
                a.Instances.start,
                a.IsInstanced ? a.Instances.count + b.Instances.count : 1
            );
        }
    }

    internal enum DrawMethod
    {
        Draw,
        DrawIndexed,
        DrawInstanced,
        DrawIndexedInstanced
    }

    internal readonly record struct BufferBindings
    {
        public readonly DeviceBuffer? Vertices;
        public readonly DeviceBuffer? InstanceData;
        public readonly DeviceBuffer? Indices;

        public BufferBindings(DeviceBuffer vertices) : this()
            => Vertices = vertices;

        public BufferBindings(DeviceBuffer vertices, DeviceBuffer indices) : this()
            => (Vertices, Indices) = (vertices, indices);
    }

    internal readonly struct ResourceBindings : IEquatable<ResourceBindings>
    {
        public readonly ResourceSetKey? ResourceSet0;
        public readonly ResourceSetKey? ResourceSet1;
        public readonly ResourceSetKey? ResourceSet2;
        public readonly ResourceSetKey? ResourceSet3;

        public ResourceBindings(
            ResourceSetKey rs0,
            ResourceSetKey? rs1 = null,
            ResourceSetKey? rs2 = null,
            ResourceSetKey? rs3 = null)
        {
            ResourceSet0 = rs0;
            ResourceSet1 = rs1;
            ResourceSet2 = rs2;
            ResourceSet3 = rs3;
        }

        public bool Equals(ResourceBindings other)
        {
            return Nullable.Equals(ResourceSet0, other.ResourceSet0)
                && Nullable.Equals(ResourceSet1, other.ResourceSet1)
                && Nullable.Equals(ResourceSet2, other.ResourceSet2)
                && Nullable.Equals(ResourceSet3, other.ResourceSet3);
        }
    }

    internal sealed class DrawBatch : IDisposable
    {
        private readonly RenderContext _ctx;
        private CommandList? _commandList;

        private Draw _lastDraw;
        private Vector2 _lastAlphaMaskPosition = new(float.NaN);

        public DrawBatch(RenderContext context)
        {
            _ctx = context;
            Target = null!;
        }

        public RenderTarget Target { get; private set; }

        public void Begin(CommandList commandList, RenderTarget target, RgbaFloat? clearColor)
        {
            _commandList = commandList;
            commandList.SetFramebuffer(target.Framebuffer);
            Target = target;
            if (clearColor is { } clear)
            {
                commandList.ClearColorTarget(0, clear);
            }
        }

        public void UpdateBuffer<T>(GpuBuffer<T> buffer, in T data)
            where T : unmanaged
        {
            Debug.Assert(_commandList is not null);
            Flush();
            buffer.Update(_commandList, data);
        }

        public void PushQuad(
            QuadGeometry quad,
            Texture texture,
            Texture alphaMask,
            Vector2 alphaMaskPosition,
            BlendMode blendMode,
            FilterMode filterMode)
        {
            Debug.Assert(_commandList is not null);
            ViewProjection vp = Target.OrthoProjection;

            QuadShaderResources resources = _ctx.ShaderResources.Quad;
            if (alphaMaskPosition != _lastAlphaMaskPosition)
            {
                Vector4 newValue = new Vector4(alphaMaskPosition, 0, 0);
                UpdateBuffer(resources.AlphaMaskPositionBuffer, newValue);
                _lastAlphaMaskPosition = alphaMaskPosition;
            }

            PushQuad(quad, _ctx.ShaderResources.Quad.GetPipeline(blendMode),
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        _ctx.ShaderResources.Quad.ResourceLayout,
                        texture,
                        alphaMask,
                        _ctx.GetSampler(filterMode),
                        resources.AlphaMaskPositionBuffer.VdBuffer
                    )
                )
            );
        }

        public void PushQuad(QuadGeometry quad, Pipeline pipeline, in ResourceBindings resources)
        {
            Debug.Assert(_commandList is not null);
            Span<QuadVertex> vertices = MemoryMarshal.CreateSpan(ref quad.TopLeft, 4);
            Mesh<QuadVertex> mesh = _ctx.Quads.Append(vertices);
            PushDraw(new Draw
            {
                Pipeline = pipeline,
                ResourceBindings = resources,
                BufferBindings = new BufferBindings(mesh.Vertices.Buffer, mesh.Indices.Buffer),
                Params = DrawParams.Indexed(0, mesh.IndexBase, 6)
            });
        }

        public void PushQuadUV3(QuadGeometryUV3 quad, Pipeline pipeline, in ResourceBindings resources)
        {
            Debug.Assert(_commandList is not null);
            Span<QuadVertexUV3> vertices = MemoryMarshal.CreateSpan(ref quad.TopLeft, 4);
            Mesh<QuadVertexUV3> mesh = _ctx.QuadsUV3.Append(vertices);
            PushDraw(new Draw
            {
                Pipeline = pipeline,
                ResourceBindings = resources,
                BufferBindings = new BufferBindings(mesh.Vertices.Buffer, mesh.Indices.Buffer),
                Params = DrawParams.Indexed(0, mesh.IndexBase, 6)
            });
        }

        public void PushDraw(in Draw draw)
        {
            Debug.Assert(_commandList is not null);
            if (ReferenceEquals(draw.Pipeline, _lastDraw.Pipeline)
                && draw.ResourceBindings.Equals(_lastDraw.ResourceBindings)
                && draw.BufferBindings.Equals(_lastDraw.BufferBindings)
                && Nullable.Equals(draw.ScissorRect, _lastDraw.ScissorRect)
                && DrawParams.TryMerge(ref _lastDraw.Params, draw.Params))
            {
                return;
            }

            if (_lastDraw.IsValid)
            {
                Flush();
            }

            _lastDraw = draw;
        }

        private void Flush()
        {
            if (_commandList is null || !_lastDraw.IsValid)
            {
                return;
            }

            CommandList cl = _commandList;
            cl.SetFramebuffer(Target.Framebuffer);
            cl.SetPipeline(_lastDraw.Pipeline);
            if (_lastDraw.ScissorRect is { } sr)
            {
                cl.SetScissorRect(0, sr.Left, sr.Top, sr.Width, sr.Height);
            }
            else
            {
                cl.SetFullScissorRect(0);
            }
            ref BufferBindings buffers = ref _lastDraw.BufferBindings;
            if (buffers.Vertices is { } vertices)
            {
                cl.SetVertexBuffer(0, vertices);
            }
            if (buffers.InstanceData is { } instanceData)
            {
                cl.SetVertexBuffer(1, instanceData);

            }
            if (buffers.Indices is { } indices)
            {
                cl.SetIndexBuffer(indices, IndexFormat.UInt16);
            }

            ResourceSetCache rsCache = _ctx.ResourceSetCache;
            ref ResourceBindings resources = ref _lastDraw.ResourceBindings;
            setResources(cl, 0, resources.ResourceSet0);
            setResources(cl, 1, resources.ResourceSet1);
            setResources(cl, 2, resources.ResourceSet2);
            setResources(cl, 3, resources.ResourceSet3);

            void setResources(CommandList cl, uint slot, ResourceSetKey? rsKeyOpt)
            {
                if (rsKeyOpt is { } rsKey)
                {
                    ResourceSet rs = rsCache.GetResourceSet(rsKey);
                    cl.SetGraphicsResourceSet(slot, rs);
                }
            }

            ref DrawParams p = ref _lastDraw.Params;
            if (p.IsIndexed)
            {
                cl.DrawIndexed(
                    p.Indices.count,
                    p.Instances.count,
                    p.Indices.start,
                    (int)p.Vertices.start,
                    p.Instances.start
                );
            }
            else
            {
                cl.Draw(
                    p.Vertices.count,
                    p.Instances.count,
                    p.Vertices.start,
                    p.Instances.start
                );
            }
            _lastDraw = default;
        }

        public void End() => Flush();
        public void Dispose() => Flush();
    }
}
