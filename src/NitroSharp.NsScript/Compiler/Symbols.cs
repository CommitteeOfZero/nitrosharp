using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Compiler
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
        Parameter
    }

    public abstract class NamedSymbol : Symbol
    {
        protected NamedSymbol(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public sealed class SourceModuleSymbol : Symbol
    {
        public SourceModuleSymbol(Compilation compilation, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            Debug.Assert(syntaxTrees.Length > 0);
            Compilation = compilation;
            RootSourceFile = MakeSourceFileSymbol(syntaxTrees[0]);

            if (syntaxTrees.Length > 1)
            {
                var builder = ImmutableArray.CreateBuilder<SourceFileSymbol>(syntaxTrees.Length - 1);
                for (int i = 1; i < syntaxTrees.Length; i++)
                {
                    SourceFileSymbol sourceFile = MakeSourceFileSymbol(syntaxTrees[i]);
                    builder.Add(sourceFile);
                }

                ReferencedSourceFiles = builder.ToImmutable();
            }
            else
            {
                ReferencedSourceFiles = ImmutableArray<SourceFileSymbol>.Empty;
            }
        }

        public Compilation Compilation { get; }
        public SourceFileSymbol RootSourceFile { get; }
        public ImmutableArray<SourceFileSymbol> ReferencedSourceFiles { get; }

        private SourceFileSymbol MakeSourceFileSymbol(SyntaxTree syntaxTree)
        {
            Debug.Assert(syntaxTree.Root is SourceFileRootSyntax);
            ResolvedPath filePath = syntaxTree.SourceText.FilePath;
            SourceReferenceResolver sourceRefResolver = Compilation.SourceReferenceResolver;
            string rootDir = sourceRefResolver.RootDirectory;
            string relativePathNoExtension = Path.GetRelativePath(relativeTo: rootDir, filePath.Value);
            if (relativePathNoExtension.EndsWith(".nss", StringComparison.OrdinalIgnoreCase))
            {
                relativePathNoExtension = relativePathNoExtension
                    .Remove(relativePathNoExtension.Length - 4);
            }

            return new SourceFileSymbol(
                this,
                filePath,
                relativePathNoExtension,
                (SourceFileRootSyntax)syntaxTree.Root
            );
        }

        public override SymbolKind Kind => SymbolKind.Module;

        public ChapterSymbol? LookupChapter(string name)
        {
            ChapterSymbol? chapter = RootSourceFile.LookupChapter(name);
            if (chapter != null) { return chapter; }

            foreach (SourceFileSymbol file in ReferencedSourceFiles)
            {
                chapter = file.LookupChapter(name);
                if (chapter != null) { break; }
            }

            return chapter;
        }

        public SceneSymbol? LookupScene(string name)
        {
            SceneSymbol? scene = RootSourceFile.LookupScene(name);
            if (scene != null) { return scene; }

            foreach (SourceFileSymbol file in ReferencedSourceFiles)
            {
                scene = file.LookupScene(name);
                if (scene != null) { break; }
            }

            return scene;
        }

        public FunctionSymbol? LookupFunction(string name)
        {
            FunctionSymbol? function = RootSourceFile.LookupFunction(name);
            if (function != null) { return function; }

            foreach (SourceFileSymbol file in ReferencedSourceFiles)
            {
                function = file.LookupFunction(name);
                if (function != null) { break; }
            }

            return function;
        }

        public override string ToString() => $"Module '{RootSourceFile.Name}'";
    }

    public sealed class SourceFileSymbol : NamedSymbol
    {
        private readonly Dictionary<string, ChapterSymbol> _chapterMap;
        private readonly Dictionary<string, SceneSymbol> _sceneMap;
        private readonly Dictionary<string, FunctionSymbol> _functionMap;

        internal SourceFileSymbol(
            SourceModuleSymbol module,
            ResolvedPath filePath,
            string relativePathNoExtension,
            SourceFileRootSyntax syntax)
            : base(relativePathNoExtension)
        {
            Module = module;
            FilePath = filePath;

            (int chapterCount, int sceneCount, int functionCount) =
                ((int)syntax.ChapterCount,
                (int)syntax.SceneCount,
                (int)syntax.FunctionCount);

            _chapterMap = new Dictionary<string, ChapterSymbol>(chapterCount);
            _sceneMap = new Dictionary<string, SceneSymbol>(sceneCount);
            _functionMap = new Dictionary<string, FunctionSymbol>(functionCount);

            var chapters = ImmutableArray.CreateBuilder<ChapterSymbol>(chapterCount);
            var scenes = ImmutableArray.CreateBuilder<SceneSymbol>(sceneCount);
            var functions = ImmutableArray.CreateBuilder<FunctionSymbol>(functionCount);

            foreach (SubroutineDeclarationSyntax decl in syntax.SubroutineDeclarations)
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
            SubroutineCount = (uint)(chapters.Count + functions.Count + scenes.Count);
        }

        public override SymbolKind Kind => SymbolKind.SourceFile;

        public SourceModuleSymbol Module { get; }
        public ResolvedPath FilePath { get; }

        public ImmutableArray<ChapterSymbol> Chapters { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<SceneSymbol> Scenes { get; }

        public uint SubroutineCount { get; }

        public ChapterSymbol? LookupChapter(string name)
            => LookupSubroutine(_chapterMap, name);

        public SceneSymbol? LookupScene(string name)
            => LookupSubroutine(_sceneMap, name);

        public FunctionSymbol? LookupFunction(string name)
            => LookupSubroutine(_functionMap, name);

        private T? LookupSubroutine<T>(Dictionary<string, T> map, string name) where T : SubroutineSymbol
            => map.TryGetValue(name, out T? symbol) ? symbol : null;

        public override string ToString() => $"SourceFile '{Name}'";
    }

    public abstract class SubroutineSymbol : NamedSymbol, IEquatable<SubroutineSymbol>
    {
        protected SubroutineSymbol(
            SourceFileSymbol declaringSourceFile,
            string name,
            SubroutineDeclarationSyntax declaration) : base(name)
        {
            DeclaringSourceFile = declaringSourceFile;
            Declaration = declaration;
        }

        public SourceFileSymbol DeclaringSourceFile { get; }
        public SubroutineDeclarationSyntax Declaration { get; }

        public virtual ParameterSymbol? LookupParameter(string name) => null;

        public bool Equals(SubroutineSymbol? other)
            => ReferenceEquals(Declaration, other?.Declaration);

        public override bool Equals(object? obj)
            => obj is SubroutineSymbol other && ReferenceEquals(Declaration, other.Declaration);

        public override int GetHashCode() => Declaration.GetHashCode();
    }

    public sealed class FunctionSymbol : SubroutineSymbol, IEquatable<FunctionSymbol>
    {
        private readonly Dictionary<string, ParameterSymbol>? _parameterMap;

        internal FunctionSymbol(
            SourceFileSymbol declaringSourceFile,
            string name,
            FunctionDeclarationSyntax declaration)
            : base(declaringSourceFile, name, declaration)
        {
            var parameters = ImmutableArray<ParameterSymbol>.Empty;
            int paramCount = declaration.Parameters.Length;
            if (paramCount > 0)
            {
                var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(paramCount);
                _parameterMap = new Dictionary<string, ParameterSymbol>();
                foreach (ParameterSyntax paramSyntax in declaration.Parameters)
                {
                    var parameter = new ParameterSymbol(this, paramSyntax.Name);
                    builder.Add(parameter);
                    _parameterMap.Add(parameter.Name, parameter);
                }

                parameters = builder.ToImmutable();
            }

            Parameters = parameters;
            Declaration = declaration;
        }

        public override SymbolKind Kind => SymbolKind.Function;

        public new FunctionDeclarationSyntax Declaration { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }

        public override ParameterSymbol? LookupParameter(string name)
        {
            if (_parameterMap == null) { return null; }
            return _parameterMap.TryGetValue(name, out ParameterSymbol? symbol) ? symbol : null;
        }

        public bool Equals(FunctionSymbol? other) => ReferenceEquals(Declaration, other?.Declaration);
        public override int GetHashCode() => Declaration.GetHashCode();
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

    public sealed class ChapterSymbol : SubroutineSymbol, IEquatable<ChapterSymbol>
    {
        internal ChapterSymbol(
            SourceFileSymbol declaringSourceFile,
            string name,
            ChapterDeclarationSyntax declaration)
            : base(declaringSourceFile, name, declaration)
        {
            Declaration = declaration;
        }

        public override SymbolKind Kind => SymbolKind.Chapter;
        public new ChapterDeclarationSyntax Declaration { get; }

        public bool Equals(ChapterSymbol? other) => ReferenceEquals(Declaration, other?.Declaration);
        public override int GetHashCode() => Declaration.GetHashCode();
        public override string ToString() => $"Chapter '{Name}'";
    }

    public sealed class SceneSymbol : SubroutineSymbol, IEquatable<SceneSymbol>
    {
        internal SceneSymbol(
            SourceFileSymbol declaringSourceFile,
            string name,
            SceneDeclarationSyntax declaration)
            : base(declaringSourceFile, name, declaration)
        {
            Declaration = declaration;
        }

        public override SymbolKind Kind => SymbolKind.Scene;
        public new SceneDeclarationSyntax Declaration { get; }

        public bool Equals(SceneSymbol? other) => ReferenceEquals(Declaration, other?.Declaration);
        public override int GetHashCode() => Declaration.GetHashCode();
        public override string ToString() => $"Scene '{Name}'";
    }
}
