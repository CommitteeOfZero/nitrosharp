using System.Runtime.InteropServices;
using MessagePack;

namespace NitroSharp.NsScript.Primitives;

public enum NsCoordinateKind
{
    Value,
    Alignment,
    Inherit
}

public enum NsAlignment
{
    Left,
    Top,
    Right,
    Bottom,
    Center
}

[StructLayout(LayoutKind.Explicit)]
public readonly record struct NsCoordinate
{
    [FieldOffset(0)]
    public readonly NsCoordinateKind Kind;

    [FieldOffset(4)]
    public readonly float AnchorPoint;

    [FieldOffset(8)]
    public readonly (float pos, bool isRelative) Value;

    [FieldOffset(8)]
    public readonly NsAlignment Alignment;

    private NsCoordinate(NsCoordinateKind kind) : this()
    {
        Kind = kind;
    }

    private NsCoordinate(float value, bool isRelative) : this()
    {
        Kind = NsCoordinateKind.Value;
        Value = (value, isRelative);
    }

    private NsCoordinate(NsAlignment alignment, float anchorPoint) : this()
    {
        Kind = NsCoordinateKind.Alignment;
        Alignment = alignment;
        AnchorPoint = anchorPoint;
    }

    public NsCoordinate(ref MessagePackReader reader)
    {
        reader.ReadArrayHeader();
        Kind = (NsCoordinateKind)reader.ReadInt32();
        (Value, Alignment, AnchorPoint) = (default, default, 0);
        switch (Kind)
        {
            case NsCoordinateKind.Value:
                Value = (reader.ReadSingle(), reader.ReadBoolean());
                break;
            case NsCoordinateKind.Alignment:
                Alignment = (NsAlignment)reader.ReadInt32();
                AnchorPoint = reader.ReadSingle();
                break;
        }
    }

    public void Serialize(ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(3);
        writer.Write((int)Kind);
        switch (Kind)
        {
            case NsCoordinateKind.Value:
                writer.Write(Value.pos);
                writer.Write(Value.isRelative);
                break;
            case NsCoordinateKind.Alignment:
                writer.Write((int)Alignment);
                writer.Write(AnchorPoint);
                break;
        }
    }

    public static NsCoordinate Inherit() => new(NsCoordinateKind.Inherit);

    public static NsCoordinate FromValue(ConstantValue val)
    {
        return val.Type switch
        {
            BuiltInType.Numeric => new NsCoordinate(val.AsNumber()!.Value, isRelative: false),
            BuiltInType.DeltaNumeric => new NsCoordinate(val.AsDeltaNumber()!.Value, isRelative: true),
            BuiltInType.BuiltInConstant => FromConstant(val.AsBuiltInConstant()!.Value),
            _ => throw ThrowHelper.ArgumentInvalid(nameof(val))
        };
    }

    private static NsCoordinate FromConstant(BuiltInConstant constant)
    {
        return constant switch
        {
            BuiltInConstant.InLeft => new NsCoordinate(NsAlignment.Left, 0.0f),
            BuiltInConstant.OnLeft => new NsCoordinate(NsAlignment.Left, 0.5f),
            BuiltInConstant.OutLeft or BuiltInConstant.Left => new NsCoordinate(NsAlignment.Left, 1.0f),
            BuiltInConstant.InTop => new NsCoordinate(NsAlignment.Top, 0.0f),
            BuiltInConstant.OnTop => new NsCoordinate(NsAlignment.Top, 0.5f),
            BuiltInConstant.OutTop or BuiltInConstant.Top => new NsCoordinate(NsAlignment.Top, 1.0f),
            BuiltInConstant.InRight => new NsCoordinate(NsAlignment.Right, 1.0f),
            BuiltInConstant.OnRight => new NsCoordinate(NsAlignment.Right, 0.5f),
            BuiltInConstant.OutRight or BuiltInConstant.Right => new NsCoordinate(NsAlignment.Right, 0.0f),
            BuiltInConstant.InBottom => new NsCoordinate(NsAlignment.Bottom, 1.0f),
            BuiltInConstant.OnBottom => new NsCoordinate(NsAlignment.Bottom, 0.5f),
            BuiltInConstant.OutBottom or BuiltInConstant.Bottom => new NsCoordinate(NsAlignment.Bottom, 0.0f),
            BuiltInConstant.Center or BuiltInConstant.Middle => new NsCoordinate(NsAlignment.Center, 0.5f),
            BuiltInConstant.Inherit => Inherit(),
            _ => throw ThrowHelper.ArgumentOutOfRange(nameof(constant))
        };
    }
}
