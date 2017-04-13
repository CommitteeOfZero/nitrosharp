using System.Collections.Generic;
using System.Collections.Immutable;

namespace MoeGame.Framework.Input
{
    public static class Keyboard
    {
        internal static readonly HashSet<Key> PressedKeys = new HashSet<Key>();
        internal static readonly HashSet<Key> NewlyPressedKeys = new HashSet<Key>();

        public static bool IsKeyDown(Key key) => PressedKeys.Contains(key);
        public static bool IsKeyUp(Key key) => !PressedKeys.Contains(key);

        public static bool IsKeyDownThisFrame(Key key) => NewlyPressedKeys.Contains(key);

        public static ImmutableArray<Key> GetPressedKeys() => PressedKeys.ToImmutableArray();
    }
}
