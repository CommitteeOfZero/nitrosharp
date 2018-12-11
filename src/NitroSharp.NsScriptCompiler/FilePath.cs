using System;
using NitroSharp.Utilities;

namespace NitroSharp.NsScriptNew
{
    public readonly struct FilePath : IEquatable<FilePath>
    {
        public FilePath(string filePath)
        {
            Path = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public string Path { get; }

        public ReadOnlySpan<char> Name
        {
            get
            {
                ReadOnlySpan<char> span = Path.AsSpan();
                ReadOnlySpan<char> delimiters = stackalloc char[] { '/', '\\' };
                int lastSlashIdx = span.LastIndexOfAny(delimiters);
                if (lastSlashIdx > 0)
                {
                    return span.Slice(lastSlashIdx + 1);
                }

                return span;
            }
        }

        public bool Equals(FilePath other) => Name.Equals(other.Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is FilePath other && Equals(other);

        public override int GetHashCode() => HashHelper.GetFNVHashCode(Name);

        public override string ToString() => Name.ToString();
        public static implicit operator FilePath(string path) => new FilePath(path);
    }
}
