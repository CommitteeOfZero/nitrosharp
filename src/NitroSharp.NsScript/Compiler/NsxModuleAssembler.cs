using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Compiler
{
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
                builder,
                sourceFile.Chapters.As<SubroutineSymbol>(),
                ref codeWriter,
                subroutineOffsets
            );
            CompileSubroutines(
                builder,
                sourceFile.Scenes.As<SubroutineSymbol>(),
                ref codeWriter,
                subroutineOffsets
            );
            CompileSubroutines(
                builder,
                sourceFile.Functions.As<SubroutineSymbol>(),
                ref codeWriter,
                subroutineOffsets
            );
            codeWriter.WriteBytes(NsxConstants.TableEndMarker);

            ReadOnlySpan<string> stringHeap = builder.StringHeap;
            const int subTableOffset = NsxConstants.NsxHeaderSize;
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
                sourceFile.Chapters.As<SubroutineSymbol>(),
                ref rtiWriter,
                ref rtiOffsetWriter
            );
            WriteRuntimeInformation(
                sourceFile.Scenes.As<SubroutineSymbol>(),
                ref rtiWriter,
                ref rtiOffsetWriter
            );
            WriteRuntimeInformation(
                sourceFile.Functions.As<SubroutineSymbol>(),
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

                SubroutineDeclaration decl = subroutine.Declaration;
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
                    SymbolKind.Chapter => (byte)0x00,
                    SymbolKind.Scene => (byte)0x01,
                    SymbolKind.Function => (byte)0x02,
                    _ => ThrowHelper.Unreachable<byte>()
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
}
