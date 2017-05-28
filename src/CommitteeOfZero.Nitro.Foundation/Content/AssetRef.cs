using System;
using System.Diagnostics;
using System.IO;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public struct AssetRef : IEquatable<AssetRef>
    {
        public AssetRef(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            FilePath = filePath;
        }

        public string FilePath { get; }

        public bool TryResolve<T>(out T asset) => ContentManager.Instance.TryGetAsset(this, out asset);

        public static implicit operator AssetRef(string assetPath) => new AssetRef(assetPath);
        public static implicit operator string(AssetRef assetRef) => assetRef.FilePath;

        public override string ToString() => FilePath;

        public bool Equals(AssetRef other) => EqualsImpl(this, other);
        public override bool Equals(object obj) => EqualsImpl(this, (AssetRef)obj);

        private static bool EqualsImpl(AssetRef a, AssetRef b)
        {
            return a.FilePath.Equals(b.FilePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode() * 29;
        }

        public static bool operator ==(AssetRef a, AssetRef b) => EqualsImpl(a, b);
        public static bool operator !=(AssetRef a, AssetRef b) => !EqualsImpl(a, b);
    }
}
