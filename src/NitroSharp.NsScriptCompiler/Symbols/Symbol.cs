using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using NitroSharp.NsScriptNew.Syntax;

namespace NitroSharp.NsScriptNew.Symbols
{
    public abstract class Symbol
    {
        public abstract SymbolKind Kind { get; }
    }

    public enum SymbolKind
    {
        Module,
        SourceFile,
        Chapter,
        Scene,
        Function,
        Parameter,
        BuiltInFunction,
        DialogueBlock
    }

    public sealed class ModuleSymbol : Symbol
    {
        internal ModuleSymbol(ImmutableArray<SyntaxTree> syntaxTrees)
        {
            Debug.Assert(syntaxTrees.Length > 0);
            SourceFile = MakeSourceFileSymbol(syntaxTrees[0]);

            if (syntaxTrees.Length > 1)
            {
                var builder = ImmutableArray.CreateBuilder<SourceFileSymbol>(syntaxTrees.Length - 1);
                for (int i = 1; i < syntaxTrees.Length; i++)
                {
                    builder.Add(MakeSourceFileSymbol(syntaxTrees[i]));
                }

                ReferencedSourceFiles = builder.ToImmutable();
            }
        }

        public SourceFileSymbol SourceFile { get; }
        public ImmutableArray<SourceFileSymbol> ReferencedSourceFiles { get; }

        private SourceFileSymbol MakeSourceFileSymbol(SyntaxTree syntaxTree)
        {
            Debug.Assert(syntaxTree.Root is SourceFileRootSyntax);
            return new SourceFileSymbol(this,
                syntaxTree.SourceText.FilePath,
                (SourceFileRootSyntax)syntaxTree.Root);
        }

        public override SymbolKind Kind => SymbolKind.Module;

        public ChapterSymbol LookupChapter(string name)
        {
            ChapterSymbol chapter = SourceFile.LookupChapter(name);
            if (chapter != null) { return chapter; }

            foreach (SourceFileSymbol file in ReferencedSourceFiles)
            {
                chapter = file.LookupChapter(name);
                if (chapter != null) { break; }
            }

            return chapter;
        }

        public SceneSymbol LookupScene(string name)
        {
            SceneSymbol scene = SourceFile.LookupScene(name);
            if (scene != null) { return scene; }

            foreach (SourceFileSymbol file in ReferencedSourceFiles)
            {
                scene = file.LookupScene(name);
                if (scene != null) { break; }
            }

            return scene;
        }

        public FunctionSymbol LookupFunction(string name)
        {
            FunctionSymbol function = SourceFile.LookupFunction(name);
            if (function != null) { return function; }

            foreach (SourceFileSymbol file in ReferencedSourceFiles)
            {
                function = file.LookupFunction(name);
                if (function != null) { break; }
            }

            return function;
        }

        public override string ToString() => $"Module '{SourceFile.Name}'";
    }

    public sealed class SourceFileSymbol : NamedSymbol
    {
        private readonly Dictionary<string, ChapterSymbol> _chapterMap;
        private readonly Dictionary<string, SceneSymbol> _sceneMap;
        private readonly Dictionary<string, FunctionSymbol> _functionMap;

        internal SourceFileSymbol(ModuleSymbol module, string name, SourceFileRootSyntax syntax)
            : base(name)
        {
            Module = module;
            _chapterMap = new Dictionary<string, ChapterSymbol>();
            _sceneMap = new Dictionary<string, SceneSymbol>();
            _functionMap = new Dictionary<string, FunctionSymbol>();

            var chapters = ImmutableArray.CreateBuilder<ChapterSymbol>();
            var scenes = ImmutableArray.CreateBuilder<SceneSymbol>();
            var functions = ImmutableArray.CreateBuilder<FunctionSymbol>();

            foreach (MemberDeclarationSyntax decl in syntax.MemberDeclarations)
            {
                string declName = decl.Name.Value;
                switch (decl.Kind)
                {
                    case SyntaxNodeKind.ChapterDeclaration:
                        var chapterDecl = (ChapterDeclarationSyntax)decl;
                        var chapter = new ChapterSymbol(this, declName, chapterDecl);
                        _chapterMap.Add(declName, chapter);
                        chapters.Add(chapter);
                        break;
                    case SyntaxNodeKind.FunctionDeclaration:
                        var functionDecl = (FunctionDeclarationSyntax)decl;
                        var function = new FunctionSymbol(this, declName, functionDecl);
                        _functionMap.Add(declName, function);
                        functions.Add(function);
                        break;
                    case SyntaxNodeKind.SceneDeclaration:
                        var sceneDecl = (SceneDeclarationSyntax)decl;
                        var scene = new SceneSymbol(this, declName, sceneDecl);
                        _sceneMap.Add(declName, scene);
                        scenes.Add(scene);
                        break;
                }
            }

            Chapters = chapters.ToImmutable();
            Functions = functions.ToImmutable();
            Scenes = scenes.ToImmutable();
        }

        public override SymbolKind Kind => SymbolKind.SourceFile;

        public ModuleSymbol Module { get; }

        public ImmutableArray<ChapterSymbol> Chapters { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<SceneSymbol> Scenes { get; }

        public ChapterSymbol LookupChapter(string name)
            => LookupMember(_chapterMap, name);

        public SceneSymbol LookupScene(string name)
            => LookupMember(_sceneMap, name);

        public FunctionSymbol LookupFunction(string name)
            => LookupMember(_functionMap, name);

        private T LookupMember<T>(Dictionary<string, T> map, string name) where T : MemberSymbol
            => map.TryGetValue(name, out T symbol) ? symbol : null;

        public override string ToString() => $"SourceFile '{Name}'";
    }

    public abstract class MemberSymbol : NamedSymbol
    {
        protected MemberSymbol(SourceFileSymbol declaringSourceFile, string name,
            MemberDeclarationSyntax declaration) : base(name)
        {
            DeclaringSourceFile = declaringSourceFile;
            Declaration = declaration;
        }

        public SourceFileSymbol DeclaringSourceFile { get; }
        public MemberDeclarationSyntax Declaration { get; }
    }

    public sealed class FunctionSymbol : MemberSymbol
    {
        internal FunctionSymbol(SourceFileSymbol declaringSourceFile, string name,
            FunctionDeclarationSyntax declaration) : base(declaringSourceFile, name, declaration)
        {
            var parameters = ImmutableArray<ParameterSymbol>.Empty;
            int paramCount = declaration.Parameters.Length;
            if (paramCount > 0)
            {
                var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(paramCount);
                foreach (ParameterSyntax paramSyntax in declaration.Parameters)
                {
                    var parameter = new ParameterSymbol(this, paramSyntax.Name);
                    builder.Add(parameter);
                }

                parameters = builder.ToImmutable();
            }

            Parameters = parameters;
            Declaration = declaration;
        }

        public override SymbolKind Kind => SymbolKind.Function;

        public new FunctionDeclarationSyntax Declaration { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }

        public override string ToString() => $"Function '{Name}'";
    }

    public sealed class ParameterSymbol : NamedSymbol
    {
        internal ParameterSymbol(FunctionSymbol containingFunction, string name)
            : base(name)
        {
            ContainingFunction = containingFunction;
        }

        public FunctionSymbol ContainingFunction { get; }
        public override SymbolKind Kind => SymbolKind.Parameter;

        public override string ToString() => $"Parameter '{Name}'";
    }

    public sealed class ChapterSymbol : MemberSymbol
    {
        internal ChapterSymbol(SourceFileSymbol declaringSourceFile, string name,
            ChapterDeclarationSyntax declaration) : base(declaringSourceFile, name, declaration)
        {
            Declaration = declaration;
        }

        public override SymbolKind Kind => SymbolKind.Chapter;
        public new ChapterDeclarationSyntax Declaration { get; }

        public override string ToString() => $"Function '{Name}'";
    }

    public sealed class SceneSymbol : MemberSymbol
    {
        internal SceneSymbol(SourceFileSymbol declaringSourceFile, string name,
            SceneDeclarationSyntax declaration) : base(declaringSourceFile, name, declaration)
        {
            Declaration = declaration;
        }

        public override SymbolKind Kind => SymbolKind.Scene;
        public new SceneDeclarationSyntax Declaration { get; }

        public override string ToString() => $"Scene '{Name}'";
    }
}
