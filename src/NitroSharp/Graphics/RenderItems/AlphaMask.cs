using NitroSharp.Content;
using NitroSharp.Saving;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class AlphaMask :  ConstraintBox
    {
        public AlphaMask(
            in ResolvedEntityPath path,
            int priority,
            AssetRef<Texture> texture,
            bool isContainer)
            : base(path, priority, isContainer)
        {
            Texture = texture;
        }

        public AlphaMask(in ResolvedEntityPath path, in ConstraintBoxSaveData saveData)
            : base(path, saveData)
        {
        }

        public AssetRef<Texture> Texture { get; }

        public override EntityKind Kind => EntityKind.AlphaMask;

        public override Size GetUnconstrainedBounds(RenderContext ctx)
            => ctx.Content.GetTextureSize(Texture);

        public override void Dispose()
        {
            base.Dispose();
            Texture.Dispose();
        }

        public new ConstraintBoxSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            IsContainer = IsContainer,
            AlphaMaskPath = Texture.Path
        };
    }
}
