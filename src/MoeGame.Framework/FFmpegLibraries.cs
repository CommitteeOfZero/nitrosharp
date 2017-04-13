using FFmpeg.AutoGen;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MoeGame.Framework
{
    internal static class FFmpegLibraries
    {
        private static bool _initialized = false;
        private const string RelativePath = "FFmpeg";

        public static void Init()
        {
            if (!_initialized)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), RelativePath);
                    SetDllDirectory(fullPath);
                }

                ffmpeg.av_register_all();
                ffmpeg.avcodec_register_all();

                _initialized = true;
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
