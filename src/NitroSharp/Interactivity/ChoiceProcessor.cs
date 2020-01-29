using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Experimental;
using NitroSharp.Graphics;
using NitroSharp.Primitives;
using Veldrid;

#nullable enable

namespace NitroSharp.Interactivity
{
    internal sealed class ChoiceProcessor : GameSystem
    {
        internal enum StateTransition
        {
            None,
            Enter,
            Leave
        }

        public ChoiceProcessor(Game.Presenter presenter) : base(presenter)
        {
        }

        public void ProcessChoice(World world, Entity choice, InputTracker inputTracker)
        {
            Vector2 mousePos = inputTracker.CurrentSnapshot.MousePosition;
            bool mouseDown = inputTracker.IsMouseButtonDown(MouseButton.Left);

            var choices = world.GetStorage<ChoiceStorage>(choice);
            ChoiceEntities entities = choices.AssociatedEntities[choice];
            if (world.Exists(entities.DefaultVisual))
            {
                MouseState oldState = choices.MouseState[choice];
                bool mouseOver = getRect(entities.DefaultVisual).Contains(mousePos);
                MouseState newState = (mouseOver, mouseDown) switch
                {
                    (true, true) => MouseState.Pressed,
                    (true, false) => MouseState.Over,
                    _ => MouseState.Normal
                };

                StateTransition transition = (oldState, newState) switch
                {
                    (MouseState.Normal, MouseState.Over) => StateTransition.Enter,
                    (MouseState.Over, MouseState.Normal) => StateTransition.Leave,
                    _ => StateTransition.None
                };

                choices.MouseState[choice] = newState;
                var duration = TimeSpan.FromMilliseconds(200);
                switch (transition)
                {
                    case StateTransition.Enter:
                        fadeOut(entities.DefaultVisual, duration);
                        fadeIn(entities.MouseOverVisual, TimeSpan.Zero);
                        suspendThread(entities.MouseLeaveThread);
                        resumeThread(entities.MouseEnterThread);
                        break;
                    case StateTransition.Leave:
                        fadeOut(entities.MouseOverVisual, duration);
                        fadeIn(entities.DefaultVisual, duration);
                        suspendThread(entities.MouseEnterThread);
                        resumeThread(entities.MouseLeaveThread);
                        break;
                }
            }

            void resumeThread(Entity threadEntity)
                => threadAction(threadEntity, Game.ThreadActionMessage.ActionKind.StartOrResume);

            void suspendThread(Entity threadEntity)
                => threadAction(threadEntity, Game.ThreadActionMessage.ActionKind.Suspend);

            void threadAction(Entity threadEntity, Game.ThreadActionMessage.ActionKind actionKind)
            {
                ThreadRecordStorage? threadRecs;
                if (world.Exists(threadEntity) && (threadRecs = world.
                    GetStorage<ThreadRecordStorage>(threadEntity)) != null)
                {
                    PostMessage(new Game.ThreadActionMessage
                    {
                        ThreadInfo = threadRecs.Infos[threadEntity],
                        Action = actionKind
                    });
                }
            }

            RectangleF getRect(Entity entity)
            {
                var storage = world.GetStorage<QuadStorage>(entity);
                return storage.DesignSpaceRects[entity];
            }

            void fadeIn(Entity entity, TimeSpan duration)
            {
                var anim = new FadeAnimation(entity, duration)
                {
                    InitialOpacity = 0.0f,
                    FinalOpacity = 1.0f
                };
                world.ActivateAnimation(anim);
            }

            void fadeOut(Entity entity, TimeSpan duration)
            {
                var anim = new FadeAnimation(entity, duration)
                {
                    InitialOpacity = 1.0f,
                    FinalOpacity = 0.0f
                };
                world.ActivateAnimation(anim);
            }
        }
    }
}
