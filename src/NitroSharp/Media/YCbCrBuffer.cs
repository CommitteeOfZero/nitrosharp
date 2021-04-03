using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.VisualStudio.Threading;
using NitroSharp.Graphics;
using Veldrid;

namespace NitroSharp.Media
{
    public readonly struct VideoFrameInfo
    {
        public readonly int Serial;
        public readonly double Timestamp;
        public readonly double Duration;

        public VideoFrameInfo(int serial, double timestamp, double duration)
        {
            Timestamp = timestamp;
            Duration = duration;
            Serial = serial;
        }
    }

    internal readonly ref struct YCbCrFrame
    {
        private readonly YCbCrBufferInternal _buffer;
        private readonly uint _index;
        private readonly uint _width;
        private readonly uint _height;
        public readonly int Serial;
        public readonly double Timestamp;
        public readonly double Duration;

        public YCbCrFrame(
            YCbCrBufferInternal buffer,
            uint index, int serial,
            uint width, uint height,
            double timestamp, double duration)
        {
            _buffer = buffer;
            _index = index;
            _width = width;
            _height = height;
            Serial = serial;
            Timestamp = timestamp;
            Duration = duration;
            Serial = serial;
        }

        public VideoFrameInfo GetInfo() => new(Serial, Timestamp, Duration);

        public void CopyToDeviceMemory(CommandList commandList)
        {
            YCbCrTextures textures  = _buffer.GetTextures();
            commandList.CopyTexture(
                source: textures.LumaStaging,
                srcX: 0, srcY: 0, srcZ: 0,
                srcMipLevel: 0,
                srcBaseArrayLayer: _index,
                destination: textures.Luma,
                dstX: 0, dstY: 0, dstZ: 0,
                dstMipLevel: 0, dstBaseArrayLayer: 0,
                _width, _height,
                depth: 1, layerCount: 1
            );
            commandList.CopyTexture(
                source: textures.ChromaStaging,
                srcX: 0, srcY: 0, srcZ: 0,
                srcMipLevel: 0,
                srcBaseArrayLayer: _index * 2,
                destination: textures.Chroma,
                dstX: 0, dstY: 0, dstZ: 0,
                dstMipLevel: 0, dstBaseArrayLayer: 0,
                _width / 2, _height / 2,
                depth: 1, layerCount: 2
            );
        }

        public void Dispose()
        {
            _buffer.TakeFrame();
        }
    }

    internal readonly struct YCbCrBufferWriter
    {
        private readonly YCbCrBufferInternal _buffer;

        public YCbCrBufferWriter(YCbCrBufferInternal buffer) => _buffer = buffer;

        public ValueTask WriteFrameAsync(AVFrame frame, int serial, double timestamp, double duration)
            => _buffer.WriteFrameAsync(frame, serial, timestamp, duration);

        public void Clear() => _buffer.Clear();
    }

    internal readonly struct YCbCrBufferReader
    {
        private readonly YCbCrBufferInternal _buffer;

        public YCbCrBufferReader(YCbCrBufferInternal buffer) => _buffer = buffer;
        public bool PeekFrame(out YCbCrFrame frame) => _buffer.PeekFrame(out frame);

        public (Texture luma, Texture chroma) GetDeviceTextures()
        {
            YCbCrTextures textures = _buffer.GetTextures();
            return (textures.Luma, textures.Chroma);
        }
    }

    internal interface YCbCrBufferInternal
    {
        YCbCrTextures GetTextures();
        ValueTask WriteFrameAsync(AVFrame frame, int serial, double timestamp, double duration);
        bool TryWriteFrame(in AVFrame frame, int serial, double timestamp, double duration);
        bool PeekFrame(out YCbCrFrame frame);
        void TakeFrame();
        void Clear();
    }

    internal readonly struct YCbCrTextures : IDisposable
    {
        public readonly Texture LumaStaging;
        public readonly Texture Luma;
        public readonly Texture ChromaStaging;
        public readonly Texture Chroma;

        public YCbCrTextures(ResourceFactory resourceFactory, uint width, uint height, uint bufferSize)
        {
            static (Texture, Texture) create(
                ResourceFactory rf,
                uint w, uint h,
                uint cpuLayerCount, uint gpuLayerCount)
            {
                var desc = TextureDescription.Texture2D(
                    w, h, mipLevels: 1, cpuLayerCount,
                    PixelFormat.R8_UNorm, TextureUsage.Staging
                );
                Texture staging = rf.CreateTexture(ref desc);
                desc.Usage = TextureUsage.Sampled;
                desc.ArrayLayers = gpuLayerCount;
                Texture sampled = rf.CreateTexture(ref desc);
                return (staging, sampled);
            }

            ResourceFactory rf = resourceFactory;
            (LumaStaging, Luma) = create(rf,
                width, height,
                cpuLayerCount: bufferSize, gpuLayerCount: 1
            );
            (ChromaStaging, Chroma) = create(rf,
                width / 2, height / 2,
                cpuLayerCount: bufferSize * 2, gpuLayerCount: 2
            );
        }

        public void Dispose()
        {
            LumaStaging.Dispose();
            ChromaStaging.Dispose();
            Luma.Dispose();
            Chroma.Dispose();
        }
    }

    internal sealed class YCbCrBuffer : YCbCrBufferInternal, IDisposable
    {
        private const int BufferSize = 48;

        private readonly GraphicsDevice _gd;
        private readonly uint _width;
        private readonly uint _height;
        private readonly YCbCrTextures _textures;
        private readonly VideoFrameInfo[] _frameInfo = new VideoFrameInfo[BufferSize];

        private int _head;
        private int _tail;

        private readonly AsyncManualResetEvent _slotAvailable;

        public YCbCrBuffer(GraphicsDevice graphicsDevice, uint width, uint height)
        {
            _gd = graphicsDevice;
            _width = width;
            _height = height;
            _textures = new YCbCrTextures(_gd.ResourceFactory, width, height, BufferSize);
            Writer = new YCbCrBufferWriter(this);
            Reader = new YCbCrBufferReader(this);
            _slotAvailable = new AsyncManualResetEvent(true, allowInliningAwaiters: true);
        }

        public YCbCrBufferWriter Writer { get; }
        public YCbCrBufferReader Reader { get; }

        YCbCrTextures YCbCrBufferInternal.GetTextures() => _textures;

        void YCbCrBufferInternal.Clear()
        {
            _tail = 0;
            _head = 0;
            _slotAvailable.Set();
        }

        async ValueTask YCbCrBufferInternal.WriteFrameAsync(
            AVFrame frame, int serial,
            double timestamp, double duration)
        {
            while (!((YCbCrBufferInternal)this).TryWriteFrame(frame, serial, timestamp, duration))
            {
                await _slotAvailable.WaitAsync();
            }
        }

        bool YCbCrBufferInternal.TryWriteFrame(
            in AVFrame frame, int serial,
            double timestamp, double duration)
        {
            int tail = _tail;
            int nextTail = (tail + 1) % BufferSize;
            if (nextTail != _head)
            {
                uint index = (uint)tail;
                MappedResource luma = _gd.Map(_textures.LumaStaging, MapMode.Write, index);
                MappedResource cb = _gd.Map(_textures.ChromaStaging, MapMode.Write, index * 2);
                MappedResource cr = _gd.Map(_textures.ChromaStaging, MapMode.Write, index * 2 + 1);
                CopyPlane(frame, plane: 0, (uint)frame.width, (uint)frame.height, luma);
                CopyPlane(frame, plane: 1, (uint)frame.width / 2, (uint)frame.height / 2, cb);
                CopyPlane(frame, plane: 2, (uint)frame.width / 2, (uint)frame.height / 2, cr);
                _gd.Unmap(_textures.LumaStaging, index);
                _gd.Unmap(_textures.ChromaStaging, index * 2);
                _gd.Unmap(_textures.ChromaStaging, index * 2 + 1);

                _frameInfo[index] = new VideoFrameInfo(serial, timestamp, duration);

                _tail = nextTail;
                Debug.Assert(nextTail != _head);
                return true;
            }

            _slotAvailable.Reset();
            return false;
        }

        bool YCbCrBufferInternal.PeekFrame(out YCbCrFrame frame)
        {
            int head = _head;
            if (head == _tail)
            {
                frame = default;
                return false;
            }

            VideoFrameInfo frameInfo = _frameInfo[head];
            frame = new YCbCrFrame(
                this, (uint)head, frameInfo.Serial,
                _width, _height,
                frameInfo.Timestamp, frameInfo.Duration
            );
            return true;
        }

        void YCbCrBufferInternal.TakeFrame()
        {
            static void emtpy()
                => throw new InvalidOperationException("The video buffer is empty.");

            int head = _head;
            if (head == _tail)
            {
                emtpy();
            }

            _head = Inc(head);
            _slotAvailable.Set();
        }

        private static int Inc(int i) => (i + 1) % BufferSize;

        private static unsafe void CopyPlane(
            in AVFrame frame,
            int plane,
            uint w, uint h,
            in MappedResource dstTexture)
        {
            byte* srcData = frame.data[(uint)plane];
            byte* dstData = (byte*)dstTexture.Data;
            uint srcRowPitch = (uint)frame.linesize[(uint)plane];
            GraphicsUtils.CopyTextureRegion(
                srcData, srcX: 0, srcY: 0, srcZ: 0,
                srcRowPitch, srcDepthPitch: (uint)(frame.linesize[0] * frame.height),
                dstData, dstX: 0, dstY: 0, dstZ: 0,
                dstTexture.RowPitch, dstTexture.DepthPitch,
                w, h, depth: 1, bytesPerPixel: 1
            );
        }

        public void Dispose()
        {
            _textures.Dispose();
        }
    }
}
