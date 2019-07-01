namespace OpenAL
{
    public static class NativeLib
    {
        public const string PortableName = "libopenal";
        public const string WindowsName = "OpenAL32.dll";
        public const string OsxName = "libopenal.dylib";
        public const string LinuxName = "libopenal.so.1";

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
