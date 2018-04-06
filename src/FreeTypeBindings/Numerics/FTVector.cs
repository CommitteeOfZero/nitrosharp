using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FTVector : IEquatable<FTVector>
    {
        private IntPtr _x;
        private IntPtr _y;

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

        public Fixed16Dot16 X
        {
            get => Fixed16Dot16.FromRawValue((int)_x);
            set => _x = (IntPtr)value.Value;
        }

        public Fixed16Dot16 Y
        {
            get => Fixed16Dot16.FromRawValue((int)_y);
            set => _y = (IntPtr)value.Value;
        }

        public static bool operator ==(FTVector left, FTVector right) => left.Equals(right);
        public static bool operator !=(FTVector left, FTVector right) => !left.Equals(right);

        public static FTVector Unit(Fixed16Dot16 angle)
        {
            FTVector vec;
            FT.FT_Vector_Unit(out vec, (IntPtr)angle.Value);

            return vec;
        }

        public static FTVector FromPolar(Fixed16Dot16 length, Fixed16Dot16 angle)
        {
            FTVector vec;
            FT.FT_Vector_From_Polar(out vec, (IntPtr)length.Value, (IntPtr)angle.Value);

            return vec;
        }

        public void Transform(FTMatrix matrix) => FT.FT_Vector_Transform(ref this, ref matrix);
        public void Rotate(Fixed16Dot16 angle) => FT.FT_Vector_Rotate(ref this, (IntPtr)angle.Value);

        public Fixed16Dot16 Length() => Fixed16Dot16.FromRawValue((int)FT.FT_Vector_Length(ref this));

        public void Polarize(out Fixed16Dot16 length, out Fixed16Dot16 angle)
        {
            IntPtr tmpLength, tmpAngle;
            FT.FT_Vector_Polarize(ref this, out tmpLength, out tmpAngle);

            length = Fixed16Dot16.FromRawValue((int)tmpLength);
            angle = Fixed16Dot16.FromRawValue((int)tmpAngle);
        }

        public bool Equals(FTVector other) => _x == other._x && _y == other._y;
        public override bool Equals(object obj) => obj is FTVector vector && Equals(vector);

        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode();
        }
    }
}
