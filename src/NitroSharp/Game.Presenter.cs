using System;
using System.Collections.Generic;
using NitroSharp.Animation;
using NitroSharp.Diagnostics;
using NitroSharp.Dialogue;
using NitroSharp.Graphics.Systems;
using NitroSharp.Interactivity;
using NitroSharp.Media;
using Veldrid;
using NitroSharp.Graphics;
using NitroSharp.Experimental;

#nullable enable

namespace NitroSharp
{
    public partial class Game
    {
        internal sealed class Presenter : Actor, IDisposable
        {
            private readonly World _world;
            private readonly InputTracker _inputTracker;

            private DialogueSystemInput _dialogueSystemInput;
            private readonly DialogueSystem _dialogueSystem;
            private readonly RenderSystem _renderSystem;
            //private readonly AudioSystem _audioSystem;
            private readonly AnimationProcessor _animationProcessor;
            //private readonly ChoiceProcessor _choiceProcessor;

            private readonly DevModeOverlay _devModeOverlay;

            public Presenter(Game game, World world) : base(world)
            {
                _inputTracker = new InputTracker(game._window);
                _dialogueSystem = new DialogueSystem(this, game.GlyphRasterizer, _inputTracker);
                _animationProcessor = new AnimationProcessor(this);
               //_choiceProcessor = new ChoiceProcessor(this);

                _world = world;
                _renderSystem = new RenderSystem(
                    _world, game._graphicsDevice, game._swapchain,
                    game.Content, game.GlyphRasterizer, game._configuration);

                //_audioSystem = new AudioSystem(_world, game.Content, game.AudioSourcePool);

                //_devModeOverlay = new DevModeOverlay(_renderSystem.RenderContext, game.LogEventRecorder);
            }

            public InputTracker InputTracker => _inputTracker;

            public void ProcessNewEntities()
            {
                _renderSystem.ProcessNewEntities();
            }

            public void Tick(in FrameStamp framestamp, float deltaMilliseconds)
            {
                _inputTracker.Update();
                _world.FlushDetachedAnimations();

                AnimationProcessorOutput animProcessorOutput =
                    _animationProcessor.ProcessAnimations(deltaMilliseconds);

                bool blockInput = animProcessorOutput.BlockingAnimationCount > 0;
                //mainThreadWaiting || ThreadAwaitingSelect != null || animProcessorOutput.BlockingAnimationCount > 0;
                _dialogueSystemInput.AcceptUserInput = !blockInput;
                if (_dialogueSystem.AdvanceDialogueState(ref _dialogueSystemInput))
                {
                    _dialogueSystemInput.Command = DialogueSystemCommand.HandleInput;
                }

                _renderSystem.ProcessTransforms();
                //var choiceProcessorOutput = _choiceProcessor.ProcessChoices();
                //if (choiceProcessorOutput.SelectedChoice != null)
                //{
                //    PostMessage(new ChoiceSelectedMessage
                //    {
                //        ChoiceName = choiceProcessorOutput.SelectedChoice
                //    });
                //}

                //_audioSystem.UpdateAudioSources();
                _renderSystem.RenderFrame(framestamp);

                //_devModeOverlay.Tick(deltaMilliseconds, _inputTracker);

                try
                {
                    _renderSystem.Present();
                }
                catch (VeldridException e) when (e.Message == "The Swapchain's underlying surface has been lost.")
                {
                    return;
                }
            }

            protected override void HandleMessages(Queue<Message> messages)
            {
                foreach (Message message in messages)
                {
                    switch (message.Kind)
                    {
                        case MessageKind.BeginDialogueBlock:
                            var beginBlockMsg = (BeginDialogueBlockMessage)message;
                            _dialogueSystemInput.TextEntity = beginBlockMsg.TextEntity;
                            break;
                        case MessageKind.PresentDialogue:
                            var presentDialogueMsg = (PresentDialogueMessage)message;
                            _dialogueSystemInput.Command = DialogueSystemCommand.BeginDialogue;
                            _dialogueSystemInput.TextBuffer = presentDialogueMsg.TextBuffer;
                            break;
                    }
                }
            }

            public void Dispose()
            {
                //_devModeOverlay.Dispose();
            }
        }
    }
}
