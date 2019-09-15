using System.Collections.Immutable;
using System.Diagnostics;
using NitroSharp.Interactivity;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Dialogue
{
    internal struct DialogueSystemInput
    {
        public bool AcceptUserInput;
        public DialogueSystemCommand Command;
        public Entity TextEntity;
        public TextBuffer? TextBuffer;
    }

    internal enum DialogueSystemCommand
    {
        HandleInput,
        BeginDialogue
    }

    internal sealed class DialogueSystem : GameSystem
    {
        private readonly World _world;
        private readonly GlyphRasterizer _glyphRasterizer;
        private readonly InputTracker _inputTracker;

        private readonly EntityTable.RefTypeRow<TextLayout> _textLayouts;

        //private TextRevealAnimation? _revealAnimation;
        private int _currentSegment;
        private bool _startFromNewLine;

        public DialogueSystem(
            Game.Presenter presenter,
            GlyphRasterizer glyphRasterizer,
            InputTracker inputTracker)
            : base(presenter)
        {
            _world = presenter.World;
            _glyphRasterizer = glyphRasterizer;
            _inputTracker = inputTracker;
            _textLayouts = _world.TextBlocks.Layouts;
        }

        private void ResetState()
        {
            _currentSegment = 0;
            _startFromNewLine = false;
        }

        public bool AdvanceDialogueState(ref DialogueSystemInput input)
        {
            if (input.Command == DialogueSystemCommand.BeginDialogue)
            {
                ResetState();
                TextLayout textLayout = _textLayouts.GetValue(input.TextEntity);
                textLayout.Clear();
                AdvanceDialogue(ref input, textLayout);
                return true;
            }

            if (input.AcceptUserInput &&
                input.Command == DialogueSystemCommand.HandleInput
                && GotUserInput())
            {
                if (!input.TextEntity.IsValid)
                {
                    PostMessage(new Game.SimpleMessage(Game.MessageKind.ResumeMainThread));
                    return true;
                }

                switch (GetStatus(ref input))
                {
                    case Status.LineNotLoaded:
                        PostMessage(new Game.SimpleMessage(Game.MessageKind.ResumeMainThread));
                        break;
                    case Status.PlayingRevealAnimation:
                        SkipTextRevealAnimation(input.TextEntity);
                        break;
                    case Status.PlayingSkipAnimation:
                        break;
                    case Status.Waiting:
                        Debug.Assert(input.TextBuffer != null);
                        if (_currentSegment < input.TextBuffer.Segments.Length)
                        {
                            TextLayout textLayout = _textLayouts.GetValue(input.TextEntity);
                            AdvanceDialogue(ref input, textLayout);
                        }
                        else
                        {
                            PostMessage(new Game.SimpleMessage(Game.MessageKind.ResumeMainThread));
                        }
                        break;
                }
            }

            return false;
        }

        private bool GotUserInput()
        {
            InputTracker it = _inputTracker;
            return it.IsMouseButtonDownThisFrame(MouseButton.Left) || it.IsKeyDownThisFrame(Key.Space)
                || it.IsKeyDownThisFrame(Key.Enter) || it.IsKeyDownThisFrame(Key.KeypadEnter);
        }

        public enum Status
        {
            LineNotLoaded,
            PlayingRevealAnimation,
            PlayingSkipAnimation,
            Waiting
        }

        private Status GetStatus(ref DialogueSystemInput input)
        {
            if (input.TextBuffer == null)
            {
                return Status.LineNotLoaded;
            }

            //if (_world.TryGetAnimation<TextRevealAnimation>(input.TextEntity, out _revealAnimation))
            //{
            //    return Status.PlayingRevealAnimation;
            //}
            //if (_world.TryGetAnimation<RevealSkipAnimation>(input.TextEntity, out _))
            //{
            //    return Status.PlayingSkipAnimation;
            //}

            return Status.Waiting;
        }

        private void AdvanceDialogue(ref DialogueSystemInput input, TextLayout textLayout)
        {
            Debug.Assert(input.TextBuffer != null);
            ImmutableArray<TextBufferSegment> segments = input.TextBuffer.Segments;

            if (_startFromNewLine)
            {
                //textLayout.StartNewLine();
                _startFromNewLine = false;
            }

            uint revealStart = (uint)textLayout.Glyphs.Length;
            for (int i = _currentSegment; i < segments.Length; i++)
            {
                _currentSegment++;
                TextBufferSegment segment = segments[i];
                switch (segment.SegmentKind)
                {
                    case TextBufferSegmentKind.Text:
                        var textPart = (TextSegment)segment;
                        textLayout.Append(_glyphRasterizer, textPart.TextRuns.AsSpan());
                        break;

                    case TextBufferSegmentKind.Marker:
                        var marker = (MarkerSegment)segment;
                        switch (marker.MarkerKind)
                        {
                            case MarkerKind.Halt:
                                _startFromNewLine = true;
                                PostMessage(new Game.SimpleMessage(Game.MessageKind.SuspendMainThread));
                                goto exit;

                            case MarkerKind.NoLinebreaks:
                                _startFromNewLine = false;
                                break;
                        }
                        break;
                }
            }

            exit:
            return;
            //var animation = new TextRevealAnimation(_world, input.TextEntity, (ushort)revealStart);
            //_world.ActivateAnimation(animation);
        }

        private void SkipTextRevealAnimation(Entity textEntity)
        {
            //Debug.Assert(_revealAnimation != null);
            //TextRevealAnimation animation = _revealAnimation;
            //if (!animation.IsAllTextVisible)
            //{
            //    animation.Stop();
            //    if (!animation.IsAllTextVisible)
            //    {
            //        var skip = new RevealSkipAnimation(textEntity, (ushort)(animation.Position + 1));
            //        _world.ActivateAnimation(skip);
            //    }
            //}
        } 
    }
}
