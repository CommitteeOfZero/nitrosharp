using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal struct UniformUpdate
    {
        public DeviceBuffer Buffer;
        public Memory<byte> Data;

        public UniformUpdate(DeviceBuffer buffer, Memory<byte> data)
            => (Buffer, Data) = (buffer, data);
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct RenderBucketSubmission
    {
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

        public UniformUpdate? UniformUpdate;
    }

    internal sealed class RenderBucket<TKey> where TKey : IComparable<RenderItemKey>
    {
        internal readonly ref struct MultiSubmission
        {
            public readonly Span<RenderItemKey> Keys;
            public readonly Span<RenderBucketSubmission> Submissions;

            public MultiSubmission(Span<RenderItemKey> keys, Span<RenderBucketSubmission> submissions)
            {
                Keys = keys;
                Submissions = submissions;
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct RenderItem
        {
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

        private ArrayBuilder<RenderItem> _renderItems;
        private ArrayBuilder<RenderItemKey> _keys;
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

        private ArrayBuilder<Vector4> _uniformData;

        public RenderBucket(uint initialCapacity)
        {
            _renderItems = new ArrayBuilder<RenderItem>(initialCapacity);
            _keys = new ArrayBuilder<RenderItemKey>(initialCapacity);
            _submissions = new ArrayBuilder<RenderBucketSubmission>(initialCapacity);
            _vertexBuffers = new List<VertexBuffer>();
            _indexBuffers = new List<DeviceBuffer>();
            _pipelines = new List<Pipeline>();
            _sharedResourceSets = new List<ResourceSet>();
            _uniformBuffers = new List<DeviceBuffer>();
            _uniformData = new ArrayBuilder<Vector4>(initialCapacity: 128);
        }

        public void Begin()
        {
            _renderItems.Reset();
            _keys.Reset();
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

        public void Submit<TVertex>(ref RenderBucketSubmission submission, RenderItemKey key)
            where TVertex : unmanaged
        {
            ref ArrayBuilder<RenderItem> renderItems = ref _renderItems;
            ref RenderItem renderItem = ref renderItems.Add();
            if (renderItems.Count > 0)
            {
                ref RenderItem lastItem = ref renderItems[^1];
                if ((submission.InstanceBase == (lastItem.InstanceBase + lastItem.InstanceCount))
                    && submission.Pipeline == _lastPipeline.pipeline
                    && submission.SharedResourceSet == _lastSharedResourceSet.set
                    && submission.ObjectResourceSet0 == lastItem.ObjectResourceSet0
                    && submission.ObjectResourceSet1 == lastItem.ObjectResourceSet1
                    && submission.VertexBuffer0 == _lastVertexBuffer0.buffer
                    && submission.VertexBuffer1 == _lastVertexBuffer1.buffer
                    && submission.IndexBuffer == _lastIndexBuffer.buffer)
                {
                    lastItem.InstanceCount++;
                    return;
                }
            }

            (byte index, DeviceBuffer buffer) uniform = default;

            renderItem.VertexBuffer0 = GetResourceIdMaybe(submission.VertexBuffer0, _vertexBuffers, ref _lastVertexBuffer0);
            renderItem.VertexBuffer1 = byte.MaxValue;
            renderItem.IndexBuffer = GetResourceIdMaybe(submission.IndexBuffer, _indexBuffers, ref _lastIndexBuffer);
            renderItem.VertexBase = submission.VertexBase;
            renderItem.VertexCount = submission.VertexCount;
            renderItem.IndexBase = submission.IndexBase;
            renderItem.IndexCount = submission.IndexCount;
            renderItem.PipelineId = GetPipelineId(submission.Pipeline);
            renderItem.SharedResourceSetId = GetResourceId(submission.SharedResourceSet, _sharedResourceSets, ref _lastSharedResourceSet);
            renderItem.ObjectResourceSet0 = submission.ObjectResourceSet0;
            renderItem.ObjectResourceSet1 = submission.ObjectResourceSet1;
            renderItem.InstanceBase = submission.InstanceBase;
            renderItem.InstanceCount = submission.InstanceCount;

            renderItem.UniformBuffer = byte.MaxValue;
            if (submission.UniformUpdate != null)
            {
                UniformUpdate update = submission.UniformUpdate.Value;
                (byte bufferId, ushort start, ushort len) = writeUniformData(update);
                renderItem.UniformBuffer = bufferId;
                renderItem.UniformDataStart = start;
                renderItem.UniformDataLength = len;
            }

            _keys.Add(key);

            (byte bufferId, ushort start, ushort length) writeUniformData(in UniformUpdate update)
            {
                byte bufferId = GetResourceId(update.Buffer, _uniformBuffers, ref uniform);
                uint start = _uniformData.Count;
                uint length = MathUtil.RoundUp((uint)update.Data.Length, multiple: 16u);
                Span<Vector4> dst = _uniformData.Append(length);
                update.Data.Span.CopyTo(MemoryMarshal.Cast<Vector4, byte>(dst));
                return (bufferId, (ushort)start, (ushort)length);
            }
        }

        public void Submit<TVertex0, TVertex1>(ref RenderBucketSubmission submission, RenderItemKey key)
            where TVertex0 : unmanaged
            where TVertex1 : unmanaged
        {
            ref ArrayBuilder<RenderItem> renderItems = ref _renderItems;
            if (renderItems.Count > 0)
            {
                ref RenderItem lastItem = ref renderItems[^1];
                if ((submission.InstanceBase == (lastItem.InstanceBase + lastItem.InstanceCount))
                    && submission.Pipeline == _lastPipeline.pipeline
                    && submission.SharedResourceSet == _lastSharedResourceSet.set
                    && submission.ObjectResourceSet0 == lastItem.ObjectResourceSet0
                    && submission.ObjectResourceSet1 == lastItem.ObjectResourceSet1
                    && submission.VertexBuffer0 == _lastVertexBuffer0.buffer
                    && submission.VertexBuffer1 == _lastVertexBuffer1.buffer
                    && submission.IndexBuffer == _lastIndexBuffer.buffer)
                {
                    lastItem.InstanceCount++;
                    return;
                }
            }

            (byte index, DeviceBuffer buffer) uniform = default;

            ref RenderItem renderItem = ref renderItems.Add();
            renderItem.VertexBuffer0 = GetResourceIdMaybe(submission.VertexBuffer0, _vertexBuffers, ref _lastVertexBuffer0);
            renderItem.VertexBuffer1 = GetResourceIdMaybe(submission.VertexBuffer1, _vertexBuffers, ref _lastVertexBuffer1);
            renderItem.IndexBuffer = GetResourceIdMaybe(submission.IndexBuffer, _indexBuffers, ref _lastIndexBuffer);
            renderItem.VertexBase = submission.VertexBase;
            renderItem.VertexCount = submission.VertexCount;
            renderItem.IndexBase = submission.IndexBase;
            renderItem.IndexCount = submission.IndexCount;
            renderItem.PipelineId = GetPipelineId(submission.Pipeline);
            renderItem.SharedResourceSetId = GetResourceId(submission.SharedResourceSet, _sharedResourceSets, ref _lastSharedResourceSet);
            renderItem.ObjectResourceSet0 = submission.ObjectResourceSet0;
            renderItem.ObjectResourceSet1 = submission.ObjectResourceSet1;
            renderItem.InstanceBase = submission.InstanceBase;
            renderItem.InstanceCount = submission.InstanceCount;

            renderItem.UniformBuffer = byte.MaxValue;
            if (submission.UniformUpdate != null)
            {
                UniformUpdate update = submission.UniformUpdate.Value;
                (byte bufferId, ushort start, ushort len) = writeUniformData(update);
                renderItem.UniformBuffer = bufferId;
                renderItem.UniformDataStart = start;
                renderItem.UniformDataLength = len;
            }

            (byte bufferId, ushort start, ushort length) writeUniformData(in UniformUpdate update)
            {
                byte bufferId = GetResourceId(update.Buffer, _uniformBuffers, ref uniform);
                uint start = _uniformData.Count;
                uint length = MathUtil.RoundUp((uint)update.Data.Length, multiple: 16u);
                Span<Vector4> dst = _uniformData.Append(length);
                update.Data.Span.CopyTo(MemoryMarshal.Cast<Vector4, byte>(dst));
                return (bufferId, (ushort)start, (ushort)length);
            }

            _keys.Add(key);
        }

        public MultiSubmission PrepareMultiSubmission(uint renderItemCount)
        {
            return new MultiSubmission(
                _keys.Append(renderItemCount),
                _submissions.Append(renderItemCount)
            );
        }

        public void Submit(MultiSubmission multiSubmission)
        {
            int count = multiSubmission.Submissions.Length;
            Span<RenderItem> renderItems = _renderItems.Append((uint)count);
            (byte index, VertexBuffer? buffer) vb0 = _lastVertexBuffer0;
            (byte index, VertexBuffer? buffer) vb1 = _lastVertexBuffer1;
            (byte index, DeviceBuffer? buffer) ib = _lastIndexBuffer;
            (byte index, DeviceBuffer buffer) uniform = default;
            List<VertexBuffer> vertexBuffers = _vertexBuffers;
            List<DeviceBuffer> indexBuffers = _indexBuffers;
            List<ResourceSet> sharedResourceSets = _sharedResourceSets;
            (byte index, ResourceSet set) lastSharedSet = _lastSharedResourceSet;
            int curRenderItem = -1;
            Pipeline? lastPipeline = null;
            ResourceSet? lastResourceSet0 = null, lastResourceSet1 = null;
            for (int i = 0; i < count; i++)
            {
                ref RenderBucketSubmission submission = ref multiSubmission.Submissions[i];
                if (curRenderItem >= 0)
                {
                    ref RenderItem lastRI = ref renderItems[curRenderItem];
                    if (submission.IndexBase == (lastRI.IndexBase + lastRI.IndexCount)
                        && ReferenceEquals(submission.Pipeline, lastPipeline)
                        && ReferenceEquals(submission.SharedResourceSet, lastSharedSet.set)
                        && ReferenceEquals(submission.ObjectResourceSet0, lastResourceSet0)
                        && ReferenceEquals(submission.ObjectResourceSet1, lastResourceSet1)
                        && ReferenceEquals(submission.VertexBuffer0, vb0.buffer)
                        && ReferenceEquals(submission.VertexBuffer1, vb1.buffer)
                        && ReferenceEquals(submission.IndexBuffer, ib.buffer))
                    {
                        if (multiSubmission.Keys[i].Priority == multiSubmission.Keys[i - 1].Priority)
                        {
                            lastRI.VertexCount += submission.VertexCount;
                            lastRI.IndexCount += submission.IndexCount;
                            continue;
                        }
                    }
                }
                ref RenderItem renderItem = ref renderItems[++curRenderItem];
                renderItem.VertexBuffer0 = GetResourceIdMaybe(submission.VertexBuffer0, vertexBuffers, ref vb0);
                renderItem.VertexBuffer1 = GetResourceIdMaybe(submission.VertexBuffer1, vertexBuffers, ref vb1);
                renderItem.IndexBuffer = GetResourceIdMaybe(submission.IndexBuffer, indexBuffers, ref ib);
                renderItem.VertexBase = submission.VertexBase;
                renderItem.VertexCount = submission.VertexCount;
                renderItem.IndexBase = submission.IndexBase;
                renderItem.IndexCount = submission.IndexCount;
                renderItem.PipelineId = GetPipelineId(submission.Pipeline);
                renderItem.SharedResourceSetId = GetResourceId(submission.SharedResourceSet, sharedResourceSets, ref lastSharedSet);
                renderItem.ObjectResourceSet0 = submission.ObjectResourceSet0;
                renderItem.ObjectResourceSet1 = submission.ObjectResourceSet1;
                renderItem.InstanceBase = submission.InstanceBase;
                renderItem.InstanceCount = submission.InstanceCount;
                lastPipeline = submission.Pipeline;
                lastResourceSet0 = submission.ObjectResourceSet0;
                lastResourceSet1 = submission.ObjectResourceSet1;

                renderItem.UniformBuffer = byte.MaxValue;
                if (submission.UniformUpdate != null)
                {
                    UniformUpdate update = submission.UniformUpdate.Value;
                    (byte bufferId, ushort start, ushort len) = writeUniformData(update);
                    renderItem.UniformBuffer = bufferId;
                    renderItem.UniformDataStart = start;
                    renderItem.UniformDataLength = len;
                }

                multiSubmission.Keys[curRenderItem] = multiSubmission.Keys[i];
            }
            int actualCount = curRenderItem + 1;
            uint diff = (uint)(multiSubmission.Submissions.Length - actualCount);
            _renderItems.Truncate(_renderItems.Count - diff);
            _keys.Truncate(_keys.Count - diff);
            _lastVertexBuffer0 = vb0;
            _lastVertexBuffer1 = vb1;
            _lastIndexBuffer = ib;
            _lastSharedResourceSet = lastSharedSet;

            (byte bufferId, ushort start, ushort length) writeUniformData(in UniformUpdate update)
            {
                byte bufferId = GetResourceId(update.Buffer, _uniformBuffers, ref uniform);
                uint start = _uniformData.Count;
                uint length = (uint)Math.Ceiling(update.Data.Length / 16.0);
                Span<Vector4> dst = _uniformData.Append(length);
                update.Data.Span.CopyTo(MemoryMarshal.Cast<Vector4, byte>(dst));
                return (bufferId, (ushort)start, (ushort)length);
            }
        }

        public void End(CommandList commandList)
        {
            Array.Sort(_keys.UnderlyingArray, _renderItems.UnderlyingArray, 0, (int)_renderItems.Count);

            byte lastVertexBuffer0 = byte.MaxValue;
            byte lastVertexBuffer1 = byte.MaxValue;
            byte lastIndexBuffer = byte.MaxValue;
            byte lastPipelineId = byte.MaxValue;
            byte lastSharedResourceSetId = byte.MaxValue;
            ResourceSet? lastObjectResourceSet0 = null;
            ResourceSet? lastObjectResourceSet1 = null;
            for (uint i = 0; i < _renderItems.Count; i++)
            {
                ref RenderItem item = ref _renderItems[i];
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
                    Span<Vector4> data = _uniformData.AsSpan(
                        item.UniformDataStart,
                        item.UniformDataLength
                    );
                    commandList.UpdateBuffer(
                        buffer, 0, ref data[0],
                        (uint)(item.UniformDataLength * 16)
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
