using SciAdvNet.NSScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NssInteractive
{
    class TextExtractor : CodeWriter
    {
        public TextExtractor(TextWriter textWriter) : base(textWriter)
        {
        }

        public override void VisitDialogueBlock(DialogueBlock dialogueBlock)
        {
            Write($"[{dialogueBlock.Identifier}]\n");
            int idxCodeBlock = 0;
            foreach (var part in dialogueBlock.Statements)
            {
                if (part is Block)
                {
                    Write($"[code{dialogueBlock.Identifier.Replace("text", string.Empty) + "-" + idxCodeBlock}]");
                    idxCodeBlock++;
                }

                if (part is Voice)
                {
                    Visit(part as Voice);
                }

                if (part is DialogueLine)
                {
                    Visit(part as DialogueLine);
                }
   
                Write(Environment.NewLine);
            }
        }

        public override void VisitVoice(Voice voice)
        {
            Write(voice.CharacterName);
        }

        public override void VisitMethod(Method method)
        {
            Visit(method.Body);
        }

        public override void VisitBlock(Block block)
        {
            VisitArray(block.Statements);
        }
    }
}
