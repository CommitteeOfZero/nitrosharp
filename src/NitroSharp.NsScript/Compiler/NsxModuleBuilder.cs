using System;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript.Compiler;

internal sealed class NsxModuleBuilder
{
    private readonly TokenMap<SubroutineSymbol> _subroutines;
    private readonly TokenMap<SourceFileSymbol> _externalSourceFiles;
    private readonly TokenMap<string> _stringHeap;
    private ArrayBuilder<SourceMapping> _sourceMappings = new(initialCapacity: 1024);

    public NsxModuleBuilder(Compilation compilation, SourceFileSymbol sourceFile)
    {
        Compilation = compilation;
        SourceFile = sourceFile;
        Diagnostics = new DiagnosticBuilder();
        _stringHeap = new TokenMap<string>(512);
        _subroutines = new TokenMap<SubroutineSymbol>(sourceFile.SubroutineCount);
        _externalSourceFiles = new TokenMap<SourceFileSymbol>();
        ConstructSubroutineMap(sourceFile);
    }

    public Compilation Compilation { get; }
    public SourceFileSymbol SourceFile { get; }
    public DiagnosticBuilder Diagnostics { get; }

    public ReadOnlySpan<SubroutineSymbol> Subroutines => _subroutines.AsSpan();
    public ReadOnlySpan<SourceFileSymbol> Imports => _externalSourceFiles.AsSpan();
    public ReadOnlySpan<string> StringHeap => _stringHeap.AsSpan();
    public Span<SourceMapping> SourceMappings => _sourceMappings.AsSpan();

    private void ConstructSubroutineMap(SourceFileSymbol sourceFile)
    {
        foreach (ChapterSymbol chapter in sourceFile.Chapters)
        {
            _subroutines.GetOrAddToken(chapter);
        }
        foreach (SceneSymbol scene in sourceFile.Scenes)
        {
            _subroutines.GetOrAddToken(scene);
        }
        foreach (FunctionSymbol function in sourceFile.Functions)
        {
            _subroutines.GetOrAddToken(function);
        }
    }

    public ushort GetExternalModuleToken(SourceFileSymbol sourceFile)
    {
        return _externalSourceFiles.GetOrAddToken(sourceFile);
    }

    public ushort GetSubroutineToken(SubroutineSymbol subroutine)
    {
        return _subroutines.GetOrAddToken(subroutine);
    }

    public ushort GetStringToken(string s)
    {
        return _stringHeap.GetOrAddToken(s);
    }

    public void AddSourceMapping(SourceMapping range)
    {
        _sourceMappings.Add(range);
    }
}
