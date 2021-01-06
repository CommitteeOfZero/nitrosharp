using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Core
{
    internal readonly struct TextureCacheHandle : IEquatable<TextureCacheHandle>
    {
        public readonly WeakFreeListHandle Value;

        public TextureCacheHandle(WeakFreeListHandle value)
            => Value = value;

        public static TextureCacheHandle Invalid =>
            new(WeakFreeListHandle.Invalid);

        public bool IsValid => Value.Version != 0;

        public bool Equals(TextureCacheHandle other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct TextureLocation : GpuType
    {
        public const uint SizeInGpuBlocks = 2;

        public readonly Point2DU Origin;
        public readonly Size Size;
        public readonly uint Layer;
        public readonly Vector3 UserData;

        public TextureLocation(
            Point2DU origin,
            Size size,
            uint layer,
            Vector3 userData)
        {
            Origin = origin;
            Size = size;
            Layer = layer;
            UserData = userData;
        }

        public void WriteGpuBlocks(Span<Vector4> blocks)
        {
            blocks[0] = new Vector4(Layer, Origin.X, Origin.Y, Size.Width);
            blocks[1] = new Vector4(Size.Height, UserData.X, UserData.Y, UserData.Z);
        }
    }

    internal readonly struct TextureCacheItem
    {
        public readonly int UvRectPosition;
        public readonly uint Layer;

        public TextureCacheItem(int uvRectPosition, uint layer)
            => (UvRectPosition, Layer) = (uvRectPosition, layer);
    }

    internal sealed class TextureCache : IDisposable
    {
        [StructLayout(LayoutKind.Auto)]
        private struct CacheEntry
        {
            public PixelFormat PixelFormat;
            public Size Size;
            public ArrayTextureAllocation Allocation;
            public FrameStamp LastAccess;
            public GpuCacheHandle UvRectHandle;
        }

        private readonly FreeList<CacheEntry> _entries;
        private readonly List<FreeListHandle> _strongHandles;
        private readonly ArrayTexture _rgbaTexture;
        private readonly ArrayTexture _r8Texture;
        private readonly GpuCache<TextureLocation> _uvRectCache;
        private readonly uint _maxTextureLayers;

        private FrameStamp _now;

        public TextureCache(
            GraphicsDevice graphicsDevice,
            uint initialLayerCount = 8,
            uint maxTextureLayers = 32)
        {
            _entries = new FreeList<CacheEntry>();
            _strongHandles = new List<FreeListHandle>();
            _maxTextureLayers = maxTextureLayers;
            _now = FrameStamp.Invalid;
            _rgbaTexture = new ArrayTexture(
                graphicsDevice,
                PixelFormat.R8_G8_B8_A8_UNorm,
                initialLayerCount
            );
            _r8Texture = new ArrayTexture(
                graphicsDevice,
                PixelFormat.R8_UNorm,
                initialLayerCount
            );
            _uvRectCache = new GpuCache<TextureLocation>(
                graphicsDevice,
                TextureLocation.SizeInGpuBlocks,
                dimension: 256
            );
        }

        public Texture UvRectTexture => _uvRectCache.Texture;

        private FrameStamp DefaultEvictionPolicy => GetEvictionThreshold(
            maxFrames: null, maxTimeMs: 5000
        );

        private FrameStamp AgressiveEvictionPolicy => GetEvictionThreshold(
            maxFrames: 1, maxTimeMs: null
        );

        private ArrayTexture SelectArrayTexture(PixelFormat pixelFormat)
        {
            return pixelFormat switch
            {
                PixelFormat.R8_G8_B8_A8_UNorm => _rgbaTexture,
                PixelFormat.R8_UNorm => _r8Texture,
                _ => UnsupportedPixelFormat(pixelFormat)
            };
        }

        public Texture GetCacheTexture(PixelFormat pixelFormat, out bool reallocatedThisFrame)
        {
            ArrayTexture tex = SelectArrayTexture(pixelFormat);
            reallocatedThisFrame = tex.ReallocatedThisFrame;
            return tex.DeviceTexture;
        }

        public void BeginFrame(in FrameStamp framestamp)
        {
            _now = framestamp;
            _r8Texture.BeginFrame();
            _rgbaTexture.BeginFrame();
            _uvRectCache.BeginFrame();
        }

        public void EndFrame(CommandList commandList)
        {
            Debug.Assert(_now.IsValid);
            _r8Texture.EndFrame(commandList);
            _rgbaTexture.EndFrame(commandList);
            _uvRectCache.EndFrame(commandList);
            _now = FrameStamp.Invalid;
        }

        public bool RequestEntry(TextureCacheHandle handle)
        {
            Debug.Assert(_now.IsValid);
            RefOption<CacheEntry> entryOpt = _entries.GetOpt(handle.Value);
            if (entryOpt.HasValue)
            {
                entryOpt.Unwrap().LastAccess = _now;
                return true;
            }

            return false;
        }

        public TextureCacheItem Get(TextureCacheHandle handle)
        {
            RefOption<CacheEntry> entryOpt = _entries.GetOpt(handle.Value);
            ref CacheEntry entry = ref entryOpt.Unwrap();
            int uvRectPos = _uvRectCache.GetCachePosition(entry.UvRectHandle);
            return new TextureCacheItem(uvRectPos, entry.Allocation.Layer);
        }

        public void Update<TPix>(
            ref TextureCacheHandle handle,
            PixelFormat pixelFormat,
            Size textureSize,
            ReadOnlySpan<TPix> pixels,
            Vector3 userData = default)
            where TPix : unmanaged
        {
            Debug.Assert(_now.IsValid);
            RefOption<CacheEntry> entryOpt = _entries.GetOpt(handle.Value);
            bool exists;
            if (entryOpt.HasValue)
            {
                ref CacheEntry existingEntry = ref entryOpt.Unwrap();
                exists = existingEntry.PixelFormat == pixelFormat
                    && existingEntry.Size.Equals(textureSize);
            }
            else
            {
                exists = false;
            }

            if (!exists)
            {
                Allocate(ref handle, pixelFormat, textureSize);
            }

            ref CacheEntry entry = ref _entries.GetOpt(handle.Value).Unwrap();
            ArrayTexture arrayTexture = SelectArrayTexture(entry.PixelFormat);
            ArrayTextureAllocation alloc = entry.Allocation;
            arrayTexture.UploadData(alloc, entry.Size, pixels);
            var location = new TextureLocation(
                origin: alloc.Location,
                size: textureSize,
                alloc.Layer,
                userData
            );
            _uvRectCache.Upsert(ref entry.UvRectHandle, ref location);
        }

        public void Clear()
        {
            if (_strongHandles.Count == 0) { return; }
            for (int i = _strongHandles.Count - 1; i >= 0; i--)
            {
                CacheEntry entry = _entries.Free(_strongHandles[i]);
                FreeEntry(ref entry);
                RemoveStrongHandle(i);
            }
        }

        private void Allocate(ref TextureCacheHandle handle, PixelFormat pixelFormat, Size textureSize)
        {
            CacheEntry entry = DoAllocateEntry(pixelFormat, textureSize);
            (FreeListHandle? newHandleOpt, CacheEntry? oldValue) = _entries.Upsert(
                handle.Value, entry
            );
            if (newHandleOpt is FreeListHandle newHandle)
            {
                _strongHandles.Add(newHandle);
                handle = new TextureCacheHandle(newHandle.GetWeakHandle());
            }
            else if (oldValue is CacheEntry oldEntry)
            {
                FreeEntry(ref oldEntry);
            }
        }

        private CacheEntry DoAllocateEntry(PixelFormat pixelFormat, Size textureSize)
        {
            ArrayTexture arrayTexture = SelectArrayTexture(pixelFormat);
            CacheEntry? entryOpt = TryAllocateEntry(arrayTexture, textureSize);
            if (entryOpt.HasValue) { return entryOpt.Value; }

            FrameStamp evictionThreshold = arrayTexture.LayerCount < _maxTextureLayers
                ? DefaultEvictionPolicy
                : AgressiveEvictionPolicy;
            EvictOldEntries(evictionThreshold);

            entryOpt = TryAllocateEntry(arrayTexture, textureSize);
            if (entryOpt.HasValue) { return entryOpt.Value; }

            if (arrayTexture.LayerCount < _maxTextureLayers)
            {
                arrayTexture.AddLayer();
                entryOpt = TryAllocateEntry(arrayTexture, textureSize);
                return entryOpt!.Value;
            }

            throw new Exception("BUG: texture cache is full. This was thought to be impossible.");
        }

        private CacheEntry? TryAllocateEntry(ArrayTexture arrayTexture, Size textureSize)
        {
            ArrayTextureAllocation? allocOpt = arrayTexture.AllocateSpace(textureSize);
            if (allocOpt is ArrayTextureAllocation alloc)
            {
                return new CacheEntry
                {
                    PixelFormat = arrayTexture.PixelFormat,
                    Size = textureSize,
                    Allocation = alloc,
                    LastAccess = _now
                };
            }

            return null;
        }

        private void EvictOldEntries(FrameStamp evictionThreshold)
        {
            if (_strongHandles.Count == 0) { return; }
            for (int i = _strongHandles.Count - 1; i >= 0; i--)
            {
                FreeListHandle handle = _strongHandles[i];
                ref CacheEntry entryRef = ref _entries.Get(handle);
                FrameStamp lastAccess = entryRef.LastAccess;
                bool evict = lastAccess.FrameId < evictionThreshold.FrameId
                    && lastAccess.StopwatchTicks < evictionThreshold.StopwatchTicks;
                if (evict)
                {
                    Debug.Assert(lastAccess.FrameId != _now.FrameId);
                    CacheEntry entry = _entries.Free(handle);
                    FreeEntry(ref entry);
                    RemoveStrongHandle(i);
                }
            }
        }

        private void RemoveStrongHandle(int index)
        {
            int idxLast = _strongHandles.Count - 1;
            _strongHandles[index] = _strongHandles[idxLast];
            _strongHandles.RemoveAt(idxLast);
        }

        private FrameStamp GetEvictionThreshold(uint? maxFrames, uint? maxTimeMs)
        {
            FrameStamp now = _now;
            long frameId = now.FrameId - (maxFrames ?? 0);
            long timestamp = now.StopwatchTicks
                - ((maxTimeMs ?? 0) * Stopwatch.Frequency / 1000);
            if (timestamp < 0) { timestamp = 0L; }
            return new FrameStamp(frameId, timestamp);
        }

        private void FreeEntry(ref CacheEntry cacheEntry)
        {
            ArrayTexture texture = SelectArrayTexture(cacheEntry.PixelFormat);
            texture.Free(cacheEntry.Allocation);
            _uvRectCache.Free(ref cacheEntry.UvRectHandle);
        }

        public void Dispose()
        {
            _r8Texture.Dispose();
            _rgbaTexture.Dispose();
            _uvRectCache.Dispose();
        }

        private ArrayTexture UnsupportedPixelFormat(PixelFormat format)
            => throw new InvalidOperationException(
                $"The following pixel format is not supported by the texture cache: {format}");
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct ArrayTextureAllocation
    {
        public readonly uint Layer;
        public readonly Point2DU Location;

        public ArrayTextureAllocation(uint layer, Point2DU location)
            => (Layer, Location) = (layer, location);
    }

    internal sealed class ArrayTexture : IDisposable
    {
        private const uint Dimensions = 512;

        private struct Layer
        {
            private readonly Queue<(byte, byte)> _freeSlots;
            private uint _slotCount;
            public MappedResource Map;

            public Layer(uint index)
            {
                Index = index;
                _freeSlots = new Queue<(byte, byte)>();
                _slotCount = 0;
                SlabSize = default;
                DirtyRect = default;
                UsedRect = default;
                Map = default;
            }

            public uint Index { get; }
            public Size SlabSize { get; private set; }
            public RectangleU DirtyRect { get; private set; }
            public RectangleU UsedRect { get; private set; }
            public bool IsEmpty => SlabSize.Equals(default);

            public void Initialize(Size slabSize)
            {
                SlabSize = slabSize;
                uint slotsPerX = Dimensions / slabSize.Width;
                uint slotsPerY = Dimensions / slabSize.Height;
                for (int y = 0; y < slotsPerY; y++)
                {
                    for (int x = 0; x < slotsPerX; x++)
                    {
                        _freeSlots.Enqueue(((byte)x, (byte)y));
                    }
                }
                _slotCount = (uint)_freeSlots.Count;
            }

            public Point2DU? AllocateBlock()
            {
                if (_freeSlots.Count == 0) { return null; }
                (byte x, byte y) = _freeSlots.Dequeue();
                (uint w, uint h) = (SlabSize.Width, SlabSize.Height);
                var rect = new RectangleU(x * w, y * h, w, h);
                DirtyRect = DirtyRect.Width > 0
                    ? RectangleU.Union(DirtyRect, rect)
                    : rect;
                UsedRect = RectangleU.Union(UsedRect, DirtyRect);
                return new Point2DU(rect.X, rect.Y);
            }

            public void ResetDirtyRect() => DirtyRect = default;
            public void OnRealloc() => DirtyRect = UsedRect;

            public void Free(Point2DU point)
            {
                uint x = point.X / SlabSize.Width;
                uint y = point.Y / SlabSize.Height;
                _freeSlots.Enqueue(((byte)x, (byte)y));
                if (_freeSlots.Count == _slotCount)
                {
                    Deinitialize();
                }
            }

            private void Deinitialize()
            {
                SlabSize = default;
                _freeSlots.Clear();
                _slotCount = 0;
                DirtyRect = default;
                UsedRect = default;
            }
        }

        private readonly GraphicsDevice _gd;
        private ArrayBuilder<Layer> _layers;
        private Texture? _stagingTexture;
        private Texture? _sampledTexture;
        private bool _mapped;

        public ArrayTexture(
            GraphicsDevice graphicsDevice,
            PixelFormat pixelFormat,
            uint layerCount)
        {
            _gd = graphicsDevice;
            PixelFormat = pixelFormat;
            _layers = new ArrayBuilder<Layer>(layerCount);
            AllocateTexture(layerCount);
        }

        public Texture DeviceTexture
        {
            get
            {
                static void inUse() => throw new InvalidOperationException(
                   "The requested TextureCache texture is currently in use. " +
                   "Did you forget to call EndFrame()?");

                Debug.Assert(_sampledTexture != null);
                if (_mapped) { inUse(); }
                return _sampledTexture;
            }
        }

        public bool ReallocatedThisFrame { get; private set; }

        private uint BytesPerPixel =>
            PixelFormat switch
            {
                PixelFormat.R8_UNorm => 1u,
                PixelFormat.R8_G8_B8_A8_UNorm => 4u,
                _ => ThrowHelper.Unreachable<uint>()
            };

        private unsafe void AllocateTexture(uint layerCount)
        {
            var desc = TextureDescription.Texture2D(
               Dimensions, Dimensions, mipLevels: 1, layerCount,
               PixelFormat, TextureUsage.Staging
            );
            ResourceFactory rf = _gd.ResourceFactory;
            Texture newStaging = rf.CreateTexture(ref desc);
            desc.Usage = TextureUsage.Sampled;
            Texture newSampled = rf.CreateTexture(ref desc);
            if (_stagingTexture != null)
            {
                foreach (ref Layer layer in _layers.AsSpan())
                {
                    MappedResource src = layer.Map;
                    MappedResource dst = _gd.Map(newStaging, MapMode.Write, layer.Index);
                    RectangleU rect = layer.UsedRect;
                    if (rect.Width > 0)
                    {
                        GraphicsUtils.CopyTextureRegion(
                            src.Data.ToPointer(),
                            srcX: rect.X, srcY: rect.Y, srcZ: 0,
                            src.RowPitch, src.DepthPitch,
                            dst.Data.ToPointer(),
                            dstX: rect.X, dstY: rect.Y, dstZ: 0,
                            dst.RowPitch, dst.DepthPitch,
                            rect.Width, rect.Height,
                            depth: 1, BytesPerPixel
                        );
                    }
                    _gd.Unmap(src.Resource, layer.Index);
                    layer.Map = dst;
                    layer.OnRealloc();
                }

                uint prevLayerCount = _layers.Count;
                uint nbNewLayers = prevLayerCount == 0
                    ? layerCount
                    : layerCount - prevLayerCount;
                for (uint i = prevLayerCount; i < prevLayerCount + nbNewLayers; i++)
                {
                    ref Layer layer = ref _layers.Add();
                    layer = new Layer(i);
                    layer.Map = _gd.Map(newStaging, MapMode.Write, layer.Index);
                }

                _gd.DisposeWhenIdle(_stagingTexture);
                _gd.DisposeWhenIdle(_sampledTexture);
            }
            else
            {
                for (uint i = 0; i < layerCount; i++)
                {
                    _layers.Add() = new Layer(i);
                }
            }
            _stagingTexture = newStaging;
            _sampledTexture = newSampled;
            ReallocatedThisFrame = true;
        }

        public PixelFormat PixelFormat { get; }
        public uint LayerCount => _layers.Count;

        public void BeginFrame()
        {
            foreach (ref Layer layer in _layers.AsSpan())
            {
                layer.Map = _gd.Map(_stagingTexture, MapMode.Write, layer.Index);
                layer.ResetDirtyRect();
            }
            _mapped = true;
            ReallocatedThisFrame = false;
        }

        public void EndFrame(CommandList commandList)
        {
            foreach (ref Layer layer in _layers.AsSpan())
            {
                _gd.Unmap(_stagingTexture, layer.Index);
                layer.Map = default;
                RectangleU dirtyRect = layer.DirtyRect;
                if (dirtyRect.Width > 0)
                {
                    commandList.CopyTexture(
                        source: _stagingTexture,
                        dirtyRect.X, dirtyRect.Y, srcZ: 0,
                        srcMipLevel: 0, srcBaseArrayLayer: layer.Index,
                        destination: _sampledTexture,
                        dirtyRect.X, dirtyRect.Y, dstZ: 0,
                        dstMipLevel: 0, dstBaseArrayLayer: layer.Index,
                        dirtyRect.Width, dirtyRect.Height,
                        depth: 1, layerCount: 1
                    );
                }
            }
            _mapped = false;
        }

        public void AddLayer()
        {
            AllocateTexture(_layers.Count + 1);
        }

        public ArrayTextureAllocation? AllocateSpace(Size textureSize)
        {
            Size slabSize = GetSlabSize(textureSize);
            uint emptyLayer = uint.MaxValue;
            for (int i = (int)_layers.Count - 1; i >= 0; i--)
            {
                ref Layer layer = ref _layers[i];
                if (layer.IsEmpty) { emptyLayer = (uint)i; }
                else if (layer.SlabSize == slabSize)
                {
                    Point2DU? point = layer.AllocateBlock();
                    if (point != null)
                    {
                        return new ArrayTextureAllocation((uint)i, point.Value);
                    }
                }
            }

            if (emptyLayer != uint.MaxValue)
            {
                ref Layer layer = ref _layers[emptyLayer];
                layer.Initialize(slabSize);
                Point2DU? point = layer.AllocateBlock();
                if (point != null)
                {
                    return new ArrayTextureAllocation(emptyLayer, point.Value);
                }
            }

            return null;
        }

        public void Free(in ArrayTextureAllocation allocation)
        {
            ref Layer layer = ref _layers[allocation.Layer];
            layer.Free(allocation.Location);
        }

        private static Size GetSlabSize(Size textureSize)
        {
            static uint quantizeDimension(uint dim)
            {
                if (dim <= 16) { return 16; }
                if (dim <= 256) { return MathUtil.NearestPowerOfTwo(dim); }
                throw new InvalidOperationException(
                    "Texture is too large for the cache."
                );
            }

            uint width = quantizeDimension(textureSize.Width + 4);
            uint height = quantizeDimension(textureSize.Height + 4);
            uint max = Math.Max(width, height);
            return new Size(max, max);
        }

        public unsafe void UploadData<TPix>(
            in ArrayTextureAllocation allocation,
            Size size,
            ReadOnlySpan<TPix> data)
            where TPix : unmanaged
        {
            ref readonly Layer layer = ref _layers[allocation.Layer];
            Debug.Assert(layer.Map.Resource != null);
            uint bpp = BytesPerPixel;
            uint srcRowPitch = size.Width * bpp;
            uint srcDepthPitch = srcRowPitch * size.Height;
            MappedResource dst = layer.Map;
            Point2DU location = allocation.Location;
            fixed (TPix* src = &data[0])
            {
                GraphicsUtils.CopyTextureRegion(
                    src, srcX: 0, srcY: 0, srcZ: 0,
                    srcRowPitch, srcDepthPitch,
                    dst.Data.ToPointer(),
                    dstX: location.X, dstY: location.Y, dstZ: 0,
                    dst.RowPitch, dst.DepthPitch,
                    size.Width, size.Height,
                    depth: 1, BytesPerPixel
                );
            }
        }

        public void Dispose()
        {
            Debug.Assert(_stagingTexture != null);
            Debug.Assert(_sampledTexture != null);
            if (_mapped)
            {
                _gd.Unmap(_stagingTexture);
                _mapped = false;
            }
            _stagingTexture.Dispose();
            _sampledTexture.Dispose();
        }
    }
}
