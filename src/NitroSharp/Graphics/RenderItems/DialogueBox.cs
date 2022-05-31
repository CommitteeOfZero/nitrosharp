using System.Diagnostics;
using NitroSharp.Saving;

namespace NitroSharp.Graphics
{
    internal sealed class DialogueBox :  ConstraintBox
    {
        private readonly DesignSizeU _size;

        public DialogueBox(
            in ResolvedEntityPath path,
            int priority,
            DesignSizeU size,
            bool isContainer)
            : base(path, priority, isContainer)
        {
            _size = size;
        }

        public DialogueBox(in ResolvedEntityPath path, in ConstraintBoxSaveData saveData)
             : base(path, saveData)
        {
            Debug.Assert(saveData.Size.HasValue);
            _size = saveData.Size.Value;
        }

        public override EntityKind Kind => EntityKind.DialogueBox;

        public override DesignSize GetUnconstrainedBounds(RenderContext ctx) => _size.ToSizeF();

        public override bool IsIdle => false;

        public new ConstraintBoxSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            IsContainer = IsContainer,
            Size = _size
        };
    }
}
