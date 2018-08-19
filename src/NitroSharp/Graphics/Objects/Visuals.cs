using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
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
}
