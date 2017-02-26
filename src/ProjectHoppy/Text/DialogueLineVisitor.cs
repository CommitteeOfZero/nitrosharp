using SciAdvNet.NSScript;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ProjectHoppy.Text
{
    public class DialogueLineVisitor : SyntaxVisitor
    {
        public override void VisitDialogueLine(DialogueLine dialogueLine)
        {
        }

        public override void VisitPXmlContent(PXmlContent pXmlContent)
        {
            VisitArray(pXmlContent.Children);
        }

    }
}
