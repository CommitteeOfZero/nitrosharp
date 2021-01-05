using System.Diagnostics;
using NitroSharp.Saving;

namespace NitroSharp.Graphics
{
    internal sealed class DialogueBox :  ConstraintBox
    {
        private readonly Size _size;

        public DialogueBox(
            in ResolvedEntityPath path,
            int priority,
            Size size,
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

        public override Size GetUnconstrainedBounds(RenderContext ctx) => _size;

        public new ConstraintBoxSaveData ToSaveData(GameSavingContext ctx)
        {
            return new()
            {
                Common = base.ToSaveData(ctx),
                IsContainer = IsContainer,
                Size = _size
            };
        }
    }
}
