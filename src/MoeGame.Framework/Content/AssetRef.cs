using System;
using System.Diagnostics;
using System.IO;

namespace MoeGame.Framework.Content
{
    public struct AssetRef : IEquatable<AssetRef>
    {
        public AssetRef(string assetPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(assetPath));
            AssetPath = assetPath;
        }

        public string AssetPath { get; }

        public static implicit operator AssetRef(string assetPath) => new AssetRef(assetPath);
        public static implicit operator string(AssetRef assetRef) => assetRef.AssetPath;

        public override string ToString() => AssetPath;

        public bool Equals(AssetRef other) => EqualsImpl(this, other);
        public override bool Equals(object obj) => EqualsImpl(this, (AssetRef)obj);

        private static bool EqualsImpl(AssetRef a, AssetRef b)
        {
            string name1 = Path.GetFileName(a.AssetPath);
            string name2 = Path.GetFileName(b.AssetPath);
            return name1.Equals(name2, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return AssetPath.GetHashCode() * 29;
        }

        public static bool operator ==(AssetRef a, AssetRef b) => EqualsImpl(a, b);
        public static bool operator !=(AssetRef a, AssetRef b) => !EqualsImpl(a, b);
    }
}
