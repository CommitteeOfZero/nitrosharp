namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public struct AssetId
    {
        public AssetId(string value)
        {
            Value = value.Replace('\\', '/');
        }

        public string Value { get; }

        public bool Equals(AssetId other) => other.Value.Equals(Value);
        public override bool Equals(object obj) => obj is AssetId id && id.Equals(this);

        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;

        public static implicit operator AssetId(string s) => new AssetId(s);
        public static implicit operator string(AssetId id) => id.Value;
    }
}
