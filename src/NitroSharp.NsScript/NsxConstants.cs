using System;

namespace NitroSharp.NsScript;

internal static class NsxConstants
{
    public const int NsxHeaderSize = 32;
    public const int TableHeaderSize = 8;

    public static ReadOnlySpan<byte> NsxMagic => "NSX\0"u8;
    public static ReadOnlySpan<byte> SubTableMarker => "SUB\0"u8;
    public static ReadOnlySpan<byte> RtiTableMarker => "RTI\0"u8;
    public static ReadOnlySpan<byte> ImportTableMarker => "IMP\0"u8;
    public static ReadOnlySpan<byte> StringTableMarker => "STR\0"u8;
    public static ReadOnlySpan<byte> DebugTableMarker => "DBG\0"u8;
    public static ReadOnlySpan<byte> TableEndMarker => [0xFF, 0xFF, 0xFF, 0xFF];
}
