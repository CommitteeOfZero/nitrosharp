using System;
using System.Collections.Generic;
using System.IO;

namespace NitroSharp.NsScript.VM;

public readonly struct SourceMappingScope(NsxModuleLocator moduleLocator) : IDisposable
{
    private readonly Dictionary<string, SourceText> _sourceTexts = new();

    public SourceText GetSourceText(string moduleName)
    {
        if (!_sourceTexts.TryGetValue(moduleName, out SourceText? sourceText))
        {
            _sourceTexts[moduleName] = sourceText = moduleLocator.LoadSourceText(moduleName);
        }

        return sourceText;
    }

    public void Dispose()
    {
    }
}

public abstract class NsxModuleLocator
{
    /// <param name="name">Name without extension.</param>
    public abstract Stream OpenModule(string name);

    protected internal abstract SourceText LoadSourceText(string moduleName);
    public SourceMappingScope BeginSourceMapping() => new(this);
}

public sealed class FileSystemNsxModuleLocator(string bytecodeDirectory, string sourceDirectory)
    : NsxModuleLocator
{
    public override Stream OpenModule(string name)
    {
        string path = Path.Combine(bytecodeDirectory, name) + ".nsx";
        return File.OpenRead(path);
    }

    protected internal override SourceText LoadSourceText(string moduleName)
    {
        string path = Path.Combine(sourceDirectory, moduleName) + ".nss";
        return SourceText.From(File.ReadAllText(path));
    }
}
