using CommitteeOfZero.NsScript.Execution;
using System.IO;

namespace CommitteeOfZero.Nitro
{
    public sealed class ScriptLocator : IScriptLocator
    {
        private readonly string _nssFolder;

        public ScriptLocator(string contentRoot)
        {
            _nssFolder = Path.Combine(contentRoot, "nss");
        }

        public Stream Locate(string fileName)
        {
            return File.OpenRead(Path.Combine(_nssFolder, fileName.Replace("nss/", string.Empty)));
        }
    }
}
