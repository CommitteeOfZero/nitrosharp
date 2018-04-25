using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FTVector26Dot6 : IEquatable<FTVector26Dot6>
    {
        private readonly IntPtr _x, _y;

        public FTVector26Dot6(Fixed26Dot6 x, Fixed26Dot6 y)
        {
            _x = (IntPtr)x.Value;
            _y = (IntPtr)y.Value;
        }

        internal FTVector26Dot6(IntPtr reference)
        {
            _x = Marshal.ReadIntPtr(reference);
            _y = Marshal.ReadIntPtr(reference, IntPtr.Size);
        }

        public Fixed26Dot6 X => Fixed26Dot6.FromRawValue((int)_x);
        public Fixed26Dot6 Y => Fixed26Dot6.FromRawValue((int)_y);

        public static bool operator ==(FTVector26Dot6 left, FTVector26Dot6 right) => left.Equals(right);
        public static bool operator !=(FTVector26Dot6 left, FTVector26Dot6 right) => !left.Equals(right);

        public bool Equals(FTVector26Dot6 other) => _x == other._x && _y == other._y;
        public override bool Equals(object obj) => obj is FTVector26Dot6 vector && Equals(vector);

        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode();
        }
    }
}
