using System;
using System.Numerics;

namespace NitroSharp.Utilities
{
    internal static class QuaternionExt
    {
        public static Vector3 ToEuler(in Quaternion q)
        {
            // https://code.google.com/p/3d-editor-toolkit/source/browse/trunk/PureCpp/MathCore/Quaternion.cpp

            float w = q.W;
            float x = q.X;
            float y = q.Y;
            float z = q.Z;

            float sqw = w * w;
            float sqx = x * x;
            float sqy = y * y;
            float sqz = z * z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = x * y + z * w;

            var v = Vector3.Zero;
            if (test > 0.499f * unit) // singularity at north pole
            {
                v.Y = 2.0f * (float)Math.Atan2(x, w);
                v.Z = (float)Math.PI / 2.0f;
                v.X = 0.0f;
            }
            else if (test < -0.499f * unit) // singularity at south pole
            {
                v.Y = -2.0f * (float)Math.Atan2(x, w);
                v.Z = -(float)Math.PI / 2.0f;
                v.X = 0;
            }
            else
            {
                v.Y = (float)Math.Atan2(2.0f * y * w - 2.0f * x * z, sqx - sqy - sqz + sqw);
                v.Z = (float)Math.Asin(2.0f * test / unit);
                v.X = (float)Math.Atan2(2.0f * x * w - 2.0f * y * z, -sqx + sqy - sqz + sqw);
            }
            return v;
        }
    }
}
