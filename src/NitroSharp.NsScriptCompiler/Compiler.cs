using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NitroSharp.NsScript.CodeGen;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Text;
using NitroSharp.NsScript.Utilities;
using NitroSharp.Utilities;

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
                foreach (KeyValuePair<ResolvedPath, NsxModuleBuilder> kvp in nsxBuilders)
                {
                    if (!compiledSourceFiles.Contains(kvp.Key))
                    {
                        NsxModuleBuilder moduleBuilder = kvp.Value;
                        NsxModuleAssembler.WriteModule(moduleBuilder);
                        compiledSourceFiles.Add(kvp.Key);
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

    internal static class NsxModuleAssembler
    {
        public static void WriteModule(NsxModuleBuilder builder)
        {
            SourceFileSymbol sourceFile = builder.SourceFile;
            Compilation compilation = builder.Compilation;
            ReadOnlySpan<SubroutineSymbol> subroutines = builder.Subroutines;

            // Compile subroutines
            using var codeBuffer = PooledBuffer<byte>.Allocate(64 * 1024);
            var codeWriter = new BufferWriter(codeBuffer);
            var subroutineOffsets = new List<int>(subroutines.Length);
            CompileSubroutines(
                builder, sourceFile.Chapters.As<SubroutineSymbol>(), ref codeWriter, subroutineOffsets);
            CompileSubroutines(
                builder, sourceFile.Scenes.As<SubroutineSymbol>(), ref codeWriter, subroutineOffsets);
            CompileSubroutines(
                builder, sourceFile.Functions.As<SubroutineSymbol>(), ref codeWriter, subroutineOffsets);
            codeWriter.WriteBytes(NsxConstants.TableEndMarker);

            ReadOnlySpan<string> stringHeap = builder.StringHeap;
            int subTableOffset = NsxConstants.NsxHeaderSize;
            int subTableSize = NsxConstants.TableHeaderSize + 6 + subroutines.Length * sizeof(int);
            int stringTableSize = NsxConstants.TableHeaderSize + 6 + stringHeap.Length * sizeof(int);

            // Build the runtime information table (RTI)
            int rtiTableOffset = NsxConstants.NsxHeaderSize + subTableSize;
            using var rtiBuffer = PooledBuffer<byte>.Allocate(8 * 1024);
            var rtiWriter = new BufferWriter(rtiBuffer);
            uint rtiOffsetBlockSize = sourceFile.SubroutineCount * sizeof(ushort);
            using var rtiEntryOffsets = PooledBuffer<byte>.Allocate(rtiOffsetBlockSize);
            var rtiOffsetWriter = new BufferWriter(rtiEntryOffsets);

            WriteRuntimeInformation(
                sourceFile.Chapters.As<SubroutineSymbol>(), ref rtiWriter, ref rtiOffsetWriter);
            WriteRuntimeInformation(
                sourceFile.Scenes.As<SubroutineSymbol>(), ref rtiWriter, ref rtiOffsetWriter);
            WriteRuntimeInformation(
                sourceFile.Functions.As<SubroutineSymbol>(), ref rtiWriter, ref rtiOffsetWriter);
            rtiWriter.WriteBytes(NsxConstants.TableEndMarker);

            int rtiSize = NsxConstants.TableHeaderSize + rtiOffsetWriter.Position + rtiWriter.Position;
            Span<byte> rtiHeader = stackalloc byte[NsxConstants.TableHeaderSize];
            var rtiHeaderWriter = new BufferWriter(rtiHeader);
            rtiHeaderWriter.WriteBytes(NsxConstants.RtiTableMarker);
            rtiHeaderWriter.WriteUInt16LE((ushort)(rtiSize - NsxConstants.TableHeaderSize));

            int impTableOffset = rtiTableOffset + rtiSize;

            // Build the import table
            ReadOnlySpan<SourceFileSymbol> imports = builder.Imports;
            using var importTable = PooledBuffer<byte>.Allocate(2048);
            var impTableWriter = new BufferWriter(importTable);
            impTableWriter.WriteUInt16LE((ushort)imports.Length);
            for (int i = 0; i < imports.Length; i++)
            {
                impTableWriter.WriteLengthPrefixedUtf8String(imports[i].Name);
            }
            impTableWriter.WriteBytes(NsxConstants.TableEndMarker);

            Span<byte> impHeader = stackalloc byte[NsxConstants.TableHeaderSize];
            var impHeaderWriter = new BufferWriter(impHeader);
            impHeaderWriter.WriteBytes(NsxConstants.ImportTableMarker);
            impHeaderWriter.WriteUInt16LE((ushort)impTableWriter.Position);

            int impTableSize = impHeaderWriter.Position + impTableWriter.Position;
            int stringTableOffset = impTableOffset + impTableSize;
            int codeStart = stringTableOffset  + stringTableSize;

            // Build the subroutine offset table (SUB)
            using var subTable = PooledBuffer<byte>.Allocate((uint)subTableSize);
            var subWriter = new BufferWriter(subTable);
            subWriter.WriteUInt16LE((ushort)subroutines.Length);
            for (int i = 0; i < subroutines.Length; i++)
            {
                subWriter.WriteInt32LE(subroutineOffsets[i] + codeStart);
            }
            subWriter.WriteBytes(NsxConstants.TableEndMarker);

            Span<byte> subHeader = stackalloc byte[NsxConstants.TableHeaderSize];
            var subHeaderWriter = new BufferWriter(subHeader);
            subHeaderWriter.WriteBytes(NsxConstants.SubTableMarker);
            subHeaderWriter.WriteUInt16LE((ushort)subWriter.Position);

            // Encode the strings and build the offset table (STR)
            int stringHeapStart = codeStart + codeWriter.Position;
            using var stringHeapBuffer = PooledBuffer<byte>.Allocate(64 * 1024);
            using var stringOffsetTable = PooledBuffer<byte>.Allocate((uint)stringTableSize);
            var strTableWriter = new BufferWriter(stringOffsetTable);
            strTableWriter.WriteUInt16LE((ushort)stringHeap.Length);

            var stringWriter = new BufferWriter(stringHeapBuffer);
            foreach (string s in stringHeap)
            {
                strTableWriter.WriteInt32LE(stringHeapStart + stringWriter.Position);
                stringWriter.WriteLengthPrefixedUtf8String(s);
            }
            strTableWriter.WriteBytes(NsxConstants.TableEndMarker);

            Span<byte> strTableHeader = stackalloc byte[NsxConstants.TableHeaderSize];
            var strTableHeaderWriter = new BufferWriter(strTableHeader);
            strTableHeaderWriter.WriteBytes(NsxConstants.StringTableMarker);
            strTableHeaderWriter.WriteUInt16LE((ushort)stringTableSize);

            // Build the NSX header
            using var headerBuffer = PooledBuffer<byte>.Allocate(NsxConstants.NsxHeaderSize);
            var headerWriter = new BufferWriter(headerBuffer);
            long modificationTime = compilation.SourceReferenceResolver
                .GetModificationTimestamp(sourceFile.FilePath);
            headerWriter.WriteBytes(NsxConstants.NsxMagic);
            headerWriter.WriteInt64LE(modificationTime);
            headerWriter.WriteInt32LE(subTableOffset);
            headerWriter.WriteInt32LE(rtiTableOffset);
            headerWriter.WriteInt32LE(impTableOffset);
            headerWriter.WriteInt32LE(stringTableOffset);
            headerWriter.WriteInt32LE(codeStart);

            // --- Write everything to the stream ---
            string outDir = compilation.OutputDirectory;
            string? subDir = Path.GetDirectoryName(sourceFile.Name);
            if (!string.IsNullOrEmpty(subDir))
            {
                subDir = Path.Combine(outDir, subDir);
                Directory.CreateDirectory(subDir);
            }

            string path = Path.Combine(outDir, Path.ChangeExtension(sourceFile.Name, "nsx"));
            using FileStream fileStream = File.Create(path);
            fileStream.Write(headerWriter.Written);

            fileStream.Write(subHeaderWriter.Written);
            fileStream.Write(subWriter.Written);

            fileStream.Write(rtiHeader);
            fileStream.Write(rtiOffsetWriter.Written);
            fileStream.Write(rtiWriter.Written);

            fileStream.Write(impHeaderWriter.Written);
            fileStream.Write(impTableWriter.Written);

            fileStream.Write(strTableHeaderWriter.Written);
            fileStream.Write(strTableWriter.Written);

            fileStream.Write(codeWriter.Written);
            fileStream.Write(stringWriter.Written);
        }

        private static void CompileSubroutines(
            NsxModuleBuilder moduleBuilder, ImmutableArray<SubroutineSymbol> subroutines,
            ref BufferWriter writer, List<int> subroutineOffsets)
        {
            if (subroutines.Length == 0) { return; }
            var dialogueBlockOffsets = new List<int>();
            foreach (SubroutineSymbol subroutine in subroutines)
            {
                subroutineOffsets.Add(writer.Position);

                SubroutineDeclarationSyntax decl = subroutine.Declaration;
                int dialogueBlockCount = decl.DialogueBlocks.Length;
                int start = writer.Position;
                int offsetBlockSize = sizeof(ushort) + dialogueBlockCount * sizeof(ushort);
                writer.Position += 2 + offsetBlockSize;
                dialogueBlockOffsets.Clear();

                int codeStart = writer.Position;
                Emitter.CompileSubroutine(moduleBuilder, subroutine, ref writer, dialogueBlockOffsets);
                int codeEnd = writer.Position;

                int codeSize = codeEnd - codeStart;
                writer.Position = start;
                writer.WriteUInt16LE((ushort)(offsetBlockSize + codeSize));

                writer.WriteUInt16LE((ushort)dialogueBlockCount);
                for (int i = 0; i < dialogueBlockCount; i++)
                {
                    writer.WriteUInt16LE((ushort)(dialogueBlockOffsets[i]));
                }

                writer.Position = codeEnd;
            }
        }

        private static void WriteRuntimeInformation(
            ImmutableArray<SubroutineSymbol> subroutines,
            ref BufferWriter rtiWriter,
            ref BufferWriter offsetWriter)
        {
            if (subroutines.Length == 0) { return; }
            foreach (SubroutineSymbol subroutine in subroutines)
            {
                offsetWriter.WriteUInt16LE((ushort)rtiWriter.Position);

                byte kind = subroutine.Kind switch
                {
                    SymbolKind.Chapter => (byte)0x00,
                    SymbolKind.Scene => (byte)0x01,
                    SymbolKind.Function => (byte)0x02,
                    _ => ThrowHelper.Unreachable<byte>()
                };

                rtiWriter.WriteByte(kind);
                rtiWriter.WriteLengthPrefixedUtf8String(subroutine.Name);

                SubroutineDeclarationSyntax decl = subroutine.Declaration;
                rtiWriter.WriteUInt16LE((ushort)decl.DialogueBlocks.Length);
                foreach (DialogueBlockSyntax dialogueBlock in decl.DialogueBlocks)
                {
                    rtiWriter.WriteLengthPrefixedUtf8String(dialogueBlock.AssociatedBox);
                    rtiWriter.WriteLengthPrefixedUtf8String(dialogueBlock.Name);
                }

                if (subroutine.Kind == SymbolKind.Function)
                {
                    var function = (FunctionSymbol)subroutine;
                    Debug.Assert(function != null);
                    rtiWriter.WriteByte((byte)function.Parameters.Length);
                    foreach (ParameterSymbol parameter in function.Parameters)
                    {
                        rtiWriter.WriteLengthPrefixedUtf8String(parameter.Name);
                    }
                }
            }
        }
    }

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

    internal enum LookupResultDiscriminator : byte
    {
        Empty = 0,
        Subroutine,
        Parameter,
        BuiltInFunction,
        BuiltInConstant,
        GlobalVariable
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct LookupResult
    {
        [FieldOffset(0)]
        public readonly LookupResultDiscriminator Discriminator;

        [FieldOffset(4)]
        public readonly BuiltInFunction BuiltInFunction;

        [FieldOffset(4)]
        public readonly BuiltInConstant BuiltInConstant;

        [FieldOffset(8)]
        public readonly SubroutineSymbol Subroutine;

        [FieldOffset(8)]
        public readonly ParameterSymbol Parameter;

        [FieldOffset(8)]
        public readonly string GlobalVariable;

        public LookupResult(SubroutineSymbol subroutine) : this()
            => (Discriminator, Subroutine) = (LookupResultDiscriminator.Subroutine, subroutine);

        public LookupResult(ParameterSymbol parameter) : this()
            => (Discriminator, Parameter) = (LookupResultDiscriminator.Parameter, parameter);

        public LookupResult(BuiltInFunction builtInFunction) : this()
            => (Discriminator, BuiltInFunction) = (LookupResultDiscriminator.BuiltInFunction, builtInFunction);

        public LookupResult(BuiltInConstant builtInConstant) : this()
            => (Discriminator, BuiltInConstant) = (LookupResultDiscriminator.BuiltInConstant, builtInConstant);

        public LookupResult(string globalVariable) : this()
            => (Discriminator, GlobalVariable) = (LookupResultDiscriminator.GlobalVariable, globalVariable);

        public static LookupResult Empty = default;

        public bool IsEmpty => Discriminator == LookupResultDiscriminator.Empty;
    }

    internal readonly struct Checker
    {
        private readonly SubroutineSymbol _subroutine;
        private readonly SourceModuleSymbol _module;
        private readonly Compilation _compilation;
        private readonly DiagnosticBuilder _diagnostics;

        public Checker(SubroutineSymbol subroutine, DiagnosticBuilder diagnostics)
        {
            _subroutine = subroutine;
            _module = subroutine.DeclaringSourceFile.Module;
            _compilation = _module.Compilation;
            _diagnostics = diagnostics;
        }

        public LookupResult ResolveAssignmentTarget(ExpressionSyntax expression)
        {
            if (expression is NameExpressionSyntax nameExpression)
            {
                var identifier = new Spanned<string>(nameExpression.Name, nameExpression.Span);
                return LookupNonInvocableSymbol(identifier, isVariable: true);
            }

            Report(expression, DiagnosticId.BadAssignmentTarget);
            return LookupResult.Empty;
        }

        public ChapterSymbol? ResolveCallChapterTarget(CallChapterStatementSyntax callChapterStmt)
        {
            string modulePath = callChapterStmt.TargetModule.Value;
            try
            {
                SourceModuleSymbol targetSourceModule = _compilation.GetSourceModule(modulePath);
                ChapterSymbol? chapter = targetSourceModule.LookupChapter("main");
                if (chapter == null)
                {
                    Report(callChapterStmt.TargetModule, DiagnosticId.ChapterMainNotFound);
                }

                return chapter;

            }
            catch (FileNotFoundException)
            {
                string moduleName = callChapterStmt.TargetModule.Value;
                Report(callChapterStmt.TargetModule, DiagnosticId.ExternalModuleNotFound, moduleName);
                return null;
            }
        }

        public SceneSymbol? ResolveCallSceneTarget(CallSceneStatementSyntax callSceneStmt)
        {
            if (callSceneStmt.TargetModule == null)
            {
                return LookupScene(callSceneStmt.TargetScene);
            }

            Spanned<string> targetModule = callSceneStmt.TargetModule.Value;
            string modulePath = targetModule.Value;
            try
            {
                SourceModuleSymbol targetSourceModule = _compilation.GetSourceModule(modulePath);
                SceneSymbol? scene = targetSourceModule.LookupScene(callSceneStmt.TargetScene.Value);
                if (scene == null)
                {
                    ReportUnresolvedIdentifier(callSceneStmt.TargetScene);
                }

                return scene;
            }
            catch (FileNotFoundException)
            {
                string moduleName = targetModule.Value;
                Report(targetModule, DiagnosticId.ExternalModuleNotFound, moduleName);
                return null;
            }
        }

        public LookupResult LookupNonInvocableSymbol(Spanned<string> identifier, bool isVariable)
        {
            string name = identifier.Value;
            ParameterSymbol? parameter = _subroutine.LookupParameter(name);
            if (parameter != null)
            {
                return new LookupResult(parameter);
            }

            if (!isVariable)
            {
                BuiltInConstant? builtInConstant = WellKnownSymbols.LookupBuiltInConstant(name);
                if (!builtInConstant.HasValue)
                {
                    return LookupResult.Empty;
                }

                return new LookupResult(builtInConstant.Value);
            }

            return new LookupResult(globalVariable: name);
        }

        public LookupResult LookupFunction(Spanned<string> identifier)
        {
            string name = identifier.Value;
            BuiltInFunction? builtInFunction = WellKnownSymbols.LookupBuiltInFunction(name);
            if (builtInFunction.HasValue)
            {
                return new LookupResult(builtInFunction.Value);
            }

            FunctionSymbol? function = _module.LookupFunction(name);
            if (function != null)
            {
                return new LookupResult(function);
            }

            ReportUnresolvedIdentifier(identifier);
            return LookupResult.Empty;
        }

        public ChapterSymbol? LookupChapter(Spanned<string> identifier)
        {
            ChapterSymbol? chapter = _module.LookupChapter(identifier.Value);
            if (chapter != null) { return chapter; }

            ReportUnresolvedIdentifier(identifier);
            return null;
        }

        public SceneSymbol? LookupScene(Spanned<string> identifier)
        {
            SceneSymbol? scene = _module.LookupScene(identifier.Value);
            if (scene != null) { return scene; }

            ReportUnresolvedIdentifier(identifier);
            return null;
        }

        private void ReportUnresolvedIdentifier(Spanned<string> identifier)
        {
            _diagnostics.Add(
                Diagnostic.Create(identifier.Span, DiagnosticId.UnresolvedIdentifier, identifier.Value));
        }

        private void Report(Spanned<string> identifier, DiagnosticId diagnosticId)
        {
            _diagnostics.Add(Diagnostic.Create(identifier.Span, diagnosticId));
        }

        private void Report(Spanned<string> identifier, DiagnosticId diagnosticId, params object[] args)
        {
            _diagnostics.Add(Diagnostic.Create(identifier.Span, diagnosticId, args));
        }

        private void Report(ExpressionSyntax node, DiagnosticId diagnosticId)
        {
            _diagnostics.Add(Diagnostic.Create(node.Span, diagnosticId));
        }
    }

    internal ref struct Emitter
    {
        private const int JumpInstrSize = sizeof(Opcode) + sizeof(ushort);

        private readonly NsxModuleBuilder _module;
        private readonly SubroutineSymbol _subrotuine;
        private readonly Checker _checker;
        private readonly Compilation _compilation;
        private BufferWriter _code;
        private int _textId;
        private readonly TokenMap<ParameterSymbol>? _parameters;

        private readonly Queue<int> _insertBreaksAt;

        private bool _supressConstantLookup;

        public Emitter(NsxModuleBuilder moduleBuilder, SubroutineSymbol subroutine)
        {
            _module = moduleBuilder;
            _subrotuine = subroutine;
            _checker = new Checker(subroutine, moduleBuilder.Diagnostics);
            _compilation = moduleBuilder.Compilation;
            _parameters = null;
            if (subroutine is FunctionSymbol function && function.Parameters.Length > 0)
            {
                ImmutableArray<ParameterSymbol> parameters = function.Parameters;
                _parameters = new TokenMap<ParameterSymbol>((uint)parameters.Length);
                foreach (ParameterSymbol param in parameters)
                {
                    _parameters.GetOrAddToken(param);
                }
            }

            _insertBreaksAt = new Queue<int>();
            _code = default;
            _textId = 0;
            _supressConstantLookup = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitOpcode(Opcode opcode)
            => _code.WriteByte((byte)opcode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetGlobalVarToken(string variableName)
            => _compilation.GetGlobalVarToken(variableName);

        public static void CompileSubroutine(
            NsxModuleBuilder moduleBuilder, SubroutineSymbol subroutine,
            ref BufferWriter codeBuffer, List<int> dialogueBlockOffsets)
        {
            Debug.Assert(dialogueBlockOffsets.Count == 0);
            int start = codeBuffer.Position;
            var emitter = new Emitter(moduleBuilder, subroutine);
            emitter._code = codeBuffer;
            emitter.EmitStatement(subroutine.Declaration.Body);
            emitter.EmitOpcode(Opcode.Return);

            SubroutineDeclarationSyntax decl = subroutine.Declaration;
            ImmutableArray<DialogueBlockSyntax> dialogueBlocks = decl.DialogueBlocks;
            foreach (DialogueBlockSyntax dialogueBlock in dialogueBlocks)
            {
                dialogueBlockOffsets.Add(emitter._code.Position - start);
                emitter.EmitDialogueBlock(dialogueBlock);
            }
            codeBuffer = emitter._code;
        }

        private void EmitUnary(UnaryOperatorKind opKind)
        {
            switch (opKind)
            {
                case UnaryOperatorKind.Not:
                    EmitOpcode(Opcode.Invert);
                    break;
                case UnaryOperatorKind.Minus:
                    EmitOpcode(Opcode.Neg);
                    break;
                case UnaryOperatorKind.Delta:
                    EmitOpcode(Opcode.Delta);
                    break;
            }
        }

        private void EmitBinary(BinaryOperatorKind opKind)
        {
            switch (opKind)
            {
                case BinaryOperatorKind.Equals:
                    EmitOpcode(Opcode.Equal);
                    break;
                case BinaryOperatorKind.NotEquals:
                    EmitOpcode(Opcode.NotEqual);
                    break;
                default:
                    EmitOpcode(Opcode.Binary);
                    _code.WriteByte((byte)opKind);
                    break;
            }
        }

        private void EmitExpression(ExpressionSyntax expression)
        {
            switch (expression.Kind)
            {
                case SyntaxNodeKind.LiteralExpression:
                    EmitLiteral((LiteralExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.NameExpression:
                    EmitNameExpression((NameExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.UnaryExpression:
                    EmitUnaryExpression((UnaryExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.BinaryExpression:
                    EmitBinaryExpression((BinaryExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.AssignmentExpression:
                    EmitAssignmentExpression((AssignmentExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.FunctionCallExpression:
                    EmitFunctionCall((FunctionCallExpressionSyntax)expression);
                    break;
            }
        }

        private void EmitLiteral(LiteralExpressionSyntax literal)
        {
            ConstantValue value = literal.Value;
            if (value.Type == BuiltInType.String && !_supressConstantLookup)
            {
                string strValue = value.AsString()!;
                BuiltInConstant? constant = WellKnownSymbols.LookupBuiltInConstant(strValue);
                if (constant.HasValue)
                {
                    value = ConstantValue.BuiltInConstant(constant.Value);
                }
            }

            EmitLoadImm(value);
        }

        private void EmitNameExpression(NameExpressionSyntax expression)
        {
            var spanned = new Spanned<string>(expression.Name, expression.Span);
            bool isVariable = expression.HasSigil;
            LookupResult lookupResult = _checker.LookupNonInvocableSymbol(spanned, isVariable);
            switch (lookupResult.Discriminator)
            {
                case LookupResultDiscriminator.BuiltInConstant:
                    EmitLoadImm(ConstantValue.BuiltInConstant(lookupResult.BuiltInConstant));
                    break;
                case LookupResultDiscriminator.Parameter:
                    Debug.Assert(_parameters != null);
                    ushort slot = _parameters.GetOrAddToken(lookupResult.Parameter);
                    EmitLoadArg(slot);
                    break;
                case LookupResultDiscriminator.GlobalVariable:
                    ushort varToken = GetGlobalVarToken(lookupResult.GlobalVariable);
                    EmitOpcode(Opcode.LoadVar);
                    _code.WriteUInt16LE(varToken);
                    break;
                case LookupResultDiscriminator.Empty:
                    var literal = ConstantValue.String(expression.Name);
                    EmitLoadImm(literal);
                    break;
            }
        }

        private void EmitUnaryExpression(UnaryExpressionSyntax expression)
        {
            EmitExpression(expression.Operand);
            EmitUnary(expression.OperatorKind.Value);
        }

        private void EmitBinaryExpression(BinaryExpressionSyntax expression)
        {
            EmitExpression(expression.Left);
            EmitExpression(expression.Right);
            EmitBinary(expression.OperatorKind.Value);
        }

        enum VariableKind
        {
            Global,
            Parameter
        }

        private void EmitAssignmentExpression(AssignmentExpressionSyntax assignmentExpr)
        {
            LookupResult target = _checker.ResolveAssignmentTarget(assignmentExpr.Target);
            if (target.IsEmpty) { return; }

            EmitExpression(assignmentExpr.Value);

            ushort token;
            VariableKind variableKind;
            if (target.Discriminator == LookupResultDiscriminator.Parameter)
            {
                Debug.Assert(_parameters != null);
                token = _parameters.GetOrAddToken(target.Parameter);
                variableKind = VariableKind.Parameter;
            }
            else
            {
                Debug.Assert(target.Discriminator == LookupResultDiscriminator.GlobalVariable);
                token = GetGlobalVarToken(target.GlobalVariable);
                variableKind = VariableKind.Global;
            }

            AssignmentOperatorKind opKind = assignmentExpr.OperatorKind.Value;
            if (opKind != AssignmentOperatorKind.Assign)
            {
                EmitLoadVariable(variableKind, token);
            }

            switch (opKind)
            {
                case AssignmentOperatorKind.Assign:
                    break;
                case AssignmentOperatorKind.AddAssign:
                    EmitBinary(BinaryOperatorKind.Add);
                    break;
                case AssignmentOperatorKind.SubtractAssign:
                    EmitBinary(BinaryOperatorKind.Subtract);
                    break;
                case AssignmentOperatorKind.MultiplyAssign:
                    EmitBinary(BinaryOperatorKind.Multiply);
                    break;
                case AssignmentOperatorKind.DivideAssign:
                    EmitBinary(BinaryOperatorKind.Divide);
                    break;
                case AssignmentOperatorKind.Increment:
                    EmitOpcode(Opcode.Inc);
                    break;
                case AssignmentOperatorKind.Decrement:
                    EmitOpcode(Opcode.Dec);
                    break;
            }

            EmitStoreOp(variableKind, token);
        }

        private void EmitFunctionCall(FunctionCallExpressionSyntax callExpression)
        {
            LookupResult lookupResult = _checker.LookupFunction(callExpression.TargetName);
            if (lookupResult.IsEmpty) { return; }

            ImmutableArray<ExpressionSyntax> arguments = callExpression.Arguments;
            for (int i = 0; i < arguments.Length; i++)
            {
                // Assumption: the first argument is never a built-in constant
                // Even if it looks like one (e.g. "Black"), it should be treated
                // as a string literal, not as a built-in constant.
                // Reasoning: "Black" and "White" are sometimes used as entity names.
                _supressConstantLookup = i == 0;
                EmitExpression(arguments[i]);
            }

            if (lookupResult.Discriminator == LookupResultDiscriminator.BuiltInFunction)
            {
                EmitOpcode(Opcode.Dispatch);
                _code.WriteByte((byte)lookupResult.BuiltInFunction);
                _code.WriteByte((byte)callExpression.Arguments.Length);
            }
            else
            {
                var function = (FunctionSymbol)lookupResult.Subroutine;
                if (ReferenceEquals(function.DeclaringSourceFile, _subrotuine.DeclaringSourceFile))
                {
                    EmitOpcode(Opcode.Call);
                    _code.WriteUInt16LE(_module.GetSubroutineToken(function));
                    _code.WriteByte((byte)callExpression.Arguments.Length);
                }
                else
                {
                    EmitOpcode(Opcode.CallFar);
                    SourceFileSymbol externalSourceFile = function.DeclaringSourceFile;
                    NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
                    _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
                    _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(function));
                    _code.WriteByte((byte)callExpression.Arguments.Length);
                }
            }
        }

        private void EmitCallChapter(CallChapterStatementSyntax statement)
        {
            ChapterSymbol? chapter = _checker.ResolveCallChapterTarget(statement);
            if (chapter != null)
            {
                EmitCallFar(chapter);
            }
        }

        private void EmitCallScene(CallSceneStatementSyntax statement)
        {
            SceneSymbol? scene = _checker.ResolveCallSceneTarget(statement);
            if (scene != null)
            {
                EmitCallFar(scene);
            }
        }

        private void EmitCallFar(SubroutineSymbol subroutine)
        {
            EmitOpcode(Opcode.CallFar);
            SourceFileSymbol externalSourceFile = subroutine.DeclaringSourceFile;
            NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
            _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
            _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(subroutine));
            _code.WriteByte(0);
        }

        private void EmitStatement(StatementSyntax statement)
        {
            switch (statement.Kind)
            {
                case SyntaxNodeKind.Block:
                    EmitBlock((BlockSyntax)statement);
                    break;
                case SyntaxNodeKind.ExpressionStatement:
                    EmitExpressionStatement((ExpressionStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.IfStatement:
                    EmitIfStatement((IfStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.BreakStatement:
                    EmitBreakStatement();
                    break;
                case SyntaxNodeKind.WhileStatement:
                    EmitWhileStatement((WhileStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.ReturnStatement:
                    EmitReturnStatement();
                    break;
                case SyntaxNodeKind.CallChapterStatement:
                    EmitCallChapter((CallChapterStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.CallSceneStatement:
                    EmitCallScene((CallSceneStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.SelectStatement:
                    EmitSelect((SelectStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.SelectSection:
                    EmitSelectSection((SelectSectionSyntax)statement);
                    break;
                case SyntaxNodeKind.DialogueBlock:
                    EmitActivateText();
                    break;
                case SyntaxNodeKind.PXmlString:
                    EmitDialogue((PXmlString)statement);
                    break;
                case SyntaxNodeKind.PXmlLineSeparator:
                    EmitOpcode(Opcode.AwaitInput);
                    break;
            }
        }

        private void EmitActivateText()
        {
            EmitOpcode(Opcode.ActivateText);
            _code.WriteUInt16LE((ushort)_textId++);
        }

        private void EmitDialogue(PXmlString dialogue)
        {
            EmitOpcode(Opcode.PresentText);
            _code.WriteUInt16LE(_module.GetStringToken(dialogue.Text));
        }

        private void EmitSelectSection(SelectSectionSyntax section)
        {
            EmitOpcode(Opcode.GetSelChoice);
            EmitLoadImm(ConstantValue.String(section.Label.Value));
            EmitBinary(BinaryOperatorKind.Equals);
            int jumpPos = _code.Position;
            _code.Position += JumpInstrSize;

            EmitStatement(section.Body);
            int end = _code.Position;

            _code.Position = jumpPos;
            EmitOpcode(Opcode.JumpIfFalse);
            _code.WriteInt16LE((short)(end - jumpPos));
            _code.Position = end;
        }

        private void EmitSelect(SelectStatementSyntax selectStmt)
        {
            EmitOpcode(Opcode.Select);
            EmitStatement(selectStmt.Body);
        }

        private void EmitBlock(BlockSyntax block)
        {
            foreach (StatementSyntax statement in block.Statements)
            {
                EmitStatement(statement);
            }
        }

        private void EmitExpressionStatement(ExpressionStatementSyntax statement)
        {
            EmitExpression(statement.Expression);
        }

        private void EmitIfStatement(IfStatementSyntax ifStmt)
        {
            EmitExpression(ifStmt.Condition);

            if (ifStmt.IfFalseStatement == null)
            {
                // if (<condition>)
                //      <consequence>
                //
                // ---->
                //
                // <condition>
                // JumpIfFalse exit
                // <consequence>
                // end:

                int jmpInstrPos = _code.Position;
                _code.Position += JumpInstrSize;
                EmitStatement(ifStmt.IfTrueStatement);
                int end = _code.Position;
                _code.Position = jmpInstrPos;
                EmitOpcode(Opcode.JumpIfFalse);
                _code.WriteInt16LE((short)(end - jmpInstrPos));
                _code.Position = end;
            }
            else
            {
                // if <condition>
                //      <consequence>
                // else
                //      <alternative>
                //
                // ---->
                //
                // <condition>
                // JumpIfFalse alternative  # first jmp
                // <consequence>
                // Jump end                 # second jmp
                // <alternative>
                // end:

                int firstJmpInstrPos = _code.Position;
                _code.Position += JumpInstrSize;

                EmitStatement(ifStmt.IfTrueStatement);

                int secondJmpInstrPos = _code.Position;
                _code.Position += JumpInstrSize;
                int alternativePos = _code.Position;

                EmitStatement(ifStmt.IfFalseStatement);

                int end = _code.Position;
                _code.Position = secondJmpInstrPos;
                EmitOpcode(Opcode.Jump);
                _code.WriteInt16LE((short)(end - secondJmpInstrPos));

                _code.Position = firstJmpInstrPos;
                EmitOpcode(Opcode.JumpIfFalse);
                _code.WriteInt16LE((short)(alternativePos - firstJmpInstrPos));
                _code.Position = end;
            }
        }

        private void EmitReturnStatement()
        {
            EmitOpcode(Opcode.Return);
        }

        private void EmitWhileStatement(WhileStatementSyntax whileStmt)
        {
            // while (<condition>)
            //     <body>
            //
            // ---->
            //
            // <condition>
            // JumpIfFalse exit
            // <body>
            // Jump <condition>
            // exit:

            int conditionPos = _code.Position;
            EmitExpression(whileStmt.Condition);
            int firstJmpPos = _code.Position;
            _code.Position += JumpInstrSize;

            EmitStatement(whileStmt.Body);
            int secondJumpPos = _code.Position;
            EmitOpcode(Opcode.Jump);
            _code.WriteInt16LE((short)(conditionPos - secondJumpPos));
            int exit = _code.Position;

            _code.Position = firstJmpPos;
            EmitOpcode(Opcode.JumpIfFalse);
            _code.WriteInt16LE((short)(exit - firstJmpPos));

            while (_insertBreaksAt.Count > 0)
            {
                int pos = _insertBreaksAt.Dequeue();
                _code.Position = pos;
                _code.WriteInt16LE((short)(exit - pos));
            }

            _code.Position = exit;
        }

        private void EmitBreakStatement()
        {
            _insertBreaksAt.Enqueue(_code.Position);
            _code.Position += JumpInstrSize;
        }

        private void EmitLoadVariable(VariableKind variableKind, ushort token)
        {
            switch (variableKind)
            {
                case VariableKind.Global:
                    EmitOpcode(Opcode.LoadVar);
                    _code.WriteUInt16LE(token);
                    break;
                case VariableKind.Parameter:
                    EmitLoadArg(token);
                    break;
            }
        }

        private void EmitStoreOp(VariableKind variableKind, ushort tk)
        {
            if (variableKind == VariableKind.Global)
            {
                EmitOpcode(Opcode.StoreVar);
                _code.WriteUInt16LE(tk);
            }
            else
            {
                Debug.Assert(variableKind == VariableKind.Parameter);
                EmitStoreArg(tk);
            }
        }

        private void EmitLoadImm(in ConstantValue value)
        {
            switch (value.Type)
            {
                case BuiltInType.Integer:
                    int num = value.AsInteger()!.Value;
                    switch (num)
                    {
                        case 0:
                            EmitOpcode(Opcode.LoadImm0);
                            break;
                        case 1:
                            EmitOpcode(Opcode.LoadImm1);
                            break;
                        default:
                            EmitOpcode(Opcode.LoadImm);
                            _code.WriteByte((byte)value.Type);
                            _code.WriteInt32LE(num);
                            break;
                    }
                    break;
                case BuiltInType.DeltaInteger:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteInt32LE(value.AsDelta()!.Value);
                    break;
                case BuiltInType.Boolean:
                    Opcode opcode = value.AsBool()!.Value
                        ? Opcode.LoadImmTrue
                        : Opcode.LoadImmFalse;
                    EmitOpcode(opcode);
                    break;
                case BuiltInType.Float:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteSingle(value.AsFloat()!.Value);
                    break;
                case BuiltInType.String:
                    string str = value.AsString()!;
                    if (string.IsNullOrEmpty(str))
                    {
                        EmitOpcode(Opcode.LoadImmEmptyStr);
                    }
                    else
                    {
                        EmitOpcode(Opcode.LoadImm);
                        _code.WriteByte((byte)value.Type);
                        ushort token = _module.GetStringToken(str);
                        _code.WriteUInt16LE(token);
                    }
                    break;
                case BuiltInType.BuiltInConstant:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteByte((byte)value.AsInteger()!.Value);
                    break;
                case BuiltInType.Null:
                    EmitOpcode(Opcode.LoadImmNull);
                    break;
            }
        }

        private void EmitLoadArg(ushort slot)
        {
            Opcode opcode = slot switch
            {
                0 => Opcode.LoadArg0,
                1 => Opcode.LoadArg1,
                2 => Opcode.LoadArg2,
                3 => Opcode.LoadArg3,
                _ => Opcode.LoadArg
            };

            EmitOpcode(opcode);
            if (slot > 3)
            {
                _code.WriteUInt16LE(slot);
            }
        }

        private void EmitStoreArg(ushort slot)
        {
            Opcode opcode = slot switch
            {
                0 => Opcode.StoreArg0,
                1 => Opcode.StoreArg1,
                2 => Opcode.StoreArg2,
                3 => Opcode.StoreArg3,
                _ => Opcode.StoreArg
            };

            EmitOpcode(opcode);
            if (slot > 3)
            {
                _code.WriteUInt16LE(slot);
            }
        }

        private void EmitDialogueBlock(DialogueBlockSyntax dialogueBlock)
        {
            foreach (StatementSyntax statement in dialogueBlock.Parts)
            {
                EmitStatement(statement);
            }
            EmitOpcode(Opcode.Return);
        }
    }

    internal class TokenMap<T> where T : class
    {
        private readonly Dictionary<T, ushort> _itemToToken;
        private ArrayBuilder<T> _items;

        public TokenMap(uint initialCapacity = 8)
        {
            _itemToToken = new Dictionary<T, ushort>((int)initialCapacity);
            _items = new ArrayBuilder<T>(initialCapacity);
        }

        public uint Count => _items.Count;
        public ReadOnlySpan<T> AsSpan() => _items.AsReadonlySpan();

        public ushort GetOrAddToken(T item)
        {
            if (!TryGetToken(item, out ushort token))
            {
                token = AddToken(item);
            }

            return token;
        }

        public bool TryGetToken(T item, out ushort token)
            => _itemToToken.TryGetValue(item, out token);

        public ushort AddToken(T item)
        {
            ushort token = (ushort)_items.Count;
            _items.Add(item);
            _itemToToken.Add(item, token);
            return token;
        }
    }
}
