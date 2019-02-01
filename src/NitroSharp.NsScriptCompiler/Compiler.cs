using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.NsScriptCompiler.Playground;
using NitroSharp.NsScriptNew.CodeGen;
using NitroSharp.NsScriptNew.Symbols;
using NitroSharp.NsScriptNew.Syntax;
using NitroSharp.NsScriptNew.Text;
using NitroSharp.Utilities;

namespace NitroSharp.NsScriptNew
{
    public class Compilation
    {
        private static ReadOnlySpan<byte> CRLF => new byte[] { 0x0D, 0x0A };

        private readonly SourceReferenceResolver _sourceReferenceResolver;
        private readonly Dictionary<ResolvedPath, SyntaxTree> _syntaxTrees;
        private readonly Dictionary<SyntaxTree, SourceModuleSymbol> _sourceModuleSymbols;

        private readonly Dictionary<ResolvedPath, NsxModuleBuilder> _nsxModuleBuilders;
        private readonly TokenMap<string> _globals = new TokenMap<string>(4096);

        public Compilation(string rootSourceDirectory)
            : this(new DefaultSourceReferenceResolver(rootSourceDirectory))
        {
        }

        public Compilation(SourceReferenceResolver sourceReferenceResolver)
        {
            _sourceReferenceResolver = sourceReferenceResolver;
            _syntaxTrees = new Dictionary<ResolvedPath, SyntaxTree>();
            _sourceModuleSymbols = new Dictionary<SyntaxTree, SourceModuleSymbol>();
            _nsxModuleBuilders = new Dictionary<ResolvedPath, NsxModuleBuilder>();
        }

        public SourceReferenceResolver SourceReferenceResolver => _sourceReferenceResolver;

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

            //using (FileStream file = File.OpenWrite("S:/globals"))
            //{
            //    foreach (string variable in _globals.GetAll())
            //    {
            //        int sz = Encoding.UTF8.GetByteCount(variable);
            //        Span<byte> utf8Bytes = sz <= 256
            //            ? stackalloc byte[sz]
            //            : new byte[sz];
            //        Encoding.UTF8.GetBytes(variable, utf8Bytes);
            //        file.Write(utf8Bytes);
            //        file.Write(CRLF);
            //    }
            //}
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
            if (_syntaxTrees.TryGetValue(resolvedPath, out SyntaxTree syntaxTree))
            {
                return syntaxTree;
            }

            SourceText sourceText = _sourceReferenceResolver.ReadText(resolvedPath);
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
            if (_sourceModuleSymbols.TryGetValue(syntaxTree, out SourceModuleSymbol symbol))
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
            if (!_nsxModuleBuilders.TryGetValue(sourceFile.FilePath, out NsxModuleBuilder moduleBuilder))
            {
                moduleBuilder = new NsxModuleBuilder(this, sourceFile);
                _nsxModuleBuilders.Add(sourceFile.FilePath, moduleBuilder);
            }

            return moduleBuilder;
        }

        internal ushort GetGlobalVarToken(string variableName)
        {
            return _globals.GetOrAddToken(variableName);
        }
    }

    static class NsxModuleAssembler
    {
        private static ReadOnlySpan<byte> NsxMagic => new byte[] { 0x4E, 0x53, 0x58, 0x00 };
        private static ReadOnlySpan<byte> SubTableMarker => new byte[] { 0x53, 0x55, 0x42, 0x00 };
        private static ReadOnlySpan<byte> StringTableMarker => new byte[] { 0x53, 0x54, 0x52, 0x00 };
        private static ReadOnlySpan<byte> TableEndMarker => new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        public static void WriteModule(NsxModuleBuilder builder)
        {
            const int nsxHeaderSize = 20;

            SourceFileSymbol sourceFile = builder.SourceFile;
            ReadOnlySpan<SubroutineSymbol> subroutines = builder.Subroutines;

            // Compile subroutines
            var codeBuffer = PooledBuffer<byte>.Allocate(64 * 1024);
            var codeWriter = new BufferWriter(codeBuffer);
            var subroutineOffsets = new List<int>(subroutines.Length);
            CompileSubroutines(builder, sourceFile.Chapters, ref codeWriter, subroutineOffsets);
            CompileSubroutines(builder, sourceFile.Scenes, ref codeWriter, subroutineOffsets);
            CompileSubroutines(builder, sourceFile.Functions, ref codeWriter, subroutineOffsets);
            codeWriter.WriteBytes(TableEndMarker);

            ReadOnlySpan<string> stringHeap = builder.StringHeap;
            int subTableSize = subroutines.Length * sizeof(int) + 10;
            int stringTableSize = stringHeap.Length * sizeof(int) + 10;

            // Build the runtime information table (RTI)
            int rtiTableOffset = nsxHeaderSize + subTableSize;
            using var rtiBuffer = PooledBuffer<byte>.Allocate(4096);
            var rtiWriter = new BufferWriter(rtiBuffer);
            rtiWriter.WriteUtf8String("RTI\0");
            WriteRuntimeInformation(sourceFile.Chapters, ref rtiWriter);
            WriteRuntimeInformation(sourceFile.Scenes, ref rtiWriter);
            WriteRuntimeInformation(sourceFile.Functions, ref rtiWriter);
            rtiWriter.WriteBytes(TableEndMarker);

            int strTableOffset = rtiTableOffset + rtiWriter.Position;
            int codeStart = strTableOffset + stringTableSize;

            // Build the subroutine offset table (SUB)
            using var subTable = PooledBuffer<byte>.Allocate((uint)subTableSize);
            var subWriter = new BufferWriter(subTable);
            subWriter.WriteBytes(SubTableMarker); // SUB\0
            subWriter.WriteUInt16LE((ushort)subroutines.Length);
            for (int i = 0; i < subroutines.Length; i++)
            {
                subWriter.WriteInt32LE(subroutineOffsets[i] + codeStart);
            }
            subWriter.WriteBytes(TableEndMarker);

            // Encode strings and build the offset table (STR)
            int stringHeapStart = codeStart + codeWriter.Position;
            using var stringHeapBuffer = PooledBuffer<byte>.Allocate(64 * 1024);
            using var stringOffsetTable = PooledBuffer<byte>.Allocate((uint)stringTableSize);
            var strTableWriter = new BufferWriter(stringOffsetTable);
            strTableWriter.WriteBytes(StringTableMarker); // STR\0
            strTableWriter.WriteUInt16LE((ushort)stringHeap.Length);

            var stringWriter = new BufferWriter(stringHeapBuffer);
            foreach (string s in stringHeap)
            {
                strTableWriter.WriteInt32LE(stringHeapStart + stringWriter.Position);
                stringWriter.WriteLengthPrefixedUtf8String(s);
            }
            strTableWriter.WriteBytes(TableEndMarker);

            // Build the NSX header
            using var headerBuffer = PooledBuffer<byte>.Allocate(nsxHeaderSize);
            var headerWriter = new BufferWriter(headerBuffer);
            headerWriter.WriteBytes(NsxMagic);
            headerWriter.WriteInt32LE(rtiTableOffset);
            headerWriter.WriteInt32LE(strTableOffset);
            headerWriter.Position += 8;

            // --- Write everything to the stream ---
            using FileStream fileStream = File.Create($"S:/ChaosContent/Noah/nsx/{Path.ChangeExtension(sourceFile.Name, "nsx")}");
            fileStream.Write(headerWriter.Written);
            fileStream.Write(subWriter.Written);
            fileStream.Write(rtiWriter.Written);
            fileStream.Write(strTableWriter.Written);
            fileStream.Write(codeWriter.Written);
            fileStream.Write(stringWriter.Written);
        }

        private static void CompileSubroutines<T>(
            NsxModuleBuilder moduleBuilder, ImmutableArray<T> subroutines,
            ref BufferWriter writer, List<int> subroutineOffsets)
            where T : SubroutineSymbol
        {
            if (subroutines.Length == 0) { return; }
            var dialogueBlockOffsets = new List<int>();
            foreach (T subroutine in subroutines)
            {
                SubroutineDeclarationSyntax decl = subroutine.Declaration;
                int dialogueBlockCount = decl.DialogueBlocks.Length;
                int headerOffset = writer.Position;
                int headerSize = dialogueBlockCount * sizeof(ushort) + 2;
                writer.Position += headerSize;
                dialogueBlockOffsets.Clear();

                subroutineOffsets.Add(writer.Position);
                Emitter<T>.CompileSubroutine(moduleBuilder, subroutine, ref writer, dialogueBlockOffsets);

                int oldPosition = writer.Position;
                writer.Position = headerOffset;
                writer.WriteUInt16LE((ushort)dialogueBlockCount);
                for (int i = 0; i < dialogueBlockCount; i++)
                {
                    writer.WriteUInt16LE((ushort)(dialogueBlockOffsets[i] + headerSize));
                }

                writer.Position = oldPosition;
            }
        }

        private static void WriteRuntimeInformation<T>(
            ImmutableArray<T> subroutines, ref BufferWriter writer)
            where T : SubroutineSymbol
        {
            if (subroutines.Length == 0) { return; }
            foreach (T subroutine in subroutines)
            {
                byte kind = subroutine.Kind switch
                {
                    SymbolKind.Chapter => (byte)0x00,
                    SymbolKind.Scene => (byte)0x01,
                    SymbolKind.Function => (byte)0x02,
                    _ => ExceptionUtils.Unreachable<byte>()
                };

                writer.WriteByte(kind);
                writer.WriteLengthPrefixedUtf8String(subroutine.Name);

                if (typeof(T) == typeof(FunctionSymbol))
                {
                    var function = subroutine as FunctionSymbol;
                    Debug.Assert(function != null);
                    writer.WriteByte((byte)function.Parameters.Length);
                    foreach (ParameterSymbol parameter in function.Parameters)
                    {
                        writer.WriteLengthPrefixedUtf8String(parameter.Name);
                    }
                }

                SubroutineDeclarationSyntax decl = subroutine.Declaration;
                writer.WriteUInt16LE((ushort)decl.DialogueBlocks.Length);
                foreach (DialogueBlockSyntax dialogueBlock in decl.DialogueBlocks)
                {
                    writer.WriteLengthPrefixedUtf8String(dialogueBlock.Name);
                }
            }
        }
    }

    sealed class NsxModuleBuilder
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

    struct Checker<T> where T : SubroutineSymbol
    {
        private readonly T _subroutine;
        private readonly SourceModuleSymbol _module;
        private readonly Compilation _compilation;
        private readonly DiagnosticBuilder _diagnostics;

        public Checker(T subroutine, DiagnosticBuilder diagnostics)
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
            SourceModuleSymbol targetSourceModule;
            try
            {
                targetSourceModule = _compilation.GetSourceModule(modulePath);
                SourceFileSymbol rootSourceFile = targetSourceModule.RootSourceFile;
                ChapterSymbol? chapter = targetSourceModule.LookupChapter("main");
                if (chapter == null)
                {
                    Report(callChapterStmt.TargetModule, DiagnosticId.ChapterMainNotFound);
                    return null;
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
                    ReportUnresolvedIdentifier(identifier);
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
            Debug.WriteLine($"{_subroutine.DeclaringSourceFile.Name}: {identifier.Value}");
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

    internal readonly ref struct EmitResult
    {
        public readonly ReadOnlySpan<ushort> DialogueBlockOffsets;

        public EmitResult(ReadOnlySpan<ushort> dialogueBlockOffsets)
        {
            DialogueBlockOffsets = dialogueBlockOffsets;
        }
    }

    ref struct Emitter<T> where T : SubroutineSymbol
    {
        private const int JumpInstrSize = sizeof(Opcode) + sizeof(ushort);

        private readonly NsxModuleBuilder _module;
        private readonly T _subrotuine;
        private Checker<T> _checker;
        private readonly Compilation _compilation;
        private readonly SourceModuleSymbol _sourceModule;
        private BufferWriter _code;

        private readonly TokenMap<ParameterSymbol>? _parameters;

        private readonly Queue<int> _insertBreaksAt;

        public Emitter(NsxModuleBuilder moduleBuilder, T subroutine)
        {
            _module = moduleBuilder;
            _subrotuine = subroutine;
            _checker = new Checker<T>(subroutine, moduleBuilder.Diagnostics);
            _compilation = moduleBuilder.Compilation;
            _sourceModule = _subrotuine.DeclaringSourceFile.Module;
            _parameters = null;
            if (subroutine is FunctionSymbol function && function.Parameters.Length > 0)
            {
                _parameters = new TokenMap<ParameterSymbol>();
            }

            _insertBreaksAt = new Queue<int>();
            _code = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitOpcode(Opcode opcode)
            => _code.WriteByte((byte)opcode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetGlobalVarToken(string variableName)
            => _compilation.GetGlobalVarToken(variableName);

        public static void CompileSubroutine(
            NsxModuleBuilder moduleBuilder, T subroutine,
            ref BufferWriter codeBuffer, List<int> dialogueBlockOffsets)
        {
            Debug.Assert(dialogueBlockOffsets.Count == 0);
            int start = codeBuffer.Position;
            var emitter = new Emitter<T>(moduleBuilder, subroutine);
            emitter._code = codeBuffer;
            emitter.EmitStatement(subroutine.Declaration.Body);

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
                case UnaryOperatorKind.Minus:
                    EmitOpcode(Opcode.Neg);
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
                case SyntaxNodeKind.DeltaExpression:
                    EmitDeltaExpression((DeltaExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.FunctionCallExpression:
                    EmitFunctionCall((FunctionCallExpressionSyntax)expression);
                    break;
            }
        }

        private void EmitLiteral(LiteralExpressionSyntax literal)
        {
            EmitLoadImm(literal.Value);
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

        private void EmitDeltaExpression(DeltaExpressionSyntax expression)
        {
        }

        private void EmitFunctionCall(FunctionCallExpressionSyntax callExpression)
        {
            LookupResult lookupResult = _checker.LookupFunction(callExpression.TargetName);
            if (lookupResult.IsEmpty) { return; }

            foreach (ExpressionSyntax argument in callExpression.Arguments)
            {
                EmitExpression(argument);
            }

            if (lookupResult.Discriminator == LookupResultDiscriminator.BuiltInFunction)
            {
                EmitOpcode(Opcode.Dispatch);
                _code.WriteByte((byte)lookupResult.BuiltInFunction);
            }
            else
            {
                var function = (FunctionSymbol)lookupResult.Subroutine;
                if (ReferenceEquals(function.DeclaringSourceFile, _subrotuine.DeclaringSourceFile))
                {
                    EmitOpcode(Opcode.Call);
                    _code.WriteUInt16LE(_module.GetSubroutineToken(function));
                }
                else
                {
                    EmitOpcode(Opcode.CallFar);
                    SourceFileSymbol externalSourceFile = function.DeclaringSourceFile;
                    NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
                    _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
                    _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(function));
                }
            }
        }

        private void EmitCallChapter(CallChapterStatementSyntax statement)
        {
            ChapterSymbol? chapter = _checker.ResolveCallChapterTarget(statement);
            if (chapter == null) { return; }

            EmitOpcode(Opcode.CallFar);
            SourceFileSymbol externalSourceFile = chapter.DeclaringSourceFile;
            NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
            _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
            _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(chapter));
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
                    EmitBreakStatement((BreakStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.WhileStatement:
                    EmitWhileStatement((WhileStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.ReturnStatement:
                    EmitReturnStatement((ReturnStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.CallChapterStatement:
                    EmitCallChapter((CallChapterStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.SelectStatement:
                    EmitSelect((SelectStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.SelectSection:
                    EmitSelectSection((SelectSectionSyntax)statement);
                    break;
                case SyntaxNodeKind.DialogueBlock:
                    break;
                case SyntaxNodeKind.PXmlString:
                    EmitDialogue((PXmlString)statement);
                    break;
                case SyntaxNodeKind.PXmlLineSeparator:
                    EmitOpcode(Opcode.AwaitInput);
                    break;
            }
        }

        private void EmitDialogue(PXmlString textNode)
        {
            EmitOpcode(Opcode.PresentText);
            _code.WriteUInt16LE(_module.GetStringToken(textNode.Text));
        }

        private void EmitSelectSection(SelectSectionSyntax statement)
        {
            EmitStatement(statement.Body);
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

                int jmpInstrOffset = _code.Position;
                _code.Position += JumpInstrSize;
                EmitStatement(ifStmt.IfTrueStatement);
                int end = _code.Position;
                _code.Position = jmpInstrOffset;
                EmitOpcode(Opcode.JumpIfFalse);
                _code.WriteInt16LE((short)(end - jmpInstrOffset));
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

                int firstJmpInstrOffset = _code.Position;
                _code.Position += JumpInstrSize;

                EmitStatement(ifStmt.IfTrueStatement);

                int secondJmpInstrOffset = _code.Position;
                _code.Position += JumpInstrSize;
                int alternativePos = _code.Position;

                EmitStatement(ifStmt.IfFalseStatement);

                int end = _code.Position;
                int secondJmpTargetOffset = end - secondJmpInstrOffset;
                _code.Position = secondJmpInstrOffset;
                EmitOpcode(Opcode.Jump);
                _code.WriteInt16LE((short)secondJmpTargetOffset);

                _code.Position = firstJmpInstrOffset;
                EmitOpcode(Opcode.JumpIfFalse);
                _code.WriteInt16LE((short)(alternativePos - firstJmpInstrOffset));
                _code.Position = end;
            }
        }

        private void EmitReturnStatement(ReturnStatementSyntax statement)
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
            int firstJmpOffset = _code.Position;
            _code.Position += JumpInstrSize;

            EmitStatement(whileStmt.Body);
            EmitOpcode(Opcode.Jump);
            _code.WriteInt16LE((short)(conditionPos - _code.Position));
            int exit = _code.Position;

            _code.Position = firstJmpOffset;
            EmitOpcode(Opcode.JumpIfFalse);
            _code.WriteInt16LE((short)(exit - firstJmpOffset));
            _code.Position = exit;

            while (_insertBreaksAt.Count > 0)
            {
                int pos = _insertBreaksAt.Dequeue();
                _code.Position = pos;
                _code.WriteInt16LE((short)(exit - pos));
            }

            _code.Position = exit;
        }

        private void EmitBreakStatement(BreakStatementSyntax statement)
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
                    int num = value.IntegerValue;
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
                case BuiltInType.Boolean:
                    Opcode opcode = value.BooleanValue
                        ? Opcode.LoadImmTrue
                        : Opcode.LoadImmFalse;
                    EmitOpcode(opcode);
                    break;
                case BuiltInType.Float:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteSingle(value.FloatValue);
                    break;
                case BuiltInType.String:
                    string str = value.StringValue;
                    if (string.IsNullOrEmpty(str))
                    {
                        EmitOpcode(Opcode.LoadImmEmptyStr);
                    }
                    else
                    {
                        EmitOpcode(Opcode.LoadImm);
                        _code.WriteByte((byte)value.Type);
                        ushort token = _module.GetStringToken(value.StringValue);
                        _code.WriteUInt16LE(token);
                    }
                    break;
                case BuiltInType.BuiltInConstant:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteByte((byte)value.BuiltInConstantValue);
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
        }
    }

    class TokenMap<T> where T : class
    {
        private readonly Dictionary<T, ushort> _itemToToken;
        private ArrayBuilder<T> _items;

        public TokenMap(uint initialCapacity = 8)
        {
            _itemToToken = new Dictionary<T, ushort>((int)initialCapacity);
            _items = new ArrayBuilder<T>(initialCapacity);
        }

        public ReadOnlySpan<T> AsSpan() => _items.AsReadonlySpan();

        public ushort GetOrAddToken(T item)
        {
            if (!_itemToToken.TryGetValue(item, out ushort token))
            {
                token = (ushort)_items.Count;
                _items.Add(item);
                _itemToToken.Add(item, token);
            }

            return token;
        }
    }
}
