using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Graphics.Components;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    internal enum EntityKind : ushort
    {
        Sprite,
        Rectangle,
        Text
    }

    internal abstract class Visuals : EntityTable
    {
        public Row<int> RenderPriorities { get; }
        public Row<RgbaFloat> Colors { get; }
        public Row<SizeF> Bounds { get; }
        public Row<TransformComponents> TransformComponents { get; }
        public Row<Matrix4x4> TransformMatrices { get; }

        public Visuals(ushort columnCount) : base(columnCount)
        {
            TransformComponents = AddRow<TransformComponents>();
            TransformMatrices = AddRow<Matrix4x4>();
            RenderPriorities = AddRow<int>();
            Colors = AddRow<RgbaFloat>();
            Bounds = AddRow<SizeF>();
        }
    }

    internal sealed class Sprites : Visuals
    {
        public Row<SpriteComponent> SpriteComponents { get; }

        public Sprites(ushort spriteCount) : base(spriteCount)
        {
            SpriteComponents = AddRow<SpriteComponent>();
        }
    }

    internal sealed class Rectangles : Visuals
    {
        public Rectangles(ushort rectCount) : base(rectCount)
        {
        }
    }

    internal sealed class TextInstances : Visuals
    {
        public RefTypeRow<TextLayout> Layouts { get; }
        public Row<bool> ClearFlags { get; }

        public TextInstances(ushort initialCount) : base(initialCount)
        {
            Layouts = AddRefTypeRow<TextLayout>();
            ClearFlags = AddRow<bool>();
        }
    }
}
