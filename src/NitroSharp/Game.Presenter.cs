using NitroSharp.Animation;
using NitroSharp.Dialogue;
using NitroSharp.Graphics.Systems;
using NitroSharp.Input;
using NitroSharp.Interactivity;
using NitroSharp.Media;
using Veldrid;

namespace NitroSharp
{
    public partial class Game
    {
        internal sealed class Presenter : Actor
        {
            private readonly World _world;
            private readonly Configuration _configuration;
            private readonly InputTracker _inputTracker;

            private DialogueSystemInput _dialogueSystemInput;
            private readonly DialogueSystem _dialogueSystem;
            private readonly RenderSystem _renderSystem;
            private readonly AudioSystem _audioSystem;
            private readonly AnimationProcessor _animationProcessor;
            private readonly ChoiceProcessor _choiceProcessor;

            public Presenter(Game game, World world) : base(world)
            {
                _configuration = game._configuration;
                _inputTracker = new InputTracker(game._window);

                _dialogueSystem = new DialogueSystem(this, _inputTracker);
                _animationProcessor = new AnimationProcessor(this);
                _choiceProcessor = new ChoiceProcessor(this);

                _world = world;
                _renderSystem = new RenderSystem(
                    _world, game._graphicsDevice, game._swapchain,
                    game.Content, game.FontService, _configuration);

                _audioSystem = new AudioSystem(_world, game.Content, game.AudioSourcePool);
            }

            public InputTracker InputTracker => _inputTracker;

            public void Tick(float deltaMilliseconds)
            {
                _inputTracker.Update(deltaMilliseconds);
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
                var choiceProcessorOutput = _choiceProcessor.ProcessChoices();
                if (choiceProcessorOutput.SelectedChoice != null)
                {
                    PostMessage(new ChoiceSelectedMessage
                    {
                        ChoiceName = choiceProcessorOutput.SelectedChoice
                    });
                }

                _audioSystem.UpdateAudioSources();
                _renderSystem.ExecutePipeline(deltaMilliseconds);

                try
                {
                    _renderSystem.Present();
                }
                catch (VeldridException e) when (e.Message == "The Swapchain's underlying surface has been lost.")
                {
                    return;
                }

                _world.FlushEvents();
            }

            protected override void HandleMessages<T>(T messages)
            {
                foreach (Message message in messages)
                {
                    switch (message.Kind)
                    {
                        case MessageKind.BeginDialogueBlock:
                            var beginBlockMsg = (BeginDialogueBlockMessage)message;
                            _dialogueSystemInput.TextEntity = beginBlockMsg.TextEntity;
                            break;
                        case MessageKind.BeginDialogueLine:
                            var beginLineMsg = (BeginDialogueLineMessage)message;
                            _dialogueSystemInput.Command = DialogueSystemCommand.BeginDialogue;
                            _dialogueSystemInput.DialogueLine = beginLineMsg.DialogueLine;
                            break;
                    }
                }
            }
        }
    }
}
