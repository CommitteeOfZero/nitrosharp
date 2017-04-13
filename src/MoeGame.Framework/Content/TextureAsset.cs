namespace MoeGame.Framework.Content
{
    public struct TextureAsset
    {
        public TextureAsset(object deviceTexture)
        {
            DeviceTexture = deviceTexture;
        }

        object DeviceTexture { get; }

        public static implicit operator SharpDX.Direct2D1.Bitmap1(TextureAsset asset)
        {
            return asset.DeviceTexture as SharpDX.Direct2D1.Bitmap1;
        }

        public float Width => ((SharpDX.Direct2D1.Bitmap1)DeviceTexture).Size.Width;

        public float Height => ((SharpDX.Direct2D1.Bitmap1)DeviceTexture).Size.Height;
    }
}
