using NitroSharp.Graphics.Components;

namespace NitroSharp.Graphics
{
    internal sealed class Sprites : Visuals
    {
        public Sprites(ushort spriteCount) : base(spriteCount)
        {
            SpriteComponents = AddRow<SpriteComponent>();
        }

        public Row<SpriteComponent> SpriteComponents { get; }
    }
}
