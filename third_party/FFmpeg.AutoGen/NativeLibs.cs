using System;

namespace FFmpeg.AutoGen
{
    public static class NativeLibs
    {
        public static class avcodec
        {
            public const string PortableName = "libavcodec.58";
            public const string WindowsName = "avcodec-58.dll";
            public const string OsxName = "libavcodec.58.dylib";
            public const string LinuxName = "libavcodec.so.58";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }

        public static class avdevice
        {
            public const string PortableName = "libavdevice.58";
            public const string WindowsName = "avdevice-58.dll";
            public const string OsxName = "libavdevice.58.dylib";
            public const string LinuxName = "libavdevice.so.58";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }

        public static class avformat
        {
            public const string PortableName = "libavformat.58";
            public const string WindowsName = "avformat-58.dll";
            public const string OsxName = "libavformat.58.dylib";
            public const string LinuxName = "libavformat.so.58";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }

        public static class avutil
        {
            public const string PortableName = "libavutil.56";
            public const string WindowsName = "avutil-56.dll";
            public const string OsxName = "libavutil.56.dylib";
            public const string LinuxName = "libavutil.so.56";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }

        public static class avfilter
        {
            public const string PortableName = "libavfilter.7";
            public const string WindowsName = "avfilter-7.dll";
            public const string OsxName = "libavfilter.7.dylib";
            public const string LinuxName = "libavfilter.so.7";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }

        public static class swresample
        {
            public const string PortableName = "libswresample.3";
            public const string WindowsName = "swresample-3.dll";
            public const string OsxName = "libswresample.3.dylib";
            public const string LinuxName = "libswresample.so.3";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }

        public static class swscale
        {
            public const string PortableName = "libswscale.5";
            public const string WindowsName = "swscale-5.dll";
            public const string OsxName = "libswscale.5.dylib";
            public const string LinuxName = "libswscale.so.5";

#if WINDOWS
            public const string Name = WindowsName;
#elif MACOS
            public const string Name = OsxName;
#elif LINUX
            public const string Name = LinuxName;
#else
            public const string Name = PortableName;
#endif
        }
    }
}
