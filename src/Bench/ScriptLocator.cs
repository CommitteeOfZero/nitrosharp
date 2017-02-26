using SciAdvNet.NSScript.Execution;
using System.IO;
using System.IO.Compression;

namespace Bench
{
    public class ScriptLocator : IScriptLocator
    {
        private readonly ZipArchive _content = ZipFile.OpenRead("Content.zip");

        public Stream Locate(string fileName)
        {
            return _content.GetEntry(fileName).Open();
        }
    }
}
