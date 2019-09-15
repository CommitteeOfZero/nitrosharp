using System;
using NitroSharp.Media.Decoding;
using NitroSharp.Content;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.VM;
using NitroSharp.Primitives;
using Veldrid;
using NitroSharp.Dialogue;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        private readonly FontConfiguration _fontConfig;

        private string _lastDialogueBlockName;
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
            Entity box = _world.CreateRectangle(
                entityName,
                priority,
                new SizeF(width, height),
                ref color
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
            if (_world.TryGetEntity(token.BoxName, out Entity dialogueBox))
            {
                if (_lastDialogueBlockName != null)
                {
                    _world.RemoveEntity(_lastDialogueBlockName);
                }

                MutTextBlock textBlock = _world.CreateTextBlock(token.BlockName, 99999);
                _lastDialogueBlockName = token.BlockName;
                textBlock.Layout = new TextLayout(
                    GlyphRasterizer,
                    Array.Empty<TextRun>(),
                    new Size((uint)maxWidth, (uint)maxHeight)
                );
                SetParent(textBlock.Entity, dialogueBox);

                _lastDialogueBlockToken = token;
                _lastTextEntity = textBlock.Entity;
            }
        }

        public override void CreateText(
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
            if (_lastVoiceName != null)
            {
                _world.RemoveEntity(_lastVoiceName);
            }

            if (voice.Action == NsVoiceAction.Play)
            {
                var assetId = new AssetId("voice/" + voice.FileName);
                MediaPlaybackSession? voiceClip = Content.TryGetMediaClip(assetId, increaseRefCount: false);
                if (voiceClip != null)
                {
                    Entity entity = _world.CreateAudioClip(voice.FileName, assetId, false);
                    _world.AudioClips.Duration.Set(entity, voiceClip.AudioStream!.Duration);
                    _lastVoiceName = voice.FileName;
                }
            }
            else
            {
                _world.RemoveEntity(voice.FileName);
            }
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
