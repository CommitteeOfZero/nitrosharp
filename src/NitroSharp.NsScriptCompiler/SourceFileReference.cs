using System;

namespace NitroSharp.NsScriptNew
{
    public struct SourceFileReference : IEquatable<SourceFileReference>
    {
        private string _fileName;
        
        public SourceFileReference(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
            _fileName = null;
        }

        public string FileName
        {
            get
            {
                if (_fileName == null)
                {
                    _fileName = System.IO.Path.GetFileName(FilePath);
                }

                return _fileName;
            }
        }
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
