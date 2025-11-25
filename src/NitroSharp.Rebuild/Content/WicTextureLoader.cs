using System;
using System.IO;
using Veldrid;
using Vortice.WIC;
using PixelFormat = Veldrid.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace NitroSharp.Content;

internal sealed unsafe class WicTextureLoader(GraphicsDevice graphicsDevice)
    : TextureLoader(graphicsDevice)
{
    private readonly IWICImagingFactory _wicFactory = new();

    protected override Texture LoadStaging(Stream stream)
    {
        using IWICStream wicStream = _wicFactory.CreateStream(stream);
        using IWICBitmapDecoder decoder = _wicFactory.CreateDecoderFromStream(wicStream);
        using IWICFormatConverter? formatConv = _wicFactory.CreateFormatConverter();
        // Do NOT dispose the frame as it might lead to a crash.
        // Seems like it's owned by the decoder, so hopefully there should be no leaks.
        IWICBitmapFrameDecode frame = decoder.GetFrame(0);
        formatConv.Initialize(frame, Vortice.WIC.PixelFormat.Format32bppRGBA);

        uint width = (uint)frame.Size.Width;
        uint height = (uint)frame.Size.Height;
        Texture stagingTexture = _rf.CreateTexture(TextureDescription.Texture2D(
            width, height, mipLevels: 1, arrayLayers: 1,
            PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
        ));

        MappedResource map = _gd.Map(stagingTexture, MapMode.Write);
        uint rowWidth = width * 4;
        if (rowWidth == map.RowPitch)
        {

            formatConv.CopyPixels((int)map.RowPitch, (int)map.SizeInBytes, map.Data);
        }
        else
        {
            for (uint y = 0; y < height; y++)
            {
                byte* dstStart = (byte*)map.Data + y * map.RowPitch;
                formatConv.CopyPixels(
                    new Rectangle(x: 0, (int)y, (int)width, height: 1),
                    stride: (int)map.RowPitch,
                    size: (int)map.RowPitch,
                    (IntPtr)dstStart
                );
            }
        }

        _gd.Unmap(stagingTexture);
        return stagingTexture;
    }

    public override void Dispose()
    {
        base.Dispose();
        _wicFactory.Dispose();
    }

    public override TextureSizeU GetTextureSize(Stream stream)
    {
        using IWICStream wicStream = _wicFactory.CreateStream(stream);
        using IWICBitmapDecoder decoder = _wicFactory.CreateDecoderFromStream(wicStream);
        // Do NOT dispose the frame as it might lead to a crash.
        // Seems like it's owned by the decoder, so hopefully there should be no leaks.
        IWICBitmapFrameDecode frame = decoder.GetFrame(0);
        stream.Seek(0, SeekOrigin.Begin);
        return new TextureSizeU((uint)frame.Size.Width, (uint)frame.Size.Height);
    }
}
