using System.Numerics;
using System.Runtime.InteropServices;

namespace NitroSharp.Graphics;

[StructLayout(LayoutKind.Auto)]
[Persistable]
internal partial struct Transform
{
    public Vector3 Position;
    public Vector3 Scale;
    public Vector3 Rotation;

    public static Transform Default => new() { Scale = Vector3.One };
}
