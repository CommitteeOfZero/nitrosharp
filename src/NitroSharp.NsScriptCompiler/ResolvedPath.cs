using System;

namespace NitroSharp.NsScriptNew
{
    public readonly struct ResolvedPath : IEquatable<ResolvedPath>
    {
        internal ResolvedPath(string path)
        {
            Value = path;
        }

        public string Value { get; }

        public bool Equals(ResolvedPath other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value;
    }
}
