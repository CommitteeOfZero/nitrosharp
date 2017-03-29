namespace ProjectHoppy.Content
{
    public struct AssetRef
    {
        public AssetRef(string assetPath)
        {
            AssetPath = assetPath;
        }

        public string AssetPath { get; }

        public static implicit operator AssetRef(string assetPath) => new AssetRef(assetPath);
        public static implicit operator string(AssetRef assetRef) => assetRef.AssetPath;
    }
}
