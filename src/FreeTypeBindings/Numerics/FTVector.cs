using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FTVector : IEquatable<FTVector>
    {
        private readonly IntPtr _x;
        private readonly IntPtr _y;

        public FTVector(Fixed16Dot16 x, Fixed16Dot16 y)
        {
            _x = (IntPtr)x.Value;
            _y = (IntPtr)y.Value;
        }

        internal FTVector(IntPtr reference)
        {
            _x = Marshal.ReadIntPtr(reference);
            _y = Marshal.ReadIntPtr(reference, IntPtr.Size);
        }

        public Fixed16Dot16 X => Fixed16Dot16.FromRawValue((int)_x);
        public Fixed16Dot16 Y => Fixed16Dot16.FromRawValue((int)_y);

        public static bool operator ==(FTVector left, FTVector right) => left.Equals(right);
        public static bool operator !=(FTVector left, FTVector right) => !left.Equals(right);

        public FTVector Transform(FTMatrix matrix)
        {
            FTVector copy = this;
            FT.FT_Vector_Transform(ref copy, ref matrix);
            return copy;
        }

        public FTVector Rotate(Fixed16Dot16 angle)
        {
            FTVector copy = this;
            FT.FT_Vector_Rotate(ref copy, (IntPtr)angle.Value);
            return copy;
        }

        public bool Equals(FTVector other) => _x == other._x && _y == other._y;
        public override bool Equals(object obj) => obj is FTVector vector && Equals(vector);

        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode();
        }
    }
}
