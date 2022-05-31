using System.Numerics;
using NitroSharp.Saving;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ColorSource : Entity
    {
        public ColorSource(in ResolvedEntityPath path, in RgbaFloat color, DesignSizeU size)
            : base(path)
        {
            Color = color;
            Size = size;
        }

        public ColorSource(in ResolvedEntityPath path, in ColorSourceSaveData saveData)
            : base(path, saveData.Common)
        {
            Color = new RgbaFloat(saveData.Color);
            Size = saveData.Size;
        }

        public RgbaFloat Color { get; }
        public DesignSizeU Size { get; }

        public override EntityKind Kind => EntityKind.ColorSource;
        public override bool IsIdle => true;

        public new ColorSourceSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            Color = Color.ToVector4(),
            Size = Size
        };
    }

    [Persistable]
    internal readonly partial struct ColorSourceSaveData : IEntitySaveData
    {
        public EntitySaveData Common { get; init; }
        public Vector4 Color { get; init; }
        public DesignSizeU Size { get; init; }

        public EntitySaveData CommonEntityData => Common;
    }
}
