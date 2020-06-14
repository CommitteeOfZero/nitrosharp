using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.Graphics.Old;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    [StructLayout(LayoutKind.Auto)]
    internal struct RenderBucketSubmission
    {
        public Action<RenderContext>? BeforeRenderCallback;
        public VertexBuffer? VertexBuffer0;
        public VertexBuffer? VertexBuffer1;
        public DeviceBuffer? IndexBuffer;
        public Pipeline Pipeline;
        public ResourceSet SharedResourceSet;
        public ResourceSet ObjectResourceSet0;
        public ResourceSet? ObjectResourceSet1;
        public ushort VertexBase;
        public ushort VertexCount;
        public ushort IndexBase;
        public ushort IndexCount;
        public ushort InstanceBase;
        public ushort InstanceCount;
        public UniformUpdate UniformUpdate;
    }

    internal readonly struct UniformUpdate
    {
        public readonly DeviceBuffer Buffer;
        public readonly ushort DataStart;
        public readonly ushort DataLength;

        public UniformUpdate(DeviceBuffer buffer, ushort dataStart, ushort dataLength)
            => (Buffer, DataStart, DataLength) = (buffer, dataStart, dataLength);
    }

    internal sealed class RenderBucket2
    {
        internal readonly ref struct MultiSubmission
        {
            public readonly Span<RenderBucketSubmission> Submissions;

            public MultiSubmission(Span<RenderBucketSubmission> submissions)
            {
                Submissions = submissions;
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct DrawCall
        {
            public Action<RenderContext>? BeforeRenderCallback;
            public ResourceSet ObjectResourceSet0;
            public ResourceSet? ObjectResourceSet1;
            public ushort VertexBase;
            public ushort VertexCount;
            public ushort IndexBase;
            public ushort IndexCount;
            public ushort InstanceBase;
            public ushort InstanceCount;
            public byte SharedResourceSetId;
            public byte PipelineId;
            public byte VertexBuffer0;
            public byte VertexBuffer1;
            public byte IndexBuffer;

            public byte UniformBuffer;
            public ushort UniformDataStart;
            public ushort UniformDataLength;

            public override string ToString()
            {
                return $"Base: {InstanceBase}, Count: {InstanceCount}";
            }
        }

        private ArrayBuilder<DrawCall> _drawCalls;
        private ArrayBuilder<RenderBucketSubmission> _submissions;

        private readonly List<VertexBuffer> _vertexBuffers;
        private (byte index, VertexBuffer? buffer) _lastVertexBuffer0;
        private (byte index, VertexBuffer? buffer) _lastVertexBuffer1;

        private readonly List<DeviceBuffer> _indexBuffers;
        private (byte index, DeviceBuffer? buffer) _lastIndexBuffer;

        private readonly List<Pipeline> _pipelines;
        private (byte index, Pipeline pipeline) _lastPipeline;

        private readonly List<ResourceSet> _sharedResourceSets;
        private (byte index, ResourceSet set) _lastSharedResourceSet;

        private readonly List<DeviceBuffer> _uniformBuffers;
        private ArrayBuilder<byte> _uniformData;

        public RenderBucket2(uint initialCapacity)
        {
            _drawCalls = new ArrayBuilder<DrawCall>(initialCapacity);
            _submissions = new ArrayBuilder<RenderBucketSubmission>(initialCapacity);
            _vertexBuffers = new List<VertexBuffer>();
            _indexBuffers = new List<DeviceBuffer>();
            _pipelines = new List<Pipeline>();
            _sharedResourceSets = new List<ResourceSet>();
            _uniformBuffers = new List<DeviceBuffer>();
            _uniformData = new ArrayBuilder<byte>(initialCapacity: 1024);
        }

        public void Begin()
        {
            _drawCalls.Reset();
            _submissions.Reset();
            _vertexBuffers.Clear();
            _lastVertexBuffer0 = default;
            _lastVertexBuffer1 = default;
            _indexBuffers.Clear();
            _lastIndexBuffer = default;
            _pipelines.Clear();
            _sharedResourceSets.Clear();
            _lastSharedResourceSet = default;
            _lastPipeline = default;
            _uniformBuffers.Clear();
            _uniformData.Clear();
        }

        public UniformUpdate StoreUniformUpdate<T>(DeviceBuffer targetBuffer, ref T data)
           where T : unmanaged
        {
            uint start = _uniformData.Count;
            uint sizeInBytes = (uint)Unsafe.SizeOf<T>();
            uint actualSize = MathUtil.RoundUp(sizeInBytes, multiple: 16u);
            Span<byte> dstBytes = _uniformData.Append(actualSize);
            MemoryMarshal.Write(dstBytes, ref data);
            return new UniformUpdate(targetBuffer, (ushort)start, (ushort)actualSize);
        }

        public void Submit(ref RenderBucketSubmission submission)
            => Submit<byte>(ref submission);

        public void Submit<TVertex>(ref RenderBucketSubmission submission)
            where TVertex : unmanaged
        {
            ref ArrayBuilder<DrawCall> drawCalls = ref _drawCalls;
            ref DrawCall drawCall = ref drawCalls.Add();
            if (drawCalls.Count > 0)
            {
                //ref RenderItem lastItem = ref renderItems[^1];
                //if ((submission.InstanceBase == (lastItem.InstanceBase + lastItem.InstanceCount))
                //    && submission.Pipeline == _lastPipeline.pipeline
                //    && submission.SharedResourceSet == _lastSharedResourceSet.set
                //    && submission.ObjectResourceSet0 == lastItem.ObjectResourceSet0
                //    && submission.ObjectResourceSet1 == lastItem.ObjectResourceSet1
                //    && submission.VertexBuffer0 == _lastVertexBuffer0.buffer
                //    && submission.VertexBuffer1 == _lastVertexBuffer1.buffer
                //    && submission.IndexBuffer == _lastIndexBuffer.buffer)
                //{
                //    lastItem.InstanceCount++;
                //    return;
                //}
            }

            drawCall.BeforeRenderCallback = submission.BeforeRenderCallback;
            drawCall.VertexBuffer0 = GetResourceIdMaybe(submission.VertexBuffer0, _vertexBuffers, ref _lastVertexBuffer0);
            drawCall.VertexBuffer1 = byte.MaxValue;
            drawCall.IndexBuffer = GetResourceIdMaybe(submission.IndexBuffer, _indexBuffers, ref _lastIndexBuffer);
            drawCall.VertexBase = submission.VertexBase;
            drawCall.VertexCount = submission.VertexCount;
            drawCall.IndexBase = submission.IndexBase;
            drawCall.IndexCount = submission.IndexCount;
            drawCall.PipelineId = GetPipelineId(submission.Pipeline);
            drawCall.SharedResourceSetId = GetResourceId(submission.SharedResourceSet, _sharedResourceSets, ref _lastSharedResourceSet);
            drawCall.ObjectResourceSet0 = submission.ObjectResourceSet0;
            drawCall.ObjectResourceSet1 = submission.ObjectResourceSet1;
            drawCall.InstanceBase = submission.InstanceBase;
            drawCall.InstanceCount = submission.InstanceCount;

            drawCall.UniformBuffer = byte.MaxValue;
            UniformUpdate update = submission.UniformUpdate;
            if (update.DataLength > 0)
            {
                (byte index, DeviceBuffer buffer) discard = default;
                byte bufferId = GetResourceId(update.Buffer, _uniformBuffers, ref discard);
                drawCall.UniformBuffer = bufferId;
                drawCall.UniformDataStart = update.DataStart;
                drawCall.UniformDataLength = update.DataLength;
            }
        }

        public void Submit<TVertex0, TVertex1>(ref RenderBucketSubmission submission)
            where TVertex0 : unmanaged
            where TVertex1 : unmanaged
        {
            Submit<TVertex0>(ref submission);
        }

        public Span<RenderBucketSubmission> PrepareMultiSubmission(uint submissionCount)
        {
            return _submissions.Append(submissionCount);
        }

        public void Submit(MultiSubmission multiSubmission)
        {
            int count = multiSubmission.Submissions.Length;
            Span<DrawCall> drawCalls = _drawCalls.Append((uint)count);
            (byte index, VertexBuffer? buffer) vb0 = _lastVertexBuffer0;
            (byte index, VertexBuffer? buffer) vb1 = _lastVertexBuffer1;
            (byte index, DeviceBuffer? buffer) ib = _lastIndexBuffer;
            List<VertexBuffer> vertexBuffers = _vertexBuffers;
            List<DeviceBuffer> indexBuffers = _indexBuffers;
            List<ResourceSet> sharedResourceSets = _sharedResourceSets;
            (byte index, ResourceSet set) lastSharedSet = _lastSharedResourceSet;
            int cur = 0;
            Pipeline? lastPipeline = null;
            ResourceSet? lastResourceSet0 = null, lastResourceSet1 = null;
            for (int i = 0; i < count; i++)
            {
                ref RenderBucketSubmission submission = ref multiSubmission.Submissions[i];
                ref DrawCall drawCall = ref drawCalls[cur];
                if (cur > 0)
                {
                    //ref RenderItem lastRI = ref renderItems[curRenderItem - 1];
                    //if (submission.IndexBase == (lastRI.IndexBase + lastRI.IndexCount)
                    //    && ReferenceEquals(submission.Pipeline, lastPipeline)
                    //    && ReferenceEquals(submission.SharedResourceSet, lastSharedSet.set)
                    //    && ReferenceEquals(submission.ObjectResourceSet0, lastResourceSet0)
                    //    && ReferenceEquals(submission.ObjectResourceSet1, lastResourceSet1)
                    //    && ReferenceEquals(submission.VertexBuffer0, vb0.buffer)
                    //    && ReferenceEquals(submission.VertexBuffer1, vb1.buffer)
                    //    && ReferenceEquals(submission.IndexBuffer, ib.buffer))
                    //{
                    //    if (multiSubmission.Keys[i].Priority == multiSubmission.Keys[i - 1].Priority)
                    //    {
                    //        lastRI.VertexCount += submission.VertexCount;
                    //        lastRI.IndexCount += submission.IndexCount;
                    //        continue;
                    //    }
                    //}
                }
                drawCall.BeforeRenderCallback = submission.BeforeRenderCallback;
                drawCall.VertexBuffer0 = GetResourceIdMaybe(submission.VertexBuffer0, vertexBuffers, ref vb0);
                drawCall.VertexBuffer1 = GetResourceIdMaybe(submission.VertexBuffer1, vertexBuffers, ref vb1);
                drawCall.IndexBuffer = GetResourceIdMaybe(submission.IndexBuffer, indexBuffers, ref ib);
                drawCall.VertexBase = submission.VertexBase;
                drawCall.VertexCount = submission.VertexCount;
                drawCall.IndexBase = submission.IndexBase;
                drawCall.IndexCount = submission.IndexCount;
                drawCall.PipelineId = GetPipelineId(submission.Pipeline);
                drawCall.SharedResourceSetId = GetResourceId(submission.SharedResourceSet, sharedResourceSets, ref lastSharedSet);
                drawCall.ObjectResourceSet0 = submission.ObjectResourceSet0;
                drawCall.ObjectResourceSet1 = submission.ObjectResourceSet1;
                drawCall.InstanceBase = submission.InstanceBase;
                drawCall.InstanceCount = submission.InstanceCount;
                lastPipeline = submission.Pipeline;
                lastResourceSet0 = submission.ObjectResourceSet0;
                lastResourceSet1 = submission.ObjectResourceSet1;

                drawCall.UniformBuffer = byte.MaxValue;
                UniformUpdate update = submission.UniformUpdate;
                if (update.DataLength > 0)
                {
                    (byte index, DeviceBuffer buffer) discard = default;
                    byte bufferId = GetResourceId(update.Buffer, _uniformBuffers, ref discard);
                    drawCall.UniformBuffer = bufferId;
                    drawCall.UniformDataStart = update.DataStart;
                    drawCall.UniformDataLength = update.DataLength;
                }
                cur++;
            }
            int actualCount = cur;
            uint diff = (uint)(multiSubmission.Submissions.Length - actualCount);
            _drawCalls.Truncate(_drawCalls.Count - diff);
            _lastVertexBuffer0 = vb0;
            _lastVertexBuffer1 = vb1;
            _lastIndexBuffer = ib;
            _lastSharedResourceSet = lastSharedSet;
        }

        public void End(in RenderContext renderContext)
        {
            CommandList commandList = renderContext.CommandList;

            byte lastPipelineId = byte.MaxValue;
            byte lastSharedResourceSetId = byte.MaxValue;
            byte lastVertexBuffer0 = byte.MaxValue;
            byte lastVertexBuffer1 = byte.MaxValue;
            byte lastIndexBuffer = byte.MaxValue;
            ResourceSet? lastObjectResourceSet0 = null;
            ResourceSet? lastObjectResourceSet1 = null;
            for (uint i = 0; i < _drawCalls.Count; i++)
            {
                ref DrawCall item = ref _drawCalls[i];
                item.BeforeRenderCallback?.Invoke(renderContext);
                if (item.PipelineId != lastPipelineId)
                {
                    Pipeline pipeline = _pipelines[item.PipelineId];
                    commandList.SetPipeline(pipeline);
                    lastPipelineId = item.PipelineId;
                }
                if (item.SharedResourceSetId != lastSharedResourceSetId)
                {
                    ResourceSet set = _sharedResourceSets[item.SharedResourceSetId];
                    commandList.SetGraphicsResourceSet(0, set);
                    lastSharedResourceSetId = item.SharedResourceSetId;
                }
                ResourceSet objectResourceSet0 = item.ObjectResourceSet0;
                if (objectResourceSet0 != lastObjectResourceSet0)
                {
                    commandList.SetGraphicsResourceSet(1, objectResourceSet0);
                    lastObjectResourceSet0 = objectResourceSet0;
                }
                ResourceSet? objectResourceSet1 = item.ObjectResourceSet1;
                if (objectResourceSet1 != lastObjectResourceSet1)
                {
                    if (objectResourceSet1 != null)
                    {
                        commandList.SetGraphicsResourceSet(2, objectResourceSet1);
                        lastObjectResourceSet1 = objectResourceSet1;
                    }
                }
                if (item.VertexBuffer0 != lastVertexBuffer0)
                {
                    byte newVB0 = item.VertexBuffer0;
                    if (newVB0 != byte.MaxValue)
                    {
                        DeviceBuffer buffer = _vertexBuffers[newVB0].DeviceBuffer;
                        commandList.SetVertexBuffer(0, buffer);
                    }
                    lastVertexBuffer0 = item.VertexBuffer0;
                }
                if (item.VertexBuffer1 != lastVertexBuffer1)
                {
                    byte newVB1 = item.VertexBuffer1;
                    if (newVB1 != byte.MaxValue)
                    {
                        DeviceBuffer buffer = _vertexBuffers[newVB1].DeviceBuffer;
                        commandList.SetVertexBuffer(1, buffer);
                    }
                    lastVertexBuffer1 = item.VertexBuffer1;
                }
                if (item.IndexBuffer != lastIndexBuffer)
                {
                    byte newIB = item.IndexBuffer;
                    if (newIB != byte.MaxValue)
                    {
                        DeviceBuffer buffer = _indexBuffers[item.IndexBuffer];
                        commandList.SetIndexBuffer(buffer, IndexFormat.UInt16);
                    }
                    lastIndexBuffer = item.IndexBuffer;
                }
                if (item.UniformBuffer != byte.MaxValue)
                {
                    DeviceBuffer buffer = _uniformBuffers[item.UniformBuffer];
                    Span<byte> data = _uniformData.AsSpan(
                        item.UniformDataStart,
                        item.UniformDataLength
                    );
                    commandList.UpdateBuffer(
                        buffer, 0, ref data[0],
                        item.UniformDataLength
                    );
                }

                bool indexed = lastIndexBuffer != byte.MaxValue;
                if (indexed)
                {
                    commandList.DrawIndexed(
                        item.IndexCount,
                        item.InstanceCount,
                        item.IndexBase,
                        item.VertexBase,
                        item.InstanceBase
                    );
                }
                else
                {
                    commandList.Draw(
                        item.VertexCount,
                        item.InstanceCount,
                        item.VertexBase,
                        item.InstanceBase
                    );
                }
            }
        }

        private byte GetPipelineId(Pipeline pipeline)
            => GetResourceId(pipeline, _pipelines, ref _lastPipeline);

        private byte GetResourceIdMaybe<T>(T? resource, List<T> resourceList, ref (byte index, T? resource) lastUsed)
            where T : class
        {
            if (resource == null)
            {
                lastUsed = (byte.MaxValue, null);
                return byte.MaxValue;
            }

            return GetResourceId(resource, resourceList, ref lastUsed!);
        }

        private byte GetResourceId<T>(T resource, List<T> resourceList, ref (byte index, T resource) lastUsed)
            where T : class
        {
            static int indexof(List<T> list, T resource)
            {
                int i = 0;
                int len = list.Count;
                while (i < len && !ReferenceEquals(list[i], resource))
                {
                    i++;
                }
                return i < len ? i : -1;
            }

            if (resource == lastUsed.resource)
            {
                return lastUsed.index;
            }

            int index;
            if ((index = indexof(resourceList, resource)) == -1)
            {
                index = resourceList.Count;
                resourceList.Add(resource);
                lastUsed = ((byte)index, resource);
            }

            return (byte)index;
        }
    }
}
