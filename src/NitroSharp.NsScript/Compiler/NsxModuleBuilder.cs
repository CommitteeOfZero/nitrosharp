using System;

namespace NitroSharp.NsScript.Compiler
{
    internal sealed class NsxModuleBuilder
    {
        private readonly Compilation _compilation;
        private readonly SourceFileSymbol _sourceFile;
        private readonly DiagnosticBuilder _diagnostics;

        private readonly TokenMap<SubroutineSymbol> _subroutines;
        private readonly TokenMap<SourceFileSymbol> _externalSourceFiles;
        private readonly TokenMap<string> _stringHeap;

        public NsxModuleBuilder(Compilation compilation, SourceFileSymbol sourceFile)
        {
            _compilation = compilation;
            _sourceFile = sourceFile;
            _diagnostics = new DiagnosticBuilder();
            _stringHeap = new TokenMap<string>(512);
            _subroutines = new TokenMap<SubroutineSymbol>(sourceFile.SubroutineCount);
            ConstructSubroutineMap(sourceFile);
            _externalSourceFiles = new TokenMap<SourceFileSymbol>();
        }

        public Compilation Compilation => _compilation;
        public SourceFileSymbol SourceFile => _sourceFile;
        public DiagnosticBuilder Diagnostics => _diagnostics;
        public ReadOnlySpan<SubroutineSymbol> Subroutines => _subroutines.AsSpan();
        public ReadOnlySpan<SourceFileSymbol> Imports => _externalSourceFiles.AsSpan();
        public ReadOnlySpan<string> StringHeap => _stringHeap.AsSpan();

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
    }
}
