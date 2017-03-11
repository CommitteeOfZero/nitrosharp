using System.Runtime.InteropServices;

namespace SciAdvNet.MediaLayer
{
    internal static class FFmpegHelper
    {
        private const string RelativePath = "FFmpeg";

        public static void SetLibrariesPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetDllDirectory(RelativePath);
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
