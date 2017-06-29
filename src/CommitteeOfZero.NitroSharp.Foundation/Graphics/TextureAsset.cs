using SharpDX.Direct2D1;
using System;
using System.Diagnostics;

namespace CommitteeOfZero.NitroSharp.Foundation.Graphics
{
    public struct TextureAsset : IDisposable
    {
        public TextureAsset(object deviceTexture)
        {
            Debug.Assert(deviceTexture != null);
            DeviceTexture = deviceTexture;
        }

        object DeviceTexture { get; }

        public static implicit operator SharpDX.Direct2D1.Bitmap1(TextureAsset asset)
        {
            return asset.DeviceTexture as SharpDX.Direct2D1.Bitmap1;
        }

        public float Width => ((SharpDX.Direct2D1.Bitmap1)DeviceTexture).Size.Width;

        public float Height => ((SharpDX.Direct2D1.Bitmap1)DeviceTexture).Size.Height;

        public void Dispose()
        {
            (DeviceTexture as Bitmap1)?.Dispose();
        }
    }
}
