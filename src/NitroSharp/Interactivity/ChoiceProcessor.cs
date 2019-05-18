using NitroSharp.Primitives;
using System.Numerics;
using Veldrid;

namespace NitroSharp.Interactivity
{
    internal readonly struct ChoiceProcessorOutput
    {
        public readonly string SelectedChoice;

        public ChoiceProcessorOutput(string selectedChoice)
        {
            SelectedChoice = selectedChoice;
        }
    }

    internal sealed class ChoiceProcessor : GameSystem
    {
        private readonly World _world;
        private readonly InputTracker _inputTracker;

        public ChoiceProcessor(Game.Presenter presenter) : base(presenter)
        {
            _world = presenter.World;
            _inputTracker = presenter.InputTracker;
        }

        public ChoiceProcessorOutput ProcessChoices()
        {
            var input = _inputTracker.CurrentSnapshot;

            var choices = _world.Choices;
            var mouseNormalVisual = choices.MouseUsualSprite.Enumerate();
            var mouseOverSprite = choices.MouseOverSprite.Enumerate();
            var mouseOverThread = choices.MouseOverThread.Enumerate();
            var mouseLeaveThread = choices.MouseLeaveThread.Enumerate();
            var choiceRects = choices.Rects.MutateAll();
            var state = choices.State.MutateAll();

            for (int i = 0; i < choiceRects.Length; i++)
            {
                if (mouseNormalVisual[i].IsValid)
                {
                    Entity mouseNormalEntity = mouseNormalVisual[i];
                    var table = _world.GetTable<RenderItemTable>(mouseNormalEntity);
                    Vector3 translation = table.TransformMatrices.GetValue(mouseNormalEntity).Translation;
                    Vector2 position = new Vector2(translation.X, translation.Y);
                    SizeF bounds = table.Bounds.GetValue(mouseNormalEntity);
                    choiceRects[i] = new Primitives.RectangleF(position, bounds);
                }
                else if (mouseOverSprite[i].IsValid)
                {
                    Entity mouseOverEntity = mouseOverSprite[i];
                    var table = _world.GetTable<RenderItemTable>(mouseOverEntity);
                    Vector3 translation = table.TransformMatrices.GetValue(mouseOverEntity).Translation;
                    Vector2 position = new Vector2(translation.X, translation.Y);
                    SizeF bounds = table.Bounds.GetValue(mouseOverEntity);
                    choiceRects[i] = new Primitives.RectangleF(position, bounds);
                }
            }

            var threads = _world.Threads;

            bool isMouseDown = _inputTracker.IsMouseButtonDownThisFrame(MouseButton.Left);
            var mouseDownSprite = choices.MouseClickSprite.Enumerate();

            int idxDown = -1;
            int maxPriority = -1;
            for (int i = 0; i < choiceRects.Length; i++)
            {
                Entity mouseNormalEntity = mouseNormalVisual[i];
                Entity mouseOverEntity = mouseOverSprite[i];
                Entity mouseDownEntity = mouseDownSprite[i];

                var mouseNormalTable = mouseNormalEntity.IsValid ? _world.GetTable<RenderItemTable>(mouseNormalEntity) : null;
                var mouseOverTable = mouseOverEntity.IsValid ? _world.GetTable<RenderItemTable>(mouseOverEntity) : null;
                var mouseDownTable = mouseDownEntity.IsValid ? _world.GetTable<RenderItemTable>(mouseDownEntity) : null;

                bool isMouseOver = choiceRects[i].Contains(input.MousePosition);
                if (isMouseOver && mouseOverSprite[i].IsValid)
                {
                    if (state[i] == State.Normal && mouseNormalTable != null)
                    {
                        // MouseEnter
                        mouseNormalTable.Colors.Mutate(mouseNormalEntity).SetAlpha(0);
                        if (mouseLeaveThread[i].IsValid)
                        {
                            TerminateThread(threads, mouseLeaveThread[i]);
                        }
                        if (mouseOverThread[i].IsValid)
                        {
                            StartThread(threads, mouseOverThread[i]);
                        }

                        mouseOverTable.Colors.Mutate(mouseOverEntity).SetAlpha(1);
                        state[i] = State.MouseOver;
                    }
                }
                else if (!isMouseOver && state[i] == State.MouseOver)
                {
                    // Mouse leave
                    mouseOverTable.Colors.Mutate(mouseOverEntity).SetAlpha(0);
                    if (mouseLeaveThread[i].IsValid)
                    {
                        StartThread(threads, mouseLeaveThread[i]);
                    }
                    if (mouseOverThread[i].IsValid)
                    {
                        TerminateThread(threads, mouseOverThread[i]);
                    }

                    mouseNormalTable.Colors.Mutate(mouseNormalEntity).SetAlpha(1);
                    state[i] = State.Normal;
                }

                if (isMouseOver && isMouseDown)
                {
                    // Mouse down
                    if (mouseDownSprite[i].IsValid)
                    {
                        mouseNormalTable.Colors.Mutate(mouseNormalEntity).SetAlpha(0);
                        mouseOverTable.Colors.Mutate(mouseOverEntity).SetAlpha(0);
                        mouseDownTable.Colors.Mutate(mouseDownEntity).SetAlpha(1);
                    }

                    if (mouseLeaveThread[i].IsValid)
                    {
                        TerminateThread(threads, mouseLeaveThread[i]);
                    }
                    if (mouseOverThread[i].IsValid)
                    {
                        TerminateThread(threads, mouseOverThread[i]);
                    }

                    //PostMessage(new Game.SimpleMessage(Game.MessageKind.ResumeMainThread));

                    int priority = mouseOverTable.SortKeys.GetValue(mouseOverEntity).Priority;
                    if (priority > maxPriority)
                    {
                        idxDown = i;
                        maxPriority = priority;
                    }
                }
            }

            return idxDown != -1 ? new ChoiceProcessorOutput(choices.Name.GetValue((ushort)idxDown)) : default;
        }

        private void StartThread(ThreadTable threads, Entity threadEntity)
        {
            InterpreterThreadInfo threadInfo = threads.Infos.GetValue(threadEntity);
            PostMessage(new Game.ThreadActionMessage
            {
                ThreadInfo = threadInfo,
                Action = Game.ThreadActionMessage.ActionKind.StartOrResume
            });
        }

        private void TerminateThread(ThreadTable threads, Entity threadEntity)
        {
            InterpreterThreadInfo threadInfo = threads.Infos.GetValue(threadEntity);
            PostMessage(new Game.ThreadActionMessage
            {
                ThreadInfo = threadInfo,
                Action = Game.ThreadActionMessage.ActionKind.Terminate
            });
        }
    }
}
