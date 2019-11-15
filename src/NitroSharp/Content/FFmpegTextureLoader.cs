using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using FFmpeg.AutoGen;
using NitroSharp.Media.Decoding;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Content
{
    internal unsafe sealed class FFmpegTextureLoader : TextureLoader
    {
        private readonly VideoFrameConverter _frameConverter;
        private readonly AVInputFormat* _inputFormat;

        public FFmpegTextureLoader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice)
        {
            _frameConverter = new VideoFrameConverter();
            var decoderCollection = DecoderCollection.Shared;
            decoderCollection.Preload(AVCodecID.AV_CODEC_ID_MJPEG);
            decoderCollection.Preload(AVCodecID.AV_CODEC_ID_PNG);
            _inputFormat = ffmpeg.av_find_input_format("image2pipe");
        }

        protected override Texture LoadStaging(Stream stream)
        {
            using (var container = MediaContainer.Open(stream, _inputFormat, leaveOpen: true))
            using (var decodingSession = new DecodingSession(container, container.BestVideoStream!.Id))
            {
                var packet = new AVPacket();
                var frame = new AVFrame();
                bool success = container.ReadFrame(&packet) == 0;
                success = decodingSession.TryDecodeFrame(ref packet, ref frame);
                Debug.Assert(success);

                uint width = (uint)frame.width;
                uint height = (uint)frame.height;
                var size = new Size(width, height);
                Texture stagingTexture = _rf.CreateTexture(TextureDescription.Texture2D(
                    width, height, mipLevels: 1, arrayLayers: 1,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
                ));

                MappedResource map = _gd.Map(stagingTexture, MapMode.Write);
                uint srcRowWidth = width * 4;
                if (srcRowWidth == map.RowPitch)
                {
                    _frameConverter.ConvertToRgba(ref frame, size, (byte*)map.Data);
                }
                else
                {
                    using (var buffer = NativeMemory.Allocate(width * height * 4))
                    {
                        _frameConverter.ConvertToRgba(ref frame, size, (byte*)buffer.Pointer);
                        byte* src = (byte*)buffer.Pointer;
                        byte* dst = (byte*)map.Data;
                        for (uint y = 0; y < height; y++)
                        {
                            Unsafe.CopyBlock(dst, src, srcRowWidth);
                            src += srcRowWidth;
                            dst += map.RowPitch;
                        }
                    }
                }

                _gd.Unmap(stagingTexture);

                FFmpegUtil.UnrefBuffers(ref packet);
                FFmpegUtil.UnrefBuffers(ref frame);

                return stagingTexture;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _frameConverter.Dispose();
        }

        public override Size GetTextureDimensions(Stream stream)
        {
            throw new System.NotImplementedException();
        }
    }
}
