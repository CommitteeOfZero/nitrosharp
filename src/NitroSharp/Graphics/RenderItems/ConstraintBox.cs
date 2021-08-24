namespace NitroSharp.Graphics
{
    internal abstract class ConstraintBox : RenderItem2D
    {
        protected ConstraintBox(
            in ResolvedEntityPath path,
            int priority,
            bool isContainer)
            : base(path, priority)
        {
            IsContainer = isContainer;
        }

        protected ConstraintBox(in ResolvedEntityPath path, in ConstraintBoxSaveData saveData)
            : base(path, saveData.Common)
        {
            IsContainer = saveData.IsContainer;
        }

        public bool IsContainer { get; }

        protected override AnimPropagationMode AnimPropagationMode
            => IsContainer ? AnimPropagationMode.All : AnimPropagationMode.None;
    }

    [Persistable]
    internal readonly partial struct ConstraintBoxSaveData : IEntitySaveData
    {
        public RenderItemSaveData Common { get; init; }
        public bool IsContainer { get; init; }
        public Size? Size { get; init; }
        public string? AlphaMaskPath { get; init; }

        public EntitySaveData CommonEntityData => Common.EntityData;
    }
}
