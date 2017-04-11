using System;
using System.Collections.Generic;
using System.Globalization;

namespace SciAdvNet.NSScript
{
    internal static class PredefinedConstants
    {
        public static Dictionary<string, NssColor> Colors { get; private set; }
        public static Dictionary<string, NssEntityAction> Actions { get; private set; }
        public static Dictionary<string, EasingFunction> EasingFunctions { get; private set; }

        public static Coordinate GetCoordinate(string s)
        {
            switch (s.ToUpperInvariant())
            {
                case "INLEFT":
                    return new Coordinate(0, CoordinateOrigin.Left, 0.0f);
                case "ONLEFT":
                    return new Coordinate(0, CoordinateOrigin.Left, 0.5f);
                case "OUTLEFT":
                case "LEFT":
                    return new Coordinate(0, CoordinateOrigin.Left, 1.0f);

                case "INTOP":
                    return new Coordinate(0, CoordinateOrigin.Top, 0.0f);
                case "ONTOP":
                    return new Coordinate(0, CoordinateOrigin.Top, 0.5f);
                case "OUTTOP":
                case "TOP":
                    return new Coordinate(0, CoordinateOrigin.Top, 1.0f);

                case "INRIGHT":
                    return new Coordinate(0, CoordinateOrigin.Right, 1.0f);
                case "ONRIGHT":
                    return new Coordinate(0, CoordinateOrigin.Right, 0.5f);
                case "OUTRIGHT":
                case "RIGHT":
                    return new Coordinate(0, CoordinateOrigin.Right, 0.0f);

                case "INBOTTOM":
                    return new Coordinate(0, CoordinateOrigin.Bottom, 1.0f);
                case "ONBOTTOM":
                    return new Coordinate(0, CoordinateOrigin.Bottom, 0.5f);
                case "OUTBOTTOM":
                case "BOTTOM":
                    return new Coordinate(0, CoordinateOrigin.Bottom, 0.0f);

                case "CENTER":
                case "MIDDLE":
                    return new Coordinate(0, CoordinateOrigin.Center, 0.5f);

                default:
                    throw new ArgumentException(nameof(s));
            }
        }

        public static void Preload()
        {
            Colors = new Dictionary<string, NssColor>(StringComparer.OrdinalIgnoreCase)
            {
                ["BLACK"] = NssColor.Black,
                ["WHITE"] = NssColor.White,
                ["RED"] = NssColor.Red,
                ["GREEN"] = NssColor.Green,
                ["BLUE"] = NssColor.Blue
            };

            Actions = new Dictionary<string, NssEntityAction>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lock"] = NssEntityAction.Lock,
                ["UnLock"] = NssEntityAction.Unlock,
                ["Play"] = NssEntityAction.Play,
                ["Disused"] = NssEntityAction.Dispose,
                ["Erase"] = NssEntityAction.ResetText,
                ["Hideable"] = NssEntityAction.Hide
            };

            EasingFunctions = new Dictionary<string, EasingFunction>(StringComparer.OrdinalIgnoreCase)
            {
                ["Axl1"] = EasingFunction.QuadraticEaseIn,
                ["Axl2"] = EasingFunction.CubicEaseIn,
                ["Axl3"] = EasingFunction.QuarticEaseIn,
                ["Dxl1"] = EasingFunction.QuadraticEaseOut,
                ["Dxl2"] = EasingFunction.CubicEaseOut,
                ["Dxl3"] = EasingFunction.QuarticEaseOut
            };
        }

        public static NssColor ParseColor(string colorString)
        {
            string strNum = colorString.Substring(1);
            if (int.TryParse(strNum, NumberStyles.HexNumber, null, out int num))
            {
                return NssColor.FromRgb(num);
            }
            else
            {
                return Colors[colorString];
            }
        }
    }
}
