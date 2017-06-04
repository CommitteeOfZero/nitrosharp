using System;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public struct AssetId
    {
        public AssetId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value.Replace('\\', '/');
        }

        public string Value { get; }

        public bool Equals(AssetId other) => other.Value.Equals(Value, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is AssetId other && other.Equals(this);

        public override int GetHashCode() => Value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value) : 0;
        public override string ToString() => Value;

        public static bool operator ==(AssetId a, AssetId b) => a.Equals(b);
        public static bool operator !=(AssetId a, AssetId b) => !a.Equals(b);

        public static implicit operator AssetId(string s) => new AssetId(s);
        public static implicit operator string(AssetId id) => id.Value;
    }
}
