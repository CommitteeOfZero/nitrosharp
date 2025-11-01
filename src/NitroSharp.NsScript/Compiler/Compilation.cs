using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Compiler;

public class Compilation
{
    private readonly Encoding? _sourceTextEncoding;
    protected readonly Dictionary<ResolvedPath, SyntaxTree> _syntaxTrees;
    private readonly Dictionary<SyntaxTree, SourceModuleSymbol> _sourceModuleSymbols;
    private readonly DiagnosticBuilder _referenceResolveDiagnostics = new();

    public Compilation(string rootSourceDirectory, Encoding? sourceTextEncoding = null)
        : this(new DefaultSourceReferenceResolver(rootSourceDirectory), sourceTextEncoding)
    {
    }

    public Compilation(SourceReferenceResolver sourceReferenceResolver, Encoding? sourceTextEncoding = null)
        : this(sourceReferenceResolver, sourceTextEncoding, new(), new())
    {
    }

    protected Compilation(Compilation source)
        : this(source.SourceReferenceResolver, source._sourceTextEncoding,
            source._syntaxTrees, source._sourceModuleSymbols)
    {
    }

    private Compilation(
        SourceReferenceResolver sourceReferenceResolver,
        Encoding? sourceTextEncoding,
        Dictionary<ResolvedPath, SyntaxTree> syntaxTrees,
        Dictionary<SyntaxTree, SourceModuleSymbol> sourceModuleSymbols)
    {
        SourceReferenceResolver = sourceReferenceResolver;
        _sourceTextEncoding = sourceTextEncoding;
        _syntaxTrees = syntaxTrees;
        _sourceModuleSymbols = sourceModuleSymbols;
    }

    public SourceReferenceResolver SourceReferenceResolver { get; }
    public virtual DiagnosticCollection Diagnostics => DiagnosticCollection.Empty;

    [MustUseReturnValue]
    public virtual EmittedCompilation Emit(
        ReadOnlySpan<SourceModuleSymbol> roots,
        string outputDirectory,
        string globalsFileName)
    {
        FileStream globalsStream = File.Create(Path.Combine(outputDirectory, globalsFileName));
        return Emit(roots, createOutputStream, globalsStream);

        Stream createOutputStream(NsxModuleBuilder nsxBuilder)
        {
            string? subDir = Path.GetDirectoryName(nsxBuilder.SourceFile.Name);
            if (!string.IsNullOrEmpty(subDir))
            {
                subDir = Path.Combine(outputDirectory, subDir);
                Directory.CreateDirectory(subDir);
            }

            string nameDotNsx = Path.ChangeExtension(nsxBuilder.SourceFile.Name, "nsx");
            string path = Path.Combine(outputDirectory, nameDotNsx);
            return File.Create(path);
        }
    }

    [MustUseReturnValue]
    public virtual EmittedCompilation EmitDiagnostics(ReadOnlySpan<SourceModuleSymbol> roots)
    {
        return Emit(roots, _ => Stream.Null, Stream.Null);
    }

    [MustUseReturnValue]
    private EmittedCompilation Emit(
        ReadOnlySpan<SourceModuleSymbol> roots,
        Func<NsxModuleBuilder, Stream> outputStreamFactory,
        Stream globalsOutputStream)
    {
        var context = new EmitContext(this);
        var compiledSourceFiles = new HashSet<ResolvedPath>();
        foreach (SourceModuleSymbol sourceModule in roots)
        {
            NsxModuleBuilder nsxBuilder = context.GetNsxModuleBuilder(sourceModule.RootSourceFile);
            emitCore(nsxBuilder);
        }

        int filesCompiledThisIter;
        do
        {
            filesCompiledThisIter = 0;
            KeyValuePair<ResolvedPath, NsxModuleBuilder>[] nsxBuilders = context.NsxModuleBuilders.ToArray();
            foreach ((ResolvedPath path, NsxModuleBuilder nsxBuilder) in nsxBuilders)
            {
                if (!compiledSourceFiles.Contains(path))
                {
                    emitCore(nsxBuilder);
                    filesCompiledThisIter++;
                }
            }
        } while (filesCompiledThisIter > 0);

        emitGlobals();
        return new EmittedCompilation(this, mergeDiagnostics());

        void emitCore(NsxModuleBuilder nsxBuilder)
        {
            using Stream outputStream = outputStreamFactory(nsxBuilder);
            nsxBuilder.Emit(outputStream);
            compiledSourceFiles.Add(nsxBuilder.SourceFile.FilePath);
        }

        void emitGlobals()
        {
            using var nameHeapBuffer = PooledBuffer<byte>.Allocate(32 * 1024);
            var nameWriter = new BufferWriter(nameHeapBuffer);
            (BufferSlice<byte> varOffsets, BufferSlice<byte> sysVarList) = appendGlobals(
                context.Variables,
                context.SystemVariables,
                ref nameWriter
            );
            (BufferSlice<byte> flagOffsets, BufferSlice<byte> sysFlagList) = appendGlobals(
                context.Flags,
                context.SystemFlags,
                ref nameWriter
            );

            using (globalsOutputStream)
            {
                globalsOutputStream.Write(varOffsets.AsSpan());
                globalsOutputStream.Write(sysVarList.AsSpan());
                globalsOutputStream.Write(flagOffsets.AsSpan());
                globalsOutputStream.Write(sysFlagList.AsSpan());
                globalsOutputStream.Write(nameWriter.Written);
            }

            varOffsets.Buffer.Dispose();
            sysVarList.Buffer.Dispose();
            flagOffsets.Buffer.Dispose();
            sysFlagList.Buffer.Dispose();
        }

        static (BufferSlice<byte> offsets, BufferSlice<byte> sysList) appendGlobals(
            TokenMap<string> globals,
            List<string> systemGlobals,
            ref BufferWriter nameWriter)
        {
            int offsetTableSize = globals.Count * 4 + 2;
            var offsetTableBuffer = PooledBuffer<byte>.Allocate(offsetTableSize);
            var offsetWriter = new BufferWriter(offsetTableBuffer);
            offsetWriter.WriteUInt16LE((ushort)globals.Count);

            int sysListSize = systemGlobals.Count * 2 + 4;
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

            var offsets = new BufferSlice<byte>(offsetTableBuffer, offsetWriter.Position);
            var sysList = new BufferSlice<byte>(sysListBuffer, sysListWriter.Position);
            return (offsets, sysList);
        }

        DiagnosticCollection mergeDiagnostics()
        {
            DiagnosticBuilder allDiagnostics = context.DiagnosticBuilder;
            allDiagnostics.MergeFrom(_referenceResolveDiagnostics);
            foreach (SyntaxTree syntaxTree in _syntaxTrees.Values)
            {
                allDiagnostics.MergeFrom(syntaxTree.DiagnosticBuilder);
            }

            return allDiagnostics.ToImmutable();
        }
    }

    /// <exception cref="FileNotFoundException"></exception>
    public SyntaxTree GetSyntaxTree(string relativePath)
    {
        ResolvedPath resolvedPath = SourceReferenceResolver.ResolvePath(relativePath);
        return GetSyntaxTree(resolvedPath);
    }

    protected virtual SyntaxTree GetSyntaxTree(ResolvedPath resolvedPath)
    {
        if (_syntaxTrees.TryGetValue(resolvedPath, out SyntaxTree? syntaxTree))
        {
            return syntaxTree;
        }

        SourceText sourceText = SourceReferenceResolver.ReadText(resolvedPath, _sourceTextEncoding);
        syntaxTree = SyntaxTree.ParseText(sourceText);
        _syntaxTrees[resolvedPath] = syntaxTree;
        return syntaxTree;
    }

    /// <exception cref="FileNotFoundException" />
    public SourceModuleSymbol GetSourceModule(string relativePath)
    {
        SyntaxTree tree = GetSyntaxTree(relativePath);
        return GetModuleSymbol(tree);
    }

    public SourceModuleSymbol? TryGetSourceModule(string relativePath)
    {
        if (SourceReferenceResolver.TryResolvePath(relativePath) is not { } resolvedPath)
        {
            return null;
        }

        SyntaxTree tree = GetSyntaxTree(resolvedPath);
        return GetModuleSymbol(tree);
    }

    private SourceModuleSymbol GetModuleSymbol(SyntaxTree syntaxTree)
    {
        if (!_sourceModuleSymbols.TryGetValue(syntaxTree, out SourceModuleSymbol? symbol))
        {
            symbol = CreateModuleSymbol(syntaxTree);
            _sourceModuleSymbols[syntaxTree] = symbol;
        }

        return symbol;
    }

    private SourceModuleSymbol CreateModuleSymbol(SyntaxTree syntaxTree)
    {
        if (syntaxTree.Root is SourceFileRoot { FileReferences.Length: 0 })
        {
            return new SourceModuleSymbol(this, [syntaxTree]);
        }

        var trees = ImmutableArray.CreateBuilder<SyntaxTree>(4);
        trees.Add(syntaxTree);
        CollectReferences(syntaxTree, trees);
        return new SourceModuleSymbol(this, trees.ToImmutable());
    }

    private void CollectReferences(SyntaxTree syntaxTree, ImmutableArray<SyntaxTree>.Builder results)
    {
        var root = (SourceFileRoot)syntaxTree.Root;
        foreach (Spanned<string> include in root.FileReferences)
        {
            try
            {
                SyntaxTree tree = GetSyntaxTree(include.Value);
                results.Add(tree);
                CollectReferences(tree, results);
            }
            catch (FileNotFoundException)
            {
                var location = new SourceLocation(syntaxTree.SourceText, include.Span);
                _referenceResolveDiagnostics
                    .Add(Diagnostic.Create(location, DiagnosticId.ExternalModuleNotFound, include.Value));
            }
        }
    }
}

public sealed class EmittedCompilation : Compilation
{
    public EmittedCompilation(Compilation source, DiagnosticCollection diagnostics) : base(source)
    {
        Diagnostics = diagnostics;
    }

    public override DiagnosticCollection Diagnostics { get; }

    protected override SyntaxTree GetSyntaxTree(ResolvedPath resolvedPath)
        => _syntaxTrees[resolvedPath];

    public override EmittedCompilation Emit(
        ReadOnlySpan<SourceModuleSymbol> roots, string outputDirectory, string globalsFileName)
    {
        throw new InvalidOperationException();
    }

    public override EmittedCompilation EmitDiagnostics(ReadOnlySpan<SourceModuleSymbol> roots)
    {
        throw new InvalidOperationException();
    }
}
