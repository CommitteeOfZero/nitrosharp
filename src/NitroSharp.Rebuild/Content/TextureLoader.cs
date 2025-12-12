using System;
using System.IO;
using Veldrid;

namespace NitroSharp.Content;

internal abstract class TextureLoader : IDisposable
{
    protected readonly GraphicsDevice _gd;
    protected readonly ResourceFactory _rf;
    private readonly CommandList _cl;

    protected TextureLoader(GraphicsDevice graphicsDevice)
    {
        _gd = graphicsDevice;
        _rf = graphicsDevice.ResourceFactory;
        _cl = _gd.ResourceFactory.CreateCommandList();
        _cl.Name = "Content loading";
    }

    protected abstract Texture LoadStaging(Stream stream);
    public abstract TextureSizeU GetTextureSize(Stream stream);

    public Texture LoadTexture(Stream stream, bool staging)
    {
        using (stream)
        {
            Texture stagingTex = LoadStaging(stream);
            if (staging) { return stagingTex; }

            Texture sampledTex = _rf.CreateTexture(TextureDescription.Texture2D(
                stagingTex.Width, stagingTex.Height, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled
            ));

            // TODO: why are we creating a new command list per texture load?
            using CommandList cl = _gd.ResourceFactory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(source: stagingTex, destination: sampledTex);
            cl.End();
            _gd.SubmitCommands(cl);
            stagingTex.Dispose();
            return sampledTex;
        }
    }

    public virtual void Dispose()
    {
        _cl.Dispose();
    }
}
