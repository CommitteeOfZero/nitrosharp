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
        private readonly string _globalsFileName;
        private readonly Encoding? _sourceTextEncoding;
        private readonly Dictionary<ResolvedPath, SyntaxTree> _syntaxTrees = new();
        private readonly Dictionary<SyntaxTree, SourceModuleSymbol> _sourceModuleSymbols = new();

        private readonly Dictionary<ResolvedPath, NsxModuleBuilder> _nsxModuleBuilders = new();
        private readonly TokenMap<string> _variables = new(4096);
        private readonly TokenMap<string> _flags = new(256);
        private readonly List<string> _systemVariables = new();
        private readonly List<string> _systemFlags = new();

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
            SourceReferenceResolver = sourceReferenceResolver;
            OutputDirectory = outputDirectory;
            _globalsFileName = globalsFileName;
            _sourceTextEncoding = sourceTextEncoding;
        }

        public SourceReferenceResolver SourceReferenceResolver { get; }
        public string OutputDirectory { get; }

        public void Emit(ReadOnlySpan<SourceModuleSymbol> roots)
        {
            var compiledSourceFiles = new HashSet<ResolvedPath>();
            foreach (SourceModuleSymbol module in roots)
            {
                EmitCore(module.RootSourceFile, compiledSourceFiles);
            }

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

            string globalsFileName = Path.Combine(OutputDirectory, _globalsFileName);
            using (FileStream file = File.Create(globalsFileName))
            {
                using var nameHeapBuffer = PooledBuffer<byte>.Allocate(32 * 1024);
                var nameWriter = new BufferWriter(nameHeapBuffer);
                (BufferSlice<byte> varOffsets, BufferSlice<byte> sysVarList) = writeGlobals(
                    _variables,
                    _systemVariables,
                    ref nameWriter
                );
                (BufferSlice<byte> flagOffsets, BufferSlice<byte> sysFlagList) = writeGlobals(
                    _flags,
                    _systemFlags,
                    ref nameWriter
                );

                static (BufferSlice<byte> offsets, BufferSlice<byte> sysList) writeGlobals(
                    TokenMap<string> globals,
                    List<string> systemGlobals,
                    ref BufferWriter nameWriter)
                {
                    uint offsetTableSize = globals.Count * 4 + 2;
                    var offsetTableBuffer = PooledBuffer<byte>.Allocate(offsetTableSize);
                    var offsetWriter = new BufferWriter(offsetTableBuffer);
                    offsetWriter.WriteUInt16LE((ushort)globals.Count);

                    uint sysListSize = (uint)(systemGlobals.Count * 2 + 4);
                    var sysListBuffer = PooledBuffer<byte>.Allocate(sysListSize);
                    var sysListWriter = new BufferWriter(sysListBuffer);
                    sysListWriter.WriteUInt16LE((ushort)systemGlobals.Count);

                    ReadOnlySpan<string> globalsSpan = globals.AsSpan();
                    for (int i = 0; i < globalsSpan.Length; i++)
                    {
                        string name = globalsSpan[i];
                        offsetWriter.WriteInt32LE(nameWriter.Position);
                        if (name.StartsWith("SYSTEM"))
                        {
                            sysListWriter.WriteUInt16LE((ushort)i);
                        }
                        nameWriter.WriteLengthPrefixedUtf8String(name);
                    }

                    var offsets = new BufferSlice<byte>(offsetTableBuffer, (uint)offsetWriter.Position);
                    var sysList = new BufferSlice<byte>(sysListBuffer, (uint)sysListWriter.Position);
                    return (offsets, sysList);
                }

                file.Write(varOffsets.AsSpan());
                file.Write(sysVarList.AsSpan());
                file.Write(flagOffsets.AsSpan());
                file.Write(sysFlagList.AsSpan());
                file.Write(nameWriter.Written);
                varOffsets.Buffer.Dispose();
                sysVarList.Buffer.Dispose();
                flagOffsets.Buffer.Dispose();
                sysFlagList.Buffer.Dispose();
            }
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
            ResolvedPath resolvedPath = SourceReferenceResolver.ResolvePath(filePath);
            if (_syntaxTrees.TryGetValue(resolvedPath, out SyntaxTree? syntaxTree))
            {
                return syntaxTree;
            }

            SourceText sourceText = SourceReferenceResolver.ReadText(resolvedPath, _sourceTextEncoding);
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

        internal ushort GetVariableToken(string name)
        {
            if (!_variables.TryGetToken(name, out ushort token))
            {
                token = _variables.AddToken(name);
                if (name.StartsWith("SYSTEM"))
                {
                    _systemVariables.Add(name);
                }
            }

            return token;
        }

        internal ushort GetFlagToken(string name)
        {
            if (!_flags.TryGetToken(name, out ushort token))
            {
                token = _flags.AddToken(name);
                if (name.StartsWith("SYSTEM"))
                {
                    _systemFlags.Add(name);
                }
            }

            return token;
        }

        internal bool TryGetVariableToken(string variableName, out ushort token)
            => _variables.TryGetToken(variableName, out token);
    }
}
