using System;
using System.Collections.Generic;
using System.Globalization;

namespace SciAdvNet.NSScript
{
    internal static class PredefinedConstants
    {
        public static Dictionary<string, NssPositionOrigin> Positions { get; private set; }
        public static Dictionary<string, NssColor> Colors { get; private set; }
        public static Dictionary<string, NssEntityAction> Actions { get; private set; }
        public static Dictionary<string, EasingFunction> EasingFunctions { get; private set; }

        public static void Preload()
        {
            Positions = new Dictionary<string, NssPositionOrigin>(StringComparer.OrdinalIgnoreCase)
            {
                ["InLeft"] = NssPositionOrigin.InLeft,
                ["OnLeft"] = NssPositionOrigin.OnLeft,
                ["OutLeft"] = NssPositionOrigin.OutLeft,
                ["Left"] = NssPositionOrigin.Left,
                ["InTop"] = NssPositionOrigin.InTop,
                ["OnTop"] = NssPositionOrigin.OnTop,
                ["OutTop"] = NssPositionOrigin.OutTop,
                ["InRight"] = NssPositionOrigin.InRight,
                ["OnRight"] = NssPositionOrigin.OnRight,
                ["OutRight"] = NssPositionOrigin.OutRight,
                ["Right"] = NssPositionOrigin.Right,
                ["InBottom"] = NssPositionOrigin.InBottom,
                ["OnBottom"] = NssPositionOrigin.OnBottom,
                ["OutBottom"] = NssPositionOrigin.OutBottom,
                ["Bottom"] = NssPositionOrigin.Bottom,
                ["Center"] = NssPositionOrigin.Center,
                ["Middle"] = NssPositionOrigin.Center
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
