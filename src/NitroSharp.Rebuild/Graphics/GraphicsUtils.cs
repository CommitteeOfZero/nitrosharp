using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.Graphics
{
    internal static class GraphicsUtils
    {
        public static unsafe void CopyTextureRegion(
            void* src,
            uint srcX, uint srcY, uint srcZ,
            uint srcRowPitch,
            uint srcDepthPitch,
            void* dst,
            uint dstX, uint dstY, uint dstZ,
            uint dstRowPitch,
            uint dstDepthPitch,
            uint width,
            uint height,
            uint depth,
            uint bytesPerPixel)
        {
            uint rowSize = width * bytesPerPixel;
            if (srcRowPitch == dstRowPitch && srcDepthPitch == dstDepthPitch)
            {
                byte* copySrc = (byte*)src
                    + srcDepthPitch * srcZ
                    + srcRowPitch * srcY
                    + bytesPerPixel * srcX;

                byte* copyDst = (byte*)dst
                   + dstDepthPitch * dstZ
                   + dstRowPitch * dstY
                   + bytesPerPixel * dstX;

                uint totalCopySize = depth * srcDepthPitch;
                Buffer.MemoryCopy(copySrc, copyDst, totalCopySize, totalCopySize);
            }
            else
            {
                for (uint z = 0; z < depth; z++)
                {
                    for (uint row = 0; row < height; row++)
                    {
                        byte* rowCopyDst = (byte*)dst
                            + dstDepthPitch * (z + dstZ)
                            + dstRowPitch * (row + dstY)
                            + bytesPerPixel * dstX;

                        byte* rowCopySrc = (byte*)src
                            + srcDepthPitch * (z + srcZ)
                            + srcRowPitch * (row + srcY)
                            + bytesPerPixel * srcX;

                        Unsafe.CopyBlock(rowCopyDst, rowCopySrc, rowSize);
                    }
                }
            }
        }
    }
}
