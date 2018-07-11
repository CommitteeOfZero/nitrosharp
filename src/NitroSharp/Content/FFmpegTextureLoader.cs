using System.IO;
using System.Runtime.CompilerServices;
using FFmpeg.AutoGen;
using NitroSharp.Graphics;
using NitroSharp.Media.Decoding;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Content
{
    internal sealed class FFmpegTextureLoader : ContentLoader
    {
        private readonly VideoFrameConverter _frameConverter;
        private unsafe AVInputFormat* _inputFormat;

        public FFmpegTextureLoader(ContentManager content)
            : base(content)
        {
            _frameConverter = new VideoFrameConverter();

            var decoderCollection = DecoderCollection.Shared;
            decoderCollection.Preload(AVCodecID.AV_CODEC_ID_MJPEG);
            decoderCollection.Preload(AVCodecID.AV_CODEC_ID_PNG);
            unsafe
            {
                _inputFormat = ffmpeg.av_find_input_format("image2pipe");
            }
        }

        public override object Load(Stream stream)
        {
            unsafe
            {
                using (var container = MediaContainer.Open(stream, _inputFormat, leaveOpen: false))
                using (var decodingSession = new DecodingSession(container, container.BestVideoStream.Id))
                {
                    var packet = new AVPacket();
                    var frame = new AVFrame();
                    bool succ = container.ReadFrame(&packet) == 0;
                    succ = decodingSession.TryDecodeFrame(ref packet, ref frame);

                    var device = Content.GraphicsDevice;
                    var texture = CreateDeviceTexture(device, device.ResourceFactory, ref frame);

                    FFmpegUtil.UnrefBuffers(ref packet);
                    FFmpegUtil.UnrefBuffers(ref frame);

                    return new BindableTexture(device.ResourceFactory, texture);
                }
            }
        }

        public override void Dispose()
        {
            _frameConverter.Dispose();
            unsafe
            {
                _inputFormat = null;
            }
        }

        private unsafe Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory, ref AVFrame frame)
        {
            uint width = (uint)frame.width;
            uint height = (uint)frame.height;
            var size = new Size(width, height);

            Texture staging = factory.CreateTexture(
                TextureDescription.Texture2D(width, height, 1, 1, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            Texture result = factory.CreateTexture(
                TextureDescription.Texture2D(width, height, 1, 1, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            cl.Name = "LoadTexture";
            cl.Begin();

            uint level = 0;
            MappedResource map = gd.Map(staging, MapMode.Write, level);
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

            gd.Unmap(staging, level);
            cl.CopyTexture(
                staging, 0, 0, 0, level, 0,
                result, 0, 0, 0, level, 0,
                width, height, 1, 1);
            cl.End();

            gd.SubmitCommands(cl);
            gd.DisposeWhenIdle(staging);
            gd.DisposeWhenIdle(cl);

            return result;
        }
    }
}
