using System;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScript.Primitives
{
    public enum NsCoordinateOrigin
    {
        Zero,
        CurrentValue,
        Left,
        Top,
        Right,
        Bottom,
        Center
    }

    public enum NsCoordinateVariant
    {
        Value,
        Inherit
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct NsCoordinate
    {
        public NsCoordinate(
            NsCoordinateVariant variant,
            int value,
            NsCoordinateOrigin origin,
            float anchorPoint)
        {
            Variant = variant;
            Value = value;
            Origin = origin;
            AnchorPoint = anchorPoint;
        }

        public NsCoordinateVariant Variant { get; }
        public int Value { get; }
        public NsCoordinateOrigin Origin { get; }
        public float AnchorPoint { get; }

        public static NsCoordinate WithValue(int value, NsCoordinateOrigin origin, float anchorPoint)
            => new NsCoordinate(NsCoordinateVariant.Value, value, origin, anchorPoint);

        public static NsCoordinate Inherit()
            => new NsCoordinate(NsCoordinateVariant.Inherit, default, default, default);

        public static NsCoordinate FromValue(ConstantValue val) => val.Type switch
        {
            BuiltInType.Integer => WithValue(val.AsInteger()!.Value, NsCoordinateOrigin.Zero, 0),
            BuiltInType.DeltaInteger => WithValue(val.AsDelta()!.Value, NsCoordinateOrigin.CurrentValue, 0),
            BuiltInType.BuiltInConstant => FromConstant(val.AsBuiltInConstant()!.Value),
            _ => throw new ArgumentException("Cannot create a valid NsCoordinate from the provided ConstantValue.")
        };

        public static NsCoordinate FromConstant(BuiltInConstant constant)
        {
            switch (constant)
            {
                case BuiltInConstant.InLeft:
                    return WithValue(0, NsCoordinateOrigin.Left, 0.0f);
                case BuiltInConstant.OnLeft:
                    return WithValue(0, NsCoordinateOrigin.Left, 0.5f);
                case BuiltInConstant.OutLeft:
                case BuiltInConstant.Left:
                    return WithValue(0, NsCoordinateOrigin.Left, 1.0f);

                case BuiltInConstant.InTop:
                    return WithValue(0, NsCoordinateOrigin.Top, 0.0f);
                case BuiltInConstant.OnTop:
                    return WithValue(0, NsCoordinateOrigin.Top, 0.5f);
                case BuiltInConstant.OutTop:
                case BuiltInConstant.Top:
                    return WithValue(0, NsCoordinateOrigin.Top, 1.0f);

                case BuiltInConstant.InRight:
                    return WithValue(0, NsCoordinateOrigin.Right, 1.0f);
                case BuiltInConstant.OnRight:
                    return WithValue(0, NsCoordinateOrigin.Right, 0.5f);
                case BuiltInConstant.OutRight:
                case BuiltInConstant.Right:
                    return WithValue(0, NsCoordinateOrigin.Right, 0.0f);

                case BuiltInConstant.InBottom:
                    return WithValue(0, NsCoordinateOrigin.Bottom, 1.0f);
                case BuiltInConstant.OnBottom:
                    return WithValue(0, NsCoordinateOrigin.Bottom, 0.5f);
                case BuiltInConstant.OutBottom:
                case BuiltInConstant.Bottom:
                    return WithValue(0, NsCoordinateOrigin.Bottom, 0.0f);

                case BuiltInConstant.Center:
                case BuiltInConstant.Middle:
                    return WithValue(0, NsCoordinateOrigin.Center, 0.5f);

                case BuiltInConstant.Inherit:
                    return Inherit();

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(constant));
            }
        }
    }
}
