using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Compiler
{
    public sealed class Compilation
    {
        private readonly SourceReferenceResolver _sourceReferenceResolver;
        private readonly string _outputDirectory;
        private readonly string _globalsFileName;
        private readonly Encoding? _sourceTextEncoding;
        private readonly Dictionary<ResolvedPath, SyntaxTree> _syntaxTrees;
        private readonly Dictionary<SyntaxTree, SourceModuleSymbol> _sourceModuleSymbols;

        private readonly Dictionary<ResolvedPath, NsxModuleBuilder> _nsxModuleBuilders;
        private readonly TokenMap<string> _globals = new TokenMap<string>(4096);
        private readonly List<string> _systemVariables = new List<string>();

        internal readonly HashSet<string> BoundVariables = new HashSet<string>();

        public Compilation(string rootSourceDirectory,
                           string outputDirectory,
                           string globalsFileName,
                           Encoding? sourceTextEncoding = null)
            : this(new DefaultSourceReferenceResolver(rootSourceDirectory),
                   outputDirectory,
                   globalsFileName,
                   sourceTextEncoding)
        {
        }

        public Compilation(SourceReferenceResolver sourceReferenceResolver,
                           string outputDirectory,
                           string globalsFileName,
                           Encoding? sourceTextEncoding = null)
        {
            _sourceReferenceResolver = sourceReferenceResolver;
            _outputDirectory = outputDirectory;
            _globalsFileName = globalsFileName;
            _sourceTextEncoding = sourceTextEncoding;
            _syntaxTrees = new Dictionary<ResolvedPath, SyntaxTree>();
            _sourceModuleSymbols = new Dictionary<SyntaxTree, SourceModuleSymbol>();
            _nsxModuleBuilders = new Dictionary<ResolvedPath, NsxModuleBuilder>();
        }

        public SourceReferenceResolver SourceReferenceResolver => _sourceReferenceResolver;
        public string OutputDirectory => _outputDirectory;

        public void Emit(SourceModuleSymbol mainModule)
        {
            var compiledSourceFiles = new HashSet<ResolvedPath>();
            EmitCore(mainModule.RootSourceFile, compiledSourceFiles);

            int filesCompiled;
            do
            {
                filesCompiled = 0;
                KeyValuePair<ResolvedPath, NsxModuleBuilder>[] nsxBuilders = _nsxModuleBuilders.ToArray();
                foreach ((ResolvedPath path, NsxModuleBuilder moduleBuilder) in nsxBuilders)
                {
                    if (!compiledSourceFiles.Contains(path))
                    {
                        NsxModuleAssembler.WriteModule(moduleBuilder);
                        compiledSourceFiles.Add(path);
                        filesCompiled++;
                    }
                }
            } while (filesCompiled > 0);

            string globalsFileName = Path.Combine(_outputDirectory, _globalsFileName);
            using (FileStream file = File.Create(globalsFileName))
            {
                uint offsetTableSize = _globals.Count * 4 + 2;
                using var offsetTableBuffer = PooledBuffer<byte>.Allocate(offsetTableSize);
                var offsetWriter = new BufferWriter(offsetTableBuffer);
                offsetWriter.WriteUInt16LE((ushort)_globals.Count);

                uint sysVarListSize = (uint)(_systemVariables.Count * 2 + 4);
                using var sysVarList = PooledBuffer<byte>.Allocate(sysVarListSize);
                var sysVarListWriter = new BufferWriter(sysVarList);
                sysVarListWriter.WriteUInt16LE((ushort)_systemVariables.Count);

                using var nameHeapBuffer = PooledBuffer<byte>.Allocate(32 * 1024);
                var nameWriter = new BufferWriter(nameHeapBuffer);
                ReadOnlySpan<string> variables = _globals.AsSpan();
                for (int i = 0; i < variables.Length; i++)
                {
                    string var = variables[i];
                    offsetWriter.WriteInt32LE(nameWriter.Position);
                    if (var.StartsWith("SYSTEM"))
                    {
                        sysVarListWriter.WriteUInt16LE((ushort)i);
                    }
                    nameWriter.WriteLengthPrefixedUtf8String(var);
                }

                file.Write(offsetWriter.Written);
                file.Write(sysVarListWriter.Written);
                file.Write(nameWriter.Written);
            }

            var unboundVars = _globals.AsSpan().ToArray()
                .Except(BoundVariables).ToArray();
        }

        private void EmitCore(SourceFileSymbol sourceFile, HashSet<ResolvedPath> compiledFiles)
        {
            NsxModuleBuilder nsxBuilder = GetNsxModuleBuilder(sourceFile);
            NsxModuleAssembler.WriteModule(nsxBuilder);
            compiledFiles.Add(sourceFile.FilePath);
        }

        /// <exception cref="FileNotFoundException" />
        public SyntaxTree GetSyntaxTree(string filePath)
        {
            ResolvedPath resolvedPath = _sourceReferenceResolver.ResolvePath(filePath);
            if (_syntaxTrees.TryGetValue(resolvedPath, out SyntaxTree? syntaxTree))
            {
                return syntaxTree;
            }

            SourceText sourceText = _sourceReferenceResolver.ReadText(resolvedPath, _sourceTextEncoding);
            syntaxTree = Parsing.ParseText(sourceText);
            _syntaxTrees[resolvedPath] = syntaxTree;
            return syntaxTree;
        }

        /// <exception cref="FileNotFoundException" />
        public SourceModuleSymbol GetSourceModule(string filePath)
        {
            SyntaxTree tree = GetSyntaxTree(filePath);
            return GetModuleSymbol(tree);
        }

        private SourceModuleSymbol GetModuleSymbol(SyntaxTree syntaxTree)
        {
            if (_sourceModuleSymbols.TryGetValue(syntaxTree, out SourceModuleSymbol? symbol))
            {
                return symbol;
            }

            symbol = CreateModuleSymbol(syntaxTree);
            _sourceModuleSymbols[syntaxTree] = symbol;
            return symbol;
        }

        private SourceModuleSymbol CreateModuleSymbol(SyntaxTree syntaxTree)
        {
            var root = syntaxTree.Root as SourceFileRootSyntax;
            if (root.FileReferences.Length == 0)
            {
                return new SourceModuleSymbol(this, ImmutableArray.Create(syntaxTree));
            }

            var builder = ImmutableArray.CreateBuilder<SyntaxTree>(4);
            builder.Add(syntaxTree);
            CollectReferences(syntaxTree, builder);
            return new SourceModuleSymbol(this, builder.ToImmutable());
        }

        private void CollectReferences(SyntaxTree syntaxTree, ImmutableArray<SyntaxTree>.Builder builder)
        {
            var root = (SourceFileRootSyntax)syntaxTree.Root;
            foreach (Spanned<string> include in root.FileReferences)
            {
                try
                {
                    SyntaxTree tree = GetSyntaxTree(include.Value);
                    builder.Add(tree);
                    CollectReferences(tree, builder);
                }
                catch (FileNotFoundException)
                {
                    // TODO: report error
                }
            }
        }

        internal NsxModuleBuilder GetNsxModuleBuilder(SourceFileSymbol sourceFile)
        {
            if (!_nsxModuleBuilders.TryGetValue(sourceFile.FilePath, out NsxModuleBuilder? moduleBuilder))
            {
                moduleBuilder = new NsxModuleBuilder(this, sourceFile);
                _nsxModuleBuilders.Add(sourceFile.FilePath, moduleBuilder);
            }

            return moduleBuilder;
        }

        internal ushort GetGlobalVarToken(string variableName)
        {
            if (!_globals.TryGetToken(variableName, out ushort token))
            {
                token = _globals.AddToken(variableName);
                if (variableName.StartsWith("SYSTEM"))
                {
                    _systemVariables.Add(variableName);
                }
            }

            return token;
        }
    }
}
