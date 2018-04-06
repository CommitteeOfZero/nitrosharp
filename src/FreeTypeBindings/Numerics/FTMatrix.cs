using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    /// <summary>
    /// A simple structure used to store a 2x2 matrix. Coefficients are in 16.16 fixed float format. The computation
    /// performed is:
    /// <code>
    /// x' = x*xx + y*xy
    /// y' = x*yx + y*yy
    /// </code>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
	public struct FTMatrix : IEquatable<FTMatrix>
	{
		private IntPtr _xx, _xy;
		private IntPtr _yx, _yy;


		public FTMatrix(int xx, int xy, int yx, int yy)
		{
            _xx = (IntPtr)xx;
            _xy = (IntPtr)xy;
            _yx = (IntPtr)yx;
            _yy = (IntPtr)yy;
		}

		public FTMatrix(FTVector row0, FTVector row1)
			: this(row0.X.Value, row0.Y.Value, row1.X.Value, row1.Y.Value)
		{
		}

		public Fixed16Dot16 XX
        {
            get => Fixed16Dot16.FromRawValue((int)_xx);
            set => _xx = (IntPtr)value.Value;
        }

        public Fixed16Dot16 XY
        {
            get => Fixed16Dot16.FromRawValue((int)_xy);
            set => _xy = (IntPtr)value.Value;
        }

        public Fixed16Dot16 YX
        {
            get => Fixed16Dot16.FromRawValue((int)_yx);
            set => _yx = (IntPtr)value.Value;
        }

        public Fixed16Dot16 YY
        {
            get => Fixed16Dot16.FromRawValue((int)_yy);
            set => _yy = (IntPtr)value.Value;
        }

        public static bool operator ==(FTMatrix left, FTMatrix right) => left.Equals(right);
        public static bool operator !=(FTMatrix left, FTMatrix right) => !left.Equals(right);

        public static void Multiply(FTMatrix a, FTMatrix b) => FT.FT_Matrix_Multiply(ref a, ref b);
        public void Multiply(FTMatrix b) => FT.FT_Matrix_Multiply(ref this, ref b);

        public void Invert()
		{
			Error err = FT.FT_Matrix_Invert(ref this);
			if (err != Error.Ok)
            {
                throw new FreeTypeException(err);
            }
        }

		public bool Equals(FTMatrix other)
		{
			return
				_xx == other._xx &&
				_xy == other._xy &&
				_yx == other._yx &&
				_yy == other._yy;
		}

        public override bool Equals(object obj) => obj is FTMatrix matrix && Equals(matrix);
		public override int GetHashCode()
		{
			return _xx.GetHashCode() ^ _xy.GetHashCode() ^ _yx.GetHashCode() ^ _yy.GetHashCode();
		}
	}
}
