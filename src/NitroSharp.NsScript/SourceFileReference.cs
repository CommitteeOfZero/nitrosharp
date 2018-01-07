using System;

namespace NitroSharp.NsScript
{
    public struct SourceFileReference : IEquatable<SourceFileReference>
    {
        public SourceFileReference(string filePath)
        {
            FilePath = filePath;
            FileName = System.IO.Path.GetFileName(filePath);
        }

        public string FileName { get; }
        public string FilePath { get; }

        public bool Equals(SourceFileReference other) => FileName.Equals(other.FileName, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is SourceFileReference other && Equals(other);

        public override int GetHashCode()
        {
            int hashCode = -345578410;
            hashCode = hashCode * -1521134295 + StringComparer.CurrentCultureIgnoreCase.GetHashCode(FileName);
            return hashCode;
        }

        public override string ToString() => FileName;
        public static implicit operator SourceFileReference(string path) => new SourceFileReference(path);
    }
}
