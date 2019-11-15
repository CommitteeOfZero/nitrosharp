using System;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.VM;
using NitroSharp.Primitives;
using Veldrid;
using NitroSharp.Dialogue;
using NitroSharp.Text;
using NitroSharp.Experimental;
using NitroSharp.Graphics;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        private readonly FontConfiguration _fontConfig;

        private EntityName _lastDialogueBlockName;
        private string _lastVoiceName;

        private DialogueBlockToken _lastDialogueBlockToken;
        private Entity _lastTextEntity;

        private GlyphRasterizer GlyphRasterizer => _game.GlyphRasterizer;

        public override void CreateDialogueBox(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y,
            int width, int height)
        {
            RgbaFloat color = RgbaFloat.White;
            color.SetAlpha(0);
            (Entity box, _) = _world.Quads.Uninitialized.New(
                new EntityName(entityName),
                new SizeF(width, height),
                priority,
                Material.SolidColor(color)
            );
            SetPosition(box, x, y);
        }

        public override void LoadText(
            in DialogueBlockToken token,
            int maxWidth,
            int maxHeight,
            int letterSpacing,
            int lineSpacing)
        {
            if (_world.TryGetEntity(new EntityName(token.BoxName), out Entity dialogueBox))
            {
                if (_lastDialogueBlockName.Value != null)
                {
                    _world.DestroyEntity(_lastDialogueBlockName);
                }

                var layout = new TextLayout(
                    GlyphRasterizer,
                    Array.Empty<TextRun>(),
                    new Size((uint)maxWidth, (uint)maxHeight)
                );
                (Entity textBlock, _) = _world.TextBlocks.Uninitialized.New(
                    new EntityName(token.BlockName), layout, 99999
                );
                _lastDialogueBlockName = new EntityName(token.BlockName);
                _world.SetParent(textBlock, dialogueBox);

                _lastDialogueBlockToken = token;
                _lastTextEntity = textBlock;
            }
        }

        public override void CreateTextBlock(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y,
            NsDimension width, NsDimension height,
            string pxmlText)
        {
            //int getDimension(in MutTextBlock textBlock, NsDimension dim)
            //{
            //    return dim.Variant switch
            //    {
            //        NsDimensionVariant.Auto => int.MaxValue,
            //        NsDimensionVariant.Value => dim.Value!.Value,
            //        NsDimensionVariant.Inherit => textBlock.pa
            //    }
            //}

            var textBuffer = TextBuffer.FromPXmlString(pxmlText, _fontConfig);
            TextSegment? textSegment = textBuffer.AssertSingleTextSegment();
            //if (textSegment != null)
            //{

            //    MutTextBlock textBlock = _world.CreateTextBlock(entityName, priority);

            //    var layout = new TextLayout(GlyphCacheEntry, textSegment.TextRuns, maxBounds: null)
            //}
        }

        public override void BeginDialogueLine(string pxmlString)
        {
            var textBuffer = TextBuffer.FromPXmlString(pxmlString, _fontConfig);
            if (textBuffer.Voice != null)
            {
                HandleVoice(textBuffer.Voice);
            }
            _messageQueue.Enqueue(new Game.PresentDialogueMessage
            {
                TextBuffer = textBuffer
            });
        }

        private void HandleVoice(VoiceSegment voice)
        {
            //if (_lastVoiceName != null)
            //{
            //    _world.RemoveEntity(_lastVoiceName);
            //}

            //if (voice.Action == NsVoiceAction.Play)
            //{
            //    var assetId = new AssetId("voice/" + voice.FileName);
            //    MediaPlaybackSession? voiceClip = Content.TryGetMediaClip(assetId, increaseRefCount: false);
            //    if (voiceClip != null)
            //    {
            //        Entity entity = _world.CreateAudioClip(voice.FileName, assetId, false);
            //        _world.AudioClips.Duration.Set(entity, voiceClip.AudioStream!.Duration);
            //        _lastVoiceName = voice.FileName;
            //    }
            //}
            //else
            //{
            //    _world.RemoveEntity(voice.FileName);
            //}
        }

        public override void WaitText(string id, TimeSpan time)
        {
            Interpreter.ActivateDialogueBlock(_lastDialogueBlockToken);
            _messageQueue.Enqueue(new Game.BeginDialogueBlockMessage
            {
                TextEntity = _lastTextEntity
            });
        }
    }
}
