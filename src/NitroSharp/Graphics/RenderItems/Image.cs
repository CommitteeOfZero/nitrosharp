using NitroSharp.Saving;

namespace NitroSharp.Graphics
{
    internal sealed class Image : Entity
    {
        public Image(in ResolvedEntityPath path, SpriteTexture texture)
            : base(path)
        {
            Texture = texture;
        }

        public Image(in ResolvedEntityPath path, in ImageSaveData saveData, GameLoadingContext ctx)
            : base(path, saveData.Common)
        {
            Texture = SpriteTexture.FromSaveData(saveData.Texture, ctx);
        }

        public SpriteTexture Texture { get; }
        public override EntityKind Kind => EntityKind.Image;
        public override bool IsIdle => true;

        public override void Dispose()
        {
            Texture.Dispose();
        }

        public new ImageSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            Texture = Texture.ToSaveData(ctx)
        };
    }

    [Persistable]
    internal readonly partial struct ImageSaveData : IEntitySaveData
    {
        public EntitySaveData Common { get; init; }
        public SpriteTextureSaveData Texture { get; init; }

        public EntitySaveData CommonEntityData => Common;
    }
}
