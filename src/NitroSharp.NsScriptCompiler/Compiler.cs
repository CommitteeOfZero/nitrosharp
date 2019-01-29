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
                        moduleBuilder.RealizeAll();
                        compiledSourceFiles.Add(kvp.Key);
                        filesCompiled++;
                    }
                }
            } while (filesCompiled > 0);
        }

        public BufferWriter CompileMember<T>(T member) where T : MemberSymbol
        {
            NsxModuleBuilder nsxBuilder = GetNsxModuleBuilder(member.DeclaringSourceFile);
            return nsxBuilder.Emit(member);
        }

        private void EmitCore(SourceFileSymbol sourceFile, HashSet<ResolvedPath> compiledFiles)
        {
            NsxModuleBuilder nsxBuilder = GetNsxModuleBuilder(sourceFile);
            nsxBuilder.RealizeAll();
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

    class NsxModuleWriter : IDisposable
    {
        private static ReadOnlySpan<byte> NsxMagic => new byte[] { 0x4E, 0x53, 0x58, 0x00 };
        private static ReadOnlySpan<byte> MemTableMarker => new byte[] { 0x4D, 0x45, 0x4D, 0x00 };
        private static ReadOnlySpan<byte> StringTableMarker => new byte[] { 0x53, 0x54, 0x52, 0x00 };

        private readonly NsxModuleBuilder _builder;
        private readonly FileStream _fileStream;
        private BufferWriter _smallBuffer;

        public NsxModuleWriter(NsxModuleBuilder builder)
        {
            _builder = builder;
            _fileStream = File.Create($"S:/ChaosContent/Noah/nsx/{Path.ChangeExtension(_builder.SourceFile.Name, "nsx")}");
            _smallBuffer = new BufferWriter(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Skip(int nbBytes)
        {
            _fileStream.Seek(nbBytes, SeekOrigin.Current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUInt16(ushort value)
        {
            _smallBuffer.WrittenCount = 0;
            _smallBuffer.WriteUInt16LE(value);
            _fileStream.Write(_smallBuffer.Written);
        }

        public void Write()
        {
            _fileStream.Write(NsxMagic);
            Skip(16);

            // Reserve space for the member table
            int memberTableSize = _builder.Members.Length * 4 + 8;
            int memberTableOffset = (int)_fileStream.Position;
            Skip(memberTableSize);

            // Reserve space for the string table
            int stringTableOffset = (int)_fileStream.Position;
            int stringTableSize = _builder.StringHeap.Length * 4 + 8;
            Skip(stringTableSize);

            var memberOffsets = new BufferWriter((uint)memberTableSize);
            SourceFileSymbol sourceFile = _builder.SourceFile;
            WriteMemberBodies(sourceFile.Chapters, ref memberOffsets);
            WriteMemberBodies(sourceFile.Scenes, ref memberOffsets);
            WriteMemberBodies(sourceFile.Functions, ref memberOffsets);

            var stringOffsets = new BufferWriter((uint)stringTableSize);
            WriteStringHeap(ref stringOffsets);

            _fileStream.Seek(memberTableOffset, SeekOrigin.Begin);
            _fileStream.Write(MemTableMarker);
            WriteUInt16((ushort)_builder.Members.Length);
            using (memberOffsets)
            {
                _fileStream.Write(memberOffsets.Written);
            }

            _fileStream.Seek(stringTableOffset, SeekOrigin.Begin);
            _fileStream.Write(StringTableMarker);
            WriteUInt16((ushort)_builder.StringHeap.Length);
            using (stringOffsets)
            {
                _fileStream.Write(stringOffsets.Written);
            }
        }

        private void WriteMemberBodies<T>(ImmutableArray<T> members, ref BufferWriter offsetTable)
            where T : MemberSymbol
        {
            foreach (T member in members)
            {
                var emitter = new Emitter<T>(_builder, member);
                using (BufferWriter bytecode = emitter.Emit())
                {
                    offsetTable.WriteInt32LE((int)_fileStream.Position);
                    _fileStream.Write(bytecode.Written);
                }
            }
        }

        private void WriteStringHeap(ref BufferWriter offsetTable)
        {
            int heapStart = (int)_fileStream.Position;
            using (var buffer = new BufferWriter(4096))
            {
                foreach (string s in _builder.StringHeap)
                {
                    offsetTable.WriteInt32LE(heapStart + buffer.WrittenCount);
                    buffer.WriteStringAsUtf8(s);
                }

                _fileStream.Write(buffer.Written);
            }
        }

        //private static (BufferWriter buffer, int[] memberOffsetPlaceholders) BuildMemberTable(
        //    ReadOnlySpan<MemberSymbol> members)
        //{
        //    var writer = new BufferWriter(4096);
        //    var offsetPlaceholders = new int[members.Length];
        //    writer.WriteStringAsUtf8("MEM\0");
        //    writer.WriteUInt16LE((ushort)members.Length);
        //    for (int i = 0; i < members.Length; i++)
        //    {
        //        MemberSymbol member = members[i];
        //        byte kind = member.Kind switch
        //        {
        //            SymbolKind.Chapter => (byte)0x00,
        //            SymbolKind.Scene => (byte)0x01,
        //            SymbolKind.Function => (byte)0x02
        //        };

        //        writer.WriteByte(kind);

        //        offsetPlaceholders[i] = writer.WrittenCount;
        //        writer.WriteInt32LE(0);

        //        writer.WriteStringAsUtf8(member.Name);
        //        if (member is FunctionSymbol function && function.Parameters.Length > 0)
        //        {
        //            ImmutableArray<ParameterSymbol> parameters = function.Parameters;
        //            writer.WriteByte((byte)parameters.Length);
        //            foreach (ParameterSymbol parameter in parameters)
        //            {
        //                writer.WriteStringAsUtf8(parameter.Name);
        //            }
        //        }
        //    }

        //    return (writer, offsetPlaceholders);
        //}

        public void Dispose()
        {
            _smallBuffer.Dispose();
            _fileStream.Dispose();
        }
    }

    class NsxModuleBuilder
    {
        private readonly Compilation _compilation;
        private readonly SourceFileSymbol _sourceFile;
        private readonly DiagnosticBuilder _diagnostics;

        private readonly TokenMap<MemberSymbol> _members;
        private readonly TokenMap<SourceFileSymbol> _externalSourceFiles;
        private readonly TokenMap<string> _stringHeap;

        public NsxModuleBuilder(Compilation compilation, SourceFileSymbol sourceFile)
        {
            _compilation = compilation;
            _sourceFile = sourceFile;
            _diagnostics = new DiagnosticBuilder();
            _stringHeap = new TokenMap<string>(512);
            _members = new TokenMap<MemberSymbol>(sourceFile.MemberCount);
            ConstructMemberMap(sourceFile);
            _externalSourceFiles = new TokenMap<SourceFileSymbol>(8);
        }

        public Compilation Compilation => _compilation;
        public SourceFileSymbol SourceFile => _sourceFile;
        public ReadOnlySpan<MemberSymbol> Members => _members.GetAll();
        public ReadOnlySpan<string> StringHeap => _stringHeap.GetAll();
        public DiagnosticBuilder Diagnostics => _diagnostics;

        private void ConstructMemberMap(SourceFileSymbol sourceFile)
        {
            foreach (ChapterSymbol chapter in sourceFile.Chapters)
            {
                _members.GetOrAddToken(chapter);
            }
            foreach (SceneSymbol scene in sourceFile.Scenes)
            {
                _members.GetOrAddToken(scene);
            }
            foreach (FunctionSymbol function in sourceFile.Functions)
            {
                _members.GetOrAddToken(function);
            }
        }

        public BufferWriter Emit<T>(T symbol) where T : MemberSymbol
        {
            var emitter = new Emitter<T>(this, symbol);
            return emitter.Emit();
        }

        public void RealizeAll()
        {
            foreach (ChapterSymbol chapter in _sourceFile.Chapters)
            {
                var chapterEmitter = new Emitter<ChapterSymbol>(this, chapter);
                chapterEmitter.Emit();
            }
            foreach (SceneSymbol scene in _sourceFile.Scenes)
            {
                var sceneEmitter = new Emitter<SceneSymbol>(this, scene);
                sceneEmitter.Emit();
            }
            foreach (FunctionSymbol function in _sourceFile.Functions)
            {
                var functionEmitter = new Emitter<FunctionSymbol>(this, function);
                functionEmitter.Emit();
            }

            using (var writer = new NsxModuleWriter(this))
            {
                writer.Write();
            }
        }

        public ushort GetExternalModuleToken(SourceFileSymbol sourceFile)
        {
            return _externalSourceFiles.GetOrAddToken(sourceFile);
        }

        public ushort GetMemberToken(MemberSymbol member)
        {
            return _members.GetOrAddToken(member);
        }

        public ushort GetStringToken(string s)
        {
            return _stringHeap.GetOrAddToken(s);
        }
    }

    internal enum LookupResultDiscriminator : byte
    {
        Empty = 0,
        Member,
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
        public readonly MemberSymbol Member;

        [FieldOffset(8)]
        public readonly ParameterSymbol Parameter;

        [FieldOffset(8)]
        public readonly string GlobalVariable;

        public LookupResult(MemberSymbol member) : this()
            => (Discriminator, Member) = (LookupResultDiscriminator.Member, member);

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

    struct Checker<T> where T : MemberSymbol
    {
        private readonly T _member;
        private readonly SourceModuleSymbol _module;
        private readonly Compilation _compilation;
        private readonly DiagnosticBuilder _diagnostics;

        public Checker(T member, DiagnosticBuilder diagnostics)
        {
            _member = member;
            _module = member.DeclaringSourceFile.Module;
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
            ParameterSymbol? parameter = _member.LookupParameter(name);
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
            Debug.WriteLine($"{_member.DeclaringSourceFile.Name}: {identifier.Value}");
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

    struct Emitter<T> where T : MemberSymbol
    {
        private const int JumpInstrSize = sizeof(Opcode) + sizeof(ushort);

        private readonly NsxModuleBuilder _module;
        private readonly T _member;
        private Checker<T> _checker;
        private readonly Compilation _compilation;
        private readonly SourceModuleSymbol _sourceModule;
        private BufferWriter _code;

        private readonly TokenMap<ParameterSymbol>? _parameters;

        private readonly Queue<int> _insertBreaksAt;

        public Emitter(NsxModuleBuilder module, T member)
        {
            _module = module;
            _member = member;
            _checker = new Checker<T>(member, module.Diagnostics);
            _compilation = module.Compilation;
            _sourceModule = _member.DeclaringSourceFile.Module;
            _code = new BufferWriter(256);
            _parameters = null;
            if (member is FunctionSymbol function && function.Parameters.Length > 0)
            {
                _parameters = new TokenMap<ParameterSymbol>();
            }

            _insertBreaksAt = new Queue<int>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitOpcode(Opcode opcode)
            => _code.WriteByte((byte)opcode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetGlobalVarToken(string variableName)
            => _compilation.GetGlobalVarToken(variableName);

        public BufferWriter Emit()
        {
            EmitStatement(_member.Declaration.Body);
            return _code;
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
                var function = (FunctionSymbol)lookupResult.Member;
                if (ReferenceEquals(function.DeclaringSourceFile, _member.DeclaringSourceFile))
                {
                    EmitOpcode(Opcode.Call);
                    _code.WriteUInt16LE(_module.GetMemberToken(function));
                }
                else
                {
                    EmitOpcode(Opcode.CallFar);
                    SourceFileSymbol externalSourceFile = function.DeclaringSourceFile;
                    NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
                    _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
                    _code.WriteUInt16LE(externalNsxBuilder.GetMemberToken(function));
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
            _code.WriteUInt16LE(externalNsxBuilder.GetMemberToken(chapter));
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
                    EmitDialogueBlock((DialogueBlockSyntax)statement);
                    break;
            }
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

                int jmpInstrOffset = _code.WrittenCount;
                _code.WrittenCount += JumpInstrSize;
                EmitStatement(ifStmt.IfTrueStatement);
                int end = _code.WrittenCount;
                _code.WrittenCount = jmpInstrOffset;
                EmitOpcode(Opcode.JumpIfFalse);
                _code.WriteInt16LE((short)(end - jmpInstrOffset));
                _code.WrittenCount = end;
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

                int firstJmpInstrOffset = _code.WrittenCount;
                _code.WrittenCount += JumpInstrSize;

                EmitStatement(ifStmt.IfTrueStatement);

                int secondJmpInstrOffset = _code.WrittenCount;
                _code.WrittenCount += JumpInstrSize;
                int alternativePos = _code.WrittenCount;

                EmitStatement(ifStmt.IfFalseStatement);

                int end = _code.WrittenCount;
                int secondJmpTargetOffset = end - secondJmpInstrOffset;
                _code.WrittenCount = secondJmpInstrOffset;
                EmitOpcode(Opcode.Jump);
                _code.WriteInt16LE((short)secondJmpTargetOffset);

                _code.WrittenCount = firstJmpInstrOffset;
                EmitOpcode(Opcode.JumpIfFalse);
                _code.WriteInt16LE((short)(alternativePos - firstJmpInstrOffset));
                _code.WrittenCount = end;
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

            int conditionPos = _code.WrittenCount;
            EmitExpression(whileStmt.Condition);
            int firstJmpOffset = _code.WrittenCount;
            _code.WrittenCount += JumpInstrSize;

            EmitStatement(whileStmt.Body);
            EmitOpcode(Opcode.Jump);
            _code.WriteInt16LE((short)(conditionPos - _code.WrittenCount));
            int exit = _code.WrittenCount;

            _code.WrittenCount = firstJmpOffset;
            EmitOpcode(Opcode.JumpIfFalse);
            _code.WriteInt16LE((short)(exit - firstJmpOffset));
            _code.WrittenCount = exit;

            while (_insertBreaksAt.Count > 0)
            {
                int pos = _insertBreaksAt.Dequeue();
                _code.WrittenCount = pos;
                _code.WriteInt16LE((short)(exit - pos));
            }

            _code.WrittenCount = exit;
        }

        private void EmitBreakStatement(BreakStatementSyntax statement)
        {
            _insertBreaksAt.Enqueue(_code.WrittenCount);
            _code.WrittenCount += JumpInstrSize;
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

        public ReadOnlySpan<T> GetAll() => _items.AsReadonlySpan();

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
