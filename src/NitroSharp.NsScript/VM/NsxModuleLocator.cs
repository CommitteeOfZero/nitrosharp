using System.IO;

namespace NitroSharp.NsScript.VM;

public abstract class NsxModuleLocator
{
    /// <param name="name">Name without extension.</param>
    public abstract Stream OpenModule(string name);
}

public sealed class FileSystemNsxModuleLocator(string rootDirectory) : NsxModuleLocator
{
    public override Stream OpenModule(string name)
    {
        string path = Path.Combine(rootDirectory, name) + ".nsx";
        return File.OpenRead(path);
    }
}
