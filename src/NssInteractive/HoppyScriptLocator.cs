using SciAdvNet.NSScript;
using System.IO;
using System;
using System.IO.Compression;
using SciAdvNet.NSScript.Execution;

namespace NssInteractive
{
    public sealed class HoppyScriptLocator : IScriptLocator
    {
        private readonly ZipArchive _archive = ZipFile.OpenRead("Content.zip");

        public Stream Locate(string fileName)
        {
            //return File.OpenRead("D:\\SciAdvNet Project\\CONTENT\\Hoppy\\" + fileName);
            return _archive.GetEntry(fileName).Open();
        }
    }
}
