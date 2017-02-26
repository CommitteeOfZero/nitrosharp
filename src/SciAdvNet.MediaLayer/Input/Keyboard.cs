using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.MediaLayer.Input
{
    public static class Keyboard
    {
        internal static List<Key> PressedKeys = new List<Key>();

        public static bool IsKeyDown(Key key) => PressedKeys.Contains(key);
        public static bool IsKeyUp(Key key) => !PressedKeys.Contains(key);

        public static ImmutableArray<Key> GetPressedKeys() => PressedKeys.ToImmutableArray();
    }
}
