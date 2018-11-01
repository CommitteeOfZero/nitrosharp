using NitroSharp.Input;
using NitroSharp.NsScript.Execution;
using NitroSharp.NsScript.Symbols;
using NitroSharp.Primitives;
using System.Numerics;

namespace NitroSharp.Interactivity
{
    internal sealed class ChoiceProcessor
    {
        private readonly World _world;
        private readonly InputTracker _inputTracker;
        private readonly NsScriptInterpreter _interpreter;

        public ChoiceProcessor(World world, InputTracker inputTracker, NsScriptInterpreter interpreter)
        {
            _world = world;
            _inputTracker = inputTracker;
            _interpreter = interpreter;
        }

        public void ProcessChoices()
        {
            var input = _inputTracker.CurrentSnapshot;

            SpriteTable sprites = _world.Sprites;
            var matrices = sprites.TransformMatrices.Enumerate();
            var bounds = sprites.Bounds.Enumerate();
            var spriteRects = sprites.WorldRects.MutateAll();
            var parents = sprites.Parents.Enumerate();

            for (int i = 0; i < matrices.Length; i++)
            {
                Vector3 translation = matrices[i].Translation;
                Vector2 position = new Vector2(translation.X, translation.Y);
                spriteRects[i] = new Primitives.RectangleF(position, bounds[i]);
            }

            var choices = _world.Choices;
            var mouseNormal = choices.MouseUsualSprite.Enumerate();
            var choiceRects = choices.Rects.MutateAll();
            var state = choices.State.MutateAll();
            for (int i = 0; i < choiceRects.Length; i++)
            {
                if (mouseNormal[i].IsValid)
                {
                    ushort spriteIndex = sprites.LookupIndex(mouseNormal[i]);
                    choiceRects[i] = spriteRects[spriteIndex];
                }
            }

            var threads = _world.Threads;

            bool mouseDown = input.IsMouseDown(Veldrid.MouseButton.Left);
            var mouseOverSprite = choices.MouseOverSprite.Enumerate();
            var mouseOverThread = choices.MouseOverThread.Enumerate();
            var mouseLeaveThread = choices.MouseLeaveThread.Enumerate();
            var threadNames = _world.Threads.Name.Enumerate();

            for (int i = 0; i < choiceRects.Length; i++)
            {
                bool isMouseOver = choiceRects[i].Contains(input.MousePosition);
                if (isMouseOver && mouseOverSprite[i].IsValid)
                {
                    if (state[i] == State.Normal)
                    {
                        sprites.Colors.Mutate(mouseNormal[i]).SetAlpha(0);
                        if (mouseLeaveThread[i].IsValid)
                        {
                            TerminateThread(threads, mouseLeaveThread[i]);
                        }
                        if (mouseOverThread[i].IsValid)
                        {
                            StartThread(threads, mouseOverThread[i]);
                        }
                    }

                    sprites.Colors.Mutate(mouseOverSprite[i]).SetAlpha(1);
                    state[i] = State.MouseOver;
                }
                else
                {
                    if (state[i] == State.MouseOver)
                    {
                        sprites.Colors.Mutate(mouseOverSprite[i]).SetAlpha(0);
                        if (mouseLeaveThread[i].IsValid)
                        {
                            StartThread(threads, mouseLeaveThread[i]);
                        }
                        if (mouseOverThread[i].IsValid)
                        {
                            TerminateThread(threads, mouseOverThread[i]);
                        }
                    }

                    sprites.Colors.Mutate(mouseNormal[i]).SetAlpha(1);
                    state[i] = State.Normal;
                }
            }
        }

        private void StartThread(ThreadTable threads, Entity threadEntity)
        {
            (string threadName, MergedSourceFileSymbol module, string target) = GetThread(threads, threadEntity);
            if (_interpreter.TryGetThread(threadName, out ThreadContext thread))
            {
                _interpreter.ResumeThread(thread);
            }
            else
            {
                _interpreter.CreateThread(threadName, module, target, start: true);
            }
        }

        private void TerminateThread(ThreadTable threads, Entity threadEntity)
        {
            (string threadName, MergedSourceFileSymbol module, string target) = GetThread(threads, threadEntity);
            if (_interpreter.TryGetThread(threadName, out ThreadContext thread))
            {
                _interpreter.TerminateThread(thread);
            }
        }

        private (string name, MergedSourceFileSymbol module, string target) GetThread(ThreadTable threads, Entity threadEntity)
        {
            ushort threadIndex = threads.LookupIndex(threadEntity);
            string threadName = threads.Name.GetValue(threadIndex);
            MergedSourceFileSymbol module = threads.Module.GetValue(threadIndex);
            string target = threads.Target.GetValue(threadIndex);
            return (threadName, module, target);
        }
    }
}
