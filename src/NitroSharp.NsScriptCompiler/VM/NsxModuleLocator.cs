using System.IO;

namespace NitroSharp.NsScriptNew.VM
{
    public abstract class NsxModuleLocator
    {
        /// <param name="name">Name without extension.</param>
        public abstract Stream OpenModule(string name);
    }

    public sealed class FileSystemNsxModuleLocator : NsxModuleLocator
    {
        private readonly string _rootDirectory;

        public FileSystemNsxModuleLocator(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
        }

        /// <param name="name">Name without extension.</param>
        public override Stream OpenModule(string name)
        {
            string path = Path.Combine(_rootDirectory, name) + ".nsx";
            return File.OpenRead(path);
        }
    }
}
