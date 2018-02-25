namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class VoiceElement : PXmlNode
    {
        internal VoiceElement(NsVoiceAction action, string characterName, string fileName)
        {
            Action = action;
            CharacterName = characterName;
            FileName = fileName;
        }

        public override PXmlNodeKind Kind => PXmlNodeKind.VoiceElement;
        public NsVoiceAction Action { get; }
        public string CharacterName { get; }
        public string FileName { get; }

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitVoiceElement(this);
        }
    }
}
