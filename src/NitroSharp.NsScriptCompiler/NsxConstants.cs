using System;

namespace NitroSharp.NsScript
{
    internal static class NsxConstants
    {
        public const int NsxHeaderSize = 20;
        public const int TableHeaderSize = 6;

        public static ReadOnlySpan<byte> NsxMagic => new byte[] { 0x4E, 0x53, 0x58, 0x00 };
        public static ReadOnlySpan<byte> SubTableMarker => new byte[] { 0x53, 0x55, 0x42, 0x00 };
        public static ReadOnlySpan<byte> RtiTableMarker => new byte[] { 0x52, 0x54, 0x49, 0x00 };
        public static ReadOnlySpan<byte> ImportTableMarker => new byte[] { 0x49, 0x4D, 0x50, 0x00 };
        public static ReadOnlySpan<byte> StringTableMarker => new byte[] { 0x53, 0x54, 0x52, 0x00 };
        public static ReadOnlySpan<byte> TableEndMarker => new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
    }
}
