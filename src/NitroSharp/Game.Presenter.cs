using System;
using System.Collections.Generic;
using NitroSharp.Animation;
using NitroSharp.Diagnostics;
using NitroSharp.Dialogue;
using NitroSharp.Interactivity;
using NitroSharp.Media;
using Veldrid;
using NitroSharp.Graphics;
using NitroSharp.Experimental;
using NitroSharp.Content;

#nullable enable

namespace NitroSharp
{
    public partial class Game
    {
        internal sealed class Presenter : Actor, IDisposable
        {
            private readonly World _world;
            private readonly ContentManager _content;
            private readonly InputTracker _inputTracker;

            private DialogueSystemInput _dialogueSystemInput;
            private bool _captureFramebuffer;
            private readonly DialogueSystem _dialogueSystem;
            private readonly Renderer _renderSystem;
            //private readonly AudioSystem _audioSystem;
            private readonly AnimationProcessor _animationProcessor;
            private readonly ChoiceProcessor _choiceProcessor;

            private readonly DevModeOverlay _devModeOverlay;

            public Presenter(Game game, World world) : base(world)
            {
                _world = world;
                _content = game.Content;
                _inputTracker = new InputTracker(game._window);
                _dialogueSystem = new DialogueSystem(this, game.GlyphRasterizer, _inputTracker);
                _animationProcessor = new AnimationProcessor(this);
                _choiceProcessor = new ChoiceProcessor(this);
                _renderSystem = new Renderer(
                    _world,
                    game._configuration,
                    game._graphicsDevice,
                    game._swapchain,
                    game.GlyphRasterizer,
                    game.Content
                );
                //_audioSystem = new AudioSystem(_world, game.Content, game.AudioSourcePool);
                //_devModeOverlay = new DevModeOverlay(_renderSystem.RenderContext, game.LogEventRecorder);
            }

            public InputTracker InputTracker => _inputTracker;

            public void ProcessChoices()
            {
                _renderSystem.ProcessTransforms();
                foreach (Entity choice in _world.Choices.Active.Entities)
                {
                    _choiceProcessor.ProcessChoice(_world, choice, _inputTracker);
                }
            }

            public void Tick(in FrameStamp framestamp, float deltaMilliseconds)
            {
                _inputTracker.Update();
                _world.FlushDetachedAnimations();

                AnimationProcessorOutput animProcessorOutput = _animationProcessor
                    .ProcessAnimations(deltaMilliseconds);

                bool blockInput = animProcessorOutput.BlockingAnimationCount > 0;
                //mainThreadWaiting || ThreadAwaitingSelect != null || animProcessorOutput.BlockingAnimationCount > 0;
                _dialogueSystemInput.AcceptUserInput = !blockInput;
                if (_dialogueSystem.AdvanceDialogueState(ref _dialogueSystemInput))
                {
                    _dialogueSystemInput.Command = DialogueSystemCommand.HandleInput;
                }

                //_audioSystem.UpdateAudioSources();

                try
                {
                    _renderSystem.Render(framestamp, _content, _captureFramebuffer);
                    if (_captureFramebuffer)
                    {
                        _captureFramebuffer = false;
                        PostMessage(new SimpleMessage(MessageKind.ResumeMainThread));
                    }
                }
                catch (VeldridException e) when (e.Message == "The Swapchain's underlying surface has been lost.")
                {
                    return;
                }

                //_devModeOverlay.Tick(deltaMilliseconds, _inputTracker);
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
                        case MessageKind.CaptureFramebuffer:
                            _captureFramebuffer = true;
                            break;
                    }
                }
            }

            public void Dispose()
            {
                _renderSystem.Dispose();
                //_devModeOverlay.Dispose();
            }
        }
    }
}
