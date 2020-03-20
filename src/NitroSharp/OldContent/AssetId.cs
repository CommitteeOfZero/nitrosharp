using System;

#nullable enable

namespace NitroSharp.OldContent
{
    internal readonly struct AssetId : IEquatable<AssetId>
    {
        public AssetId(string value)
        {
            NormalizedPath = value.Replace('\\', '/');
        }

        public string NormalizedPath { get; }

        public bool Equals(AssetId other)
            => other.NormalizedPath.Equals(NormalizedPath, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is AssetId other && other.Equals(this);

        public override int GetHashCode()
            => NormalizedPath != null ? NormalizedPath.GetHashCode(StringComparison.Ordinal) : 0;

        public override string ToString() => NormalizedPath;

        public static bool operator ==(AssetId a, AssetId b) => a.Equals(b);
        public static bool operator !=(AssetId a, AssetId b) => !a.Equals(b);
    }
}
