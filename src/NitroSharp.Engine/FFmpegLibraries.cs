using System.IO;
using System.Runtime.InteropServices;

namespace NitroSharp
{
    internal static class FFmpegLibraries
    {
        private static bool _initialized = false;
        private const string RelativePath = "FFmpeg";

        public static void Locate()
        {
            if (!_initialized)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), RelativePath);
                    fullPath = Path.Combine(fullPath, RuntimeInformation.ProcessArchitecture.ToString());
                    SetDllDirectory(fullPath);
                }

                _initialized = true;
            }
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
