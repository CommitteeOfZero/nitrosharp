using System;
using System.Runtime.InteropServices;
using MessagePack;

namespace NitroSharp.NsScript.Primitives
{
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
    public readonly struct NsCoordinate
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
            => Kind = kind;

        public NsCoordinate(float value, bool isRelative) : this()
            => (Kind, Value) = (NsCoordinateKind.Value, (value, isRelative));

        public NsCoordinate(NsAlignment alignment, float anchorPoint) : this()
            => (Kind, Alignment, AnchorPoint) = (NsCoordinateKind.Alignment, alignment, anchorPoint);

        public NsCoordinate(ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            Kind = (NsCoordinateKind)reader.ReadInt32();
            (Value, Alignment, AnchorPoint) = (default, default, default);
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

        public static NsCoordinate FromValue(ConstantValue val) => val.Type switch
        {
            BuiltInType.Numeric => new NsCoordinate(val.AsNumber()!.Value, isRelative: false),
            BuiltInType.DeltaNumeric => new NsCoordinate(val.AsDeltaNumber()!.Value, isRelative: true),
            BuiltInType.BuiltInConstant => FromConstant(val.AsBuiltInConstant()!.Value),
            _ => IllegalValue()
        };

        private static NsCoordinate IllegalValue()
            => throw new ArgumentException("Cannot create a valid NsCoordinate" +
                "from the provided ConstantValue.");

        public static NsCoordinate FromConstant(BuiltInConstant constant)
        {
            switch (constant)
            {
                case BuiltInConstant.InLeft:
                    return new NsCoordinate(NsAlignment.Left, 0.0f);
                case BuiltInConstant.OnLeft:
                    return new NsCoordinate(NsAlignment.Left, 0.5f);
                case BuiltInConstant.OutLeft:
                case BuiltInConstant.Left:
                    return new NsCoordinate(NsAlignment.Left, 1.0f);

                case BuiltInConstant.InTop:
                    return new NsCoordinate(NsAlignment.Top, 0.0f);
                case BuiltInConstant.OnTop:
                    return new NsCoordinate(NsAlignment.Top, 0.5f);
                case BuiltInConstant.OutTop:
                case BuiltInConstant.Top:
                    return new NsCoordinate(NsAlignment.Top, 1.0f);

                case BuiltInConstant.InRight:
                    return new NsCoordinate(NsAlignment.Right, 1.0f);
                case BuiltInConstant.OnRight:
                    return new NsCoordinate(NsAlignment.Right, 0.5f);
                case BuiltInConstant.OutRight:
                case BuiltInConstant.Right:
                    return new NsCoordinate(NsAlignment.Right, 0.0f);

                case BuiltInConstant.InBottom:
                    return new NsCoordinate(NsAlignment.Bottom, 1.0f);
                case BuiltInConstant.OnBottom:
                    return new NsCoordinate(NsAlignment.Bottom, 0.5f);
                case BuiltInConstant.OutBottom:
                case BuiltInConstant.Bottom:
                    return new NsCoordinate(NsAlignment.Bottom, 0.0f);

                case BuiltInConstant.Center:
                case BuiltInConstant.Middle:
                    return new NsCoordinate(NsAlignment.Center, 0.5f);

                case BuiltInConstant.Inherit:
                    return Inherit();

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(constant));
            }
        }
    }
}
