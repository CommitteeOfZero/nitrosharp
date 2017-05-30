namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public class AssetRef
    {
        public AssetRef(AssetId id)
        {
            Id = id;
            ContentManager.Instance.RegisterReference(Id);
        }

        public AssetId Id { get; }

        public T Get<T>() => ContentManager.Instance.Get<T>(Id);

        public static implicit operator AssetRef(string assetPath) => new AssetRef(assetPath);
        public override string ToString() => Id;

        public void Release()
        {
            ContentManager.Instance.UnregisterReference(Id);
            //Debug.WriteLine($"Reference to {Id} no longer in use");
        }
    }
}
