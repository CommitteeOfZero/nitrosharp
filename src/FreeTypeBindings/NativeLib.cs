namespace FreeTypeBindings
{
    public static class NativeLib
    {
        public const string PortableName = "libfreetype";
        public const string WindowsName = "freetype.dll";
        public const string OsxName = "libfreetype.dylib";
        public const string LinuxName = "libfreetype.so.6";

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
