using System;
using System.Collections.Generic;
using System.Globalization;

namespace SciAdvNet.NSScript
{
    internal static class PredefinedConstants
    {
        public static Dictionary<string, NssRelativePosition> Positions { get; private set; }
        public static Dictionary<string, NssColor> Colors { get; private set; }
        public static Dictionary<string, NssEntityAction> Actions { get; private set; }
        public static Dictionary<string, EasingFunction> EasingFunctions { get; private set; }

        public static void Preload()
        {
            Positions = new Dictionary<string, NssRelativePosition>(StringComparer.OrdinalIgnoreCase)
            {
                ["InLeft"] = NssRelativePosition.InLeft,
                ["OnLeft"] = NssRelativePosition.OnLeft,
                ["OutLeft"] = NssRelativePosition.OutLeft,
                ["Left"] = NssRelativePosition.Left,
                ["InTop"] = NssRelativePosition.InTop,
                ["OnTop"] = NssRelativePosition.OnTop,
                ["OutTop"] = NssRelativePosition.OutTop,
                ["InRight"] = NssRelativePosition.InRight,
                ["OnRight"] = NssRelativePosition.OnRight,
                ["OutRight"] = NssRelativePosition.OutRight,
                ["Right"] = NssRelativePosition.Right,
                ["InBottom"] = NssRelativePosition.InBottom,
                ["OnBottom"] = NssRelativePosition.OnBottom,
                ["OutBottom"] = NssRelativePosition.OutBottom,
                ["Bottom"] = NssRelativePosition.Bottom,
                ["Center"] = NssRelativePosition.Center,
                ["Middle"] = NssRelativePosition.Center
            };

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
                ["Play"] = NssEntityAction.Play
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
