using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    internal static class PredefinedConstants
    {
        public static ImmutableDictionary<string, NssRelativePosition> Positions { get; }
        public static ImmutableDictionary<string, NssColor> Colors { get; }
        public static ImmutableDictionary<string, NssAction> Actions { get; }

        static PredefinedConstants()
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
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

            Colors = new Dictionary<string, NssColor>(StringComparer.OrdinalIgnoreCase)
            {
                ["BLACK"] = NssColor.Black,
                ["WHITE"] = NssColor.White,
                ["RED"] = NssColor.Red,
                ["GREEN"] = NssColor.Green,
                ["BLUE"] = NssColor.Blue
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

            Actions = new Dictionary<string, NssAction>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lock"] = NssAction.Lock,
                ["UnLock"] = NssAction.Unlock,
                ["Play"] = NssAction.Play
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }
    }

    public struct NssCoordinate
    {
        public NssCoordinate(int value, NssRelativePosition relativeTo = NssRelativePosition.Zero)
        {
            Value = value;
            RelativeTo = relativeTo;
        }

        public int Value { get; }
        public NssRelativePosition RelativeTo { get; }

    }

    public struct NssColor
    {
        public NssColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public static NssColor Black { get; } = new NssColor(0, 0, 0);
        public static NssColor White { get; } = new NssColor(255, 255, 255);
        public static NssColor Red { get; } = new NssColor(255, 0, 0);
        public static NssColor Green { get; } = new NssColor(0, 255, 0);
        public static NssColor Blue { get; } = new NssColor(0, 0, 255);

        public static NssColor FromRgb(int rgb)
        {
            byte r = (byte)((rgb >> 16) & 255);
            byte g = (byte)((rgb >> 8) & 255);
            byte b = (byte)(rgb & 255);
            return new NssColor(r, g, b);
        }
    }

    public enum NssRelativePosition
    {
        Zero,
        Current,

        InLeft,
        OnLeft,
        OutLeft,
        Left,
        InTop,
        OnTop,
        OutTop,
        Top,
        InRight,
        OnRight,
        OutRight,
        Right,
        InBottom,
        OnBottom,
        OutBottom,
        Bottom,
        Center
    }

    public enum AudioKind
    {
        BackgroundMusic,
        SoundEffect
    }

    public enum NssAction
    {
        Unlock,
        Lock,
        Play,
        Other
    }

    public enum TextAlignment
    {
        Bottom
    }
}
