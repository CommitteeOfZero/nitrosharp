using System;
using System.Runtime.InteropServices;

using FT_Long = System.IntPtr;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BBox : IEquatable<BBox>
    {
        private readonly FT_Long _xMin, _yMin;
        private readonly FT_Long _xMax, _yMax;

        public BBox(int left, int bottom, int right, int top)
        {
            _xMin = (IntPtr)left;
            _yMin = (IntPtr)bottom;
            _xMax = (IntPtr)right;
            _yMax = (IntPtr)top;
        }

        public int Left => (int)_xMin;
        public int Bottom => (int)_yMin;
        public int Right => (int)_xMax;
        public int Top => (int)_yMax;

        public static bool operator ==(BBox left, BBox right) => left.Equals(right);
        public static bool operator !=(BBox left, BBox right) => !left.Equals(right);

        public bool Equals(BBox other)
        {
            return
                _xMin == other._xMin &&
                _yMin == other._yMin &&
                _xMax == other._xMax &&
                _yMax == other._yMax;
        }

        public override bool Equals(object obj) => obj is BBox bbox && Equals(bbox);

        public override int GetHashCode()
        {
            return _xMin.GetHashCode() ^ _yMin.GetHashCode() ^ _xMax.GetHashCode() ^ _yMax.GetHashCode();
        }

        public override string ToString()
        {
            return "Min: (" + (int)_xMin + ", " + (int)_yMin + "), Max: (" + (int)_xMax + ", " + (int)_yMax + ")";
        }
    }
}
