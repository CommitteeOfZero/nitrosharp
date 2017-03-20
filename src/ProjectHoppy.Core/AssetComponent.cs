namespace ProjectHoppy.Core
{
    public class AssetComponent : Component
    {
        public AssetComponent(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; set; }
    }
}
