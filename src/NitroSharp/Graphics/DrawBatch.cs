using System;
using System.Runtime.InteropServices;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics.Core
{
    internal struct DrawBatch
    {
        private readonly RenderContext _ctx;
        private readonly CommandList _commandList;

        private Draw _lastDraw;

        public DrawBatch(
            RenderContext context,
            CommandList commandList,
            RenderTarget renderTarget)
        {
            _ctx = context;
            _commandList = commandList;
            _lastDraw = default;
            commandList.SetFramebuffer(renderTarget.Framebuffer);
        }

        public void PushQuad(
            CommandList commandList,
            QuadGeometry quad,
            Texture texture,
            Texture alphaMask,
            BlendMode blendMode,
            FilterMode filterMode)
        {
            ViewProjection vp = _ctx.ViewProjection;
            PushQuad(quad, _ctx.ShaderResources.Quad.GetPipeline(blendMode),
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        _ctx.ShaderResources.Quad.ResourceLayout,
                        texture,
                        alphaMask,
                        _ctx.GetSampler(filterMode)
                    )
                )
            );
        }

        public void PushQuad(QuadGeometry quad, Pipeline pipeline, in ResourceBindings resources)
        {
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

        public void PushDraw(in Draw draw)
        {
            if (ReferenceEquals(draw.Pipeline, _lastDraw.Pipeline)
                && draw.ResourceBindings.Equals(_lastDraw.ResourceBindings)
                && draw.BufferBindings.Equals(_lastDraw.BufferBindings)
                && DrawParams.TryMerge(ref _lastDraw.Params, draw.Params))
            {
                return;
            }

            if (_lastDraw.Pipeline is object)
            {
                Flush();
            }

            _lastDraw = draw;
        }

        private void Flush()
        {
            if (_lastDraw.Pipeline is null) { return; }
            CommandList cl = _commandList;
            cl.SetPipeline(_lastDraw.Pipeline);
            ref BufferBindings buffers = ref _lastDraw.BufferBindings;
            if (buffers.Vertices is DeviceBuffer vertices)
            {
                cl.SetVertexBuffer(0, vertices);
            }
            if (buffers.InstanceData is DeviceBuffer instanceData)
            {
                cl.SetVertexBuffer(1, instanceData);

            }
            if (buffers.Indices is DeviceBuffer indices)
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
                if (rsKeyOpt is ResourceSetKey rsKey)
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
        }
    }
}
