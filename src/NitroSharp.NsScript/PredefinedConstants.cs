using System;

namespace NitroSharp.NsScript
{
    public static class PredefinedConstants
    {
        public static NsCoordinate Coordinate(string s)
        {
            switch (s.ToUpperInvariant())
            {
                case "INLEFT":
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 0.0f);
                case "ONLEFT":
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 0.5f);
                case "OUTLEFT":
                case "LEFT":
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 1.0f);

                case "INTOP":
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 0.0f);
                case "ONTOP":
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 0.5f);
                case "OUTTOP":
                case "TOP":
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 1.0f);

                case "INRIGHT":
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 1.0f);
                case "ONRIGHT":
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 0.5f);
                case "OUTRIGHT":
                case "RIGHT":
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 0.0f);

                case "INBOTTOM":
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 1.0f);
                case "ONBOTTOM":
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 0.5f);
                case "OUTBOTTOM":
                case "BOTTOM":
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 0.0f);

                case "CENTER":
                case "MIDDLE":
                    return new NsCoordinate(0, NsCoordinateOrigin.Center, 0.5f);

                default:
                    throw IllegalValue(nameof(s));
            }
        }

        public static bool TryGetColor(string colorName, out NsColor color)
        {
            switch (colorName.ToUpperInvariant())
            {
                case "BLACK":
                    color = NsColor.Black;
                    break;
                case "WHITE":
                    color = NsColor.White;
                    break;
                case "RED":
                    color = NsColor.Red;
                    break;
                case "GREEN":
                    color = NsColor.Green;
                    break;
                case "BLUE":
                    color = NsColor.Blue;
                    break;

                default:
                    color = default(NsColor);
                    return false;
            }

            return true;
        }

        private static Exception IllegalValue(string paramName)
        {
            throw new ArgumentException("Illegal value.", paramName);
        }

        public static NsEasingFunction EasingFunction(string functionName)
        {
            switch (functionName.ToUpperInvariant())
            {
                case "AXL1":
                    return NsEasingFunction.QuadraticEaseIn;
                case "AXL2":
                    return NsEasingFunction.CubicEaseIn;
                case "AXL3":
                    return NsEasingFunction.QuarticEaseIn;

                case "DXL1":
                    return NsEasingFunction.QuadraticEaseOut;
                case "DXL2":
                    return NsEasingFunction.CubicEaseOut;
                case "DXL3":
                    return NsEasingFunction.QuarticEaseOut;

                default:
                    throw IllegalValue(nameof(functionName));
            }
        }

        public static NsEntityAction EntityAction(string actionName)
        {
            switch (actionName.ToUpperInvariant())
            {
                case "LOCK":
                    return NsEntityAction.Lock;
                case "UNLOCK":
                    return NsEntityAction.Unlock;
                case "PLAY":
                    return NsEntityAction.Play;
                case "DISUSED":
                    return NsEntityAction.Dispose;
                case "ERASE":
                    return NsEntityAction.ResetText;
                case "HIDEABLE":
                    return NsEntityAction.Hide;

                default:
                    return NsEntityAction.Other;
            }
        }
    }
}
