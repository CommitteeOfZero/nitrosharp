using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using NitroSharp.Common;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Compiler;

internal sealed class NsxModuleBuilder
{
    private readonly TokenMap<SubroutineSymbol> _subroutines;
    private readonly TokenMap<SourceFileSymbol> _externalSourceFiles;
    private readonly TokenMap<string> _stringHeap;
    private ArrayBuilder<SourceMapping> _sourceMappings = new(initialCapacity: 1024);

    public NsxModuleBuilder(EmitContext emitContext, SourceFileSymbol sourceFile)
    {
        EmitContext = emitContext;
        SourceFile = sourceFile;
        _stringHeap = new TokenMap<string>(512);
        _subroutines = new TokenMap<SubroutineSymbol>(sourceFile.SubroutineCount);
        _externalSourceFiles = new TokenMap<SourceFileSymbol>();
        ConstructSubroutineMap(sourceFile);
    }

    public SourceFileSymbol SourceFile { get; }
    public EmitContext EmitContext { get; }

    private ReadOnlySpan<SubroutineSymbol> Subroutines => _subroutines.AsSpan();
    private ReadOnlySpan<SourceFileSymbol> Imports => _externalSourceFiles.AsSpan();
    private ReadOnlySpan<string> StringHeap => _stringHeap.AsSpan();
    private Span<SourceMapping> SourceMappings => _sourceMappings.AsSpan();

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

    public void Emit(Stream outputStream)
    {
        // Compile subroutines
        using var codeBuffer = PooledBuffer<byte>.Allocate(64 * 1024);
        var codeWriter = new BufferWriter(codeBuffer);
        var subroutineOffsets = new List<int>(Subroutines.Length);
        CompileSubroutines(
            SourceFile.Chapters.As<SubroutineSymbol>(),
            ref codeWriter,
            subroutineOffsets
        );
        CompileSubroutines(
            SourceFile.Scenes.As<SubroutineSymbol>(),
            ref codeWriter,
            subroutineOffsets
        );
        CompileSubroutines(
            SourceFile.Functions.As<SubroutineSymbol>(),
            ref codeWriter,
            subroutineOffsets
        );
        codeWriter.WriteBytes(NsxConstants.TableEndMarker);

        ReadOnlySpan<string> stringHeap = StringHeap;
        const int subTableOffset = NsxConstants.NsxHeaderSize;
        int subTableSize = NsxConstants.TableHeaderSize + 6 + Subroutines.Length * sizeof(int);
        int stringTableSize = NsxConstants.TableHeaderSize + 6 + stringHeap.Length * sizeof(int);

        // Build the runtime information table (RTI)
        int rtiTableOffset = NsxConstants.NsxHeaderSize + subTableSize;
        using var rtiBuffer = PooledBuffer<byte>.Allocate(8 * 1024);
        var rtiWriter = new BufferWriter(rtiBuffer);
        uint rtiOffsetBlockSize = SourceFile.SubroutineCount * sizeof(ushort);
        using var rtiEntryOffsets = PooledBuffer<byte>.Allocate((int)rtiOffsetBlockSize);
        var rtiOffsetWriter = new BufferWriter(rtiEntryOffsets);
        WriteRuntimeInformation(
            SourceFile.Chapters.As<SubroutineSymbol>(),
            ref rtiWriter,
            ref rtiOffsetWriter
        );
        WriteRuntimeInformation(
            SourceFile.Scenes.As<SubroutineSymbol>(),
            ref rtiWriter,
            ref rtiOffsetWriter
        );
        WriteRuntimeInformation(
            SourceFile.Functions.As<SubroutineSymbol>(),
            ref rtiWriter,
            ref rtiOffsetWriter
        );
        rtiWriter.WriteBytes(NsxConstants.TableEndMarker);

        int rtiSize = NsxConstants.TableHeaderSize + rtiOffsetWriter.Position + rtiWriter.Position;
        Span<byte> rtiHeader = stackalloc byte[NsxConstants.TableHeaderSize];
        var rtiHeaderWriter = new BufferWriter(rtiHeader);
        rtiHeaderWriter.WriteBytes(NsxConstants.RtiTableMarker);
        rtiHeaderWriter.WriteUInt16LE((ushort)(rtiSize - NsxConstants.TableHeaderSize));

        int impTableOffset = rtiTableOffset + rtiSize;

        // Build the import table (IMP)
        ReadOnlySpan<SourceFileSymbol> imports = Imports;
        using var importTable = PooledBuffer<byte>.Allocate(2048);
        var impTableWriter = new BufferWriter(importTable);
        impTableWriter.WriteUInt16LE((ushort)imports.Length);
        foreach (SourceFileSymbol importedFile in imports)
        {
            impTableWriter.WriteLengthPrefixedUtf8String(importedFile.Name);
        }
        impTableWriter.WriteBytes(NsxConstants.TableEndMarker);

        Span<byte> impHeader = stackalloc byte[NsxConstants.TableHeaderSize];
        fillTableHeader(impHeader, NsxConstants.ImportTableMarker, impTableWriter.Position);

        int impTableSize = NsxConstants.TableHeaderSize + impTableWriter.Position;
        int stringTableOffset = impTableOffset + impTableSize;
        int dbgTableOffset = stringTableOffset  + stringTableSize;
        const int dbgEntrySize = 2 * sizeof(int) + sizeof(ushort) * 2;
        int dbgTableSize = NsxConstants.TableHeaderSize + SourceMappings.Length * dbgEntrySize + 6;
        int codeStart = dbgTableOffset + dbgTableSize;

        // Build the subroutine offset table (SUB)
        using var subTable = PooledBuffer<byte>.Allocate(subTableSize);
        var subWriter = new BufferWriter(subTable);
        subWriter.WriteUInt16LE((ushort)Subroutines.Length);
        for (int i = 0; i < Subroutines.Length; i++)
        {
            subWriter.WriteInt32LE(subroutineOffsets[i] + codeStart);
        }
        subWriter.WriteBytes(NsxConstants.TableEndMarker);

        Span<byte> subHeader = stackalloc byte[NsxConstants.TableHeaderSize];
        fillTableHeader(subHeader, NsxConstants.SubTableMarker, subWriter.Position);

        // Encode the strings and build the offset table (STR)
        int stringHeapStart = codeStart + codeWriter.Position;
        using var stringHeapBuffer = PooledBuffer<byte>.Allocate(64 * 1024);
        using var stringOffsetTable = PooledBuffer<byte>.Allocate(stringTableSize);
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
        fillTableHeader(strTableHeader, NsxConstants.StringTableMarker, strTableWriter.Position);

        // Build the debug table (DBG)
        using var dbgTable = PooledBuffer<byte>.Allocate(dbgTableSize);
        var dbgWriter = new BufferWriter(dbgTable);

        dbgWriter.WriteUInt16LE((ushort)SourceMappings.Length);
        foreach ((BytecodeSpan bytecodeSpan, TextSpan textSpan) in SourceMappings)
        {
            dbgWriter.WriteInt32LE(textSpan.Start);
            dbgWriter.WriteInt32LE(textSpan.Length);
            dbgWriter.WriteUInt16LE((ushort)bytecodeSpan.Start);
            dbgWriter.WriteUInt16LE((ushort)bytecodeSpan.Length);
        }

        dbgWriter.WriteBytes(NsxConstants.TableEndMarker);

        Span<byte> dbgHeader = stackalloc byte[NsxConstants.TableHeaderSize];
        fillTableHeader(dbgHeader, NsxConstants.DebugTableMarker, dbgWriter.Position);

        // Build the NSX header
        using var headerBuffer = PooledBuffer<byte>.Allocate(NsxConstants.NsxHeaderSize);
        var headerWriter = new BufferWriter(headerBuffer);
        long modificationTime = EmitContext.Compilation.SourceReferenceResolver
            .GetModificationTimestamp(SourceFile.FilePath);
        headerWriter.WriteBytes(NsxConstants.NsxMagic);
        headerWriter.WriteInt64LE(modificationTime);
        headerWriter.WriteInt32LE(subTableOffset);
        headerWriter.WriteInt32LE(rtiTableOffset);
        headerWriter.WriteInt32LE(impTableOffset);
        headerWriter.WriteInt32LE(stringTableOffset);
        headerWriter.WriteInt32LE(codeStart);

        // --- Write everything to the stream ---
        outputStream.Write(headerWriter.Written);

        outputStream.Write(subHeader);
        outputStream.Write(subWriter.Written);

        outputStream.Write(rtiHeader);
        outputStream.Write(rtiOffsetWriter.Written);
        outputStream.Write(rtiWriter.Written);

        outputStream.Write(impHeader);
        outputStream.Write(impTableWriter.Written);

        outputStream.Write(strTableHeader);
        outputStream.Write(strTableWriter.Written);

        outputStream.Write(dbgHeader);
        outputStream.Write(dbgWriter.Written);

        outputStream.Write(codeWriter.Written);
        outputStream.Write(stringWriter.Written);
        return;

        static void fillTableHeader(Span<byte> buffer, ReadOnlySpan<byte> tableMarker, int tableSize)
        {
            var headerWriter = new BufferWriter(buffer);
            headerWriter.WriteBytes(tableMarker);
            headerWriter.WriteInt32LE(tableSize);
        }
    }

    private void CompileSubroutines(
        ImmutableArray<SubroutineSymbol> subroutines,
        ref BufferWriter writer, List<int> subroutineOffsets)
    {
        if (subroutines.Length == 0) { return; }
        var dialogueBlockOffsets = new List<int>();
        foreach (SubroutineSymbol subroutine in subroutines)
        {
            subroutineOffsets.Add(writer.Position);

            SubroutineDeclaration decl = subroutine.Declaration;
            int dialogueBlockCount = decl.DialogueBlocks.Length;
            int start = writer.Position;
            int offsetBlockSize = sizeof(ushort) + dialogueBlockCount * sizeof(ushort);
            writer.Position += 2 + offsetBlockSize;
            dialogueBlockOffsets.Clear();

            int codeStart = writer.Position;
            Emitter.CompileSubroutine(this, subroutine, ref writer, dialogueBlockOffsets);
            int codeEnd = writer.Position;

            int codeSize = codeEnd - codeStart;
            writer.Position = start;
            writer.WriteUInt16LE((ushort)(offsetBlockSize + codeSize));

            writer.WriteUInt16LE((ushort)dialogueBlockCount);
            for (int i = 0; i < dialogueBlockCount; i++)
            {
                writer.WriteUInt16LE((ushort)dialogueBlockOffsets[i]);
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
                SymbolKind.Chapter => 0x00,
                SymbolKind.Scene => 0x01,
                SymbolKind.Function => 0x02,
                _ => throw ThrowHelper.UnexpectedValueOf<SymbolKind>()
            };
            rtiWriter.WriteByte(kind);
            rtiWriter.WriteLengthPrefixedUtf8String(subroutine.Name);

            SubroutineDeclaration decl = subroutine.Declaration;
            rtiWriter.WriteUInt16LE((ushort)decl.DialogueBlocks.Length);
            foreach (DialogueBlock dialogueBlock in decl.DialogueBlocks)
            {
                rtiWriter.WriteLengthPrefixedUtf8String(dialogueBlock.AssociatedBox);
                rtiWriter.WriteLengthPrefixedUtf8String(dialogueBlock.Name);
            }

            if (subroutine.Kind == SymbolKind.Function)
            {
                var function = (FunctionSymbol)subroutine;
                rtiWriter.WriteByte((byte)function.Parameters.Length);
                foreach (ParameterSymbol parameter in function.Parameters)
                {
                    rtiWriter.WriteLengthPrefixedUtf8String(parameter.Name);
                }
            }
        }
    }
}
