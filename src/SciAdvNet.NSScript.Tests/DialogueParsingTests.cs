using Xunit;

namespace SciAdvNet.NSScript.Tests
{
    public class DialogueParsingTests
    {
        [Fact]
        public void TestEmptyDialogueBlock()
        {
            string text = "<PRE box>[text000]</PRE>";
            var block = NSScript.ParseDialogueBlock(text);
            Assert.Equal("box", block.BoxName);
            Assert.Equal(SyntaxNodeKind.DialogueBlock, block.Kind);
            Assert.Equal(0, block.Statements.Length);

            string toStringResult = Helpers.RemoveNewLineCharacters(block.ToString());
            Assert.Equal(text, toStringResult);
        }

        //[Fact]
        //public void TestDialogueLine_OneSegment()
        //{
        //    string text = "<PRE box>[text000]This is a test.</PRE>";
        //    var block = NSScript.ParseDialogueBlock(text);
        //    Assert.Equal("text000", block.Identifier);
        //    Assert.Equal("box", block.BoxName);
        //    Assert.Equal(SyntaxNodeKind.DialogueBlock, block.Kind);
        //    Assert.Equal(1, block.Parts.Length);

        //    var line = block.Parts[0] as DialogueLine;
        //    Assert.Equal(SyntaxNodeKind.DialogueLine, line.Kind);
        //    Assert.Equal(1, line.Segments.Length);

        //    var segment = line.Segments[0];
        //    Assert.Equal(SyntaxNodeKind.TextSegment, segment.Kind);
        //    Assert.Equal("This is a test.", segment.Text);
        //}

        [Fact]
        public void TestXmlElementWithNewline()
        {
            string text = "<PRE box>[text000]<FONT\n incolor=\"#88abda\" outcolor=\"BLACK\">test</FONT></PRE>";

            var block = NSScript.ParseDialogueBlock(text);
            Assert.Equal(1, block.Statements.Length);

            var line = block.Statements[0] as DialogueLine;
            Assert.Equal(SyntaxNodeKind.DialogueLine, line.Kind);
            //Assert.Equal(1, line.Segments.Length);

            //var segment = line.Segments[0];
            //Assert.Equal(SyntaxNodeKind.TextSegment, segment.Kind);
            //Assert.Equal("This is a test.", segment.Text);
        }

        [Fact]
        public void TestNestedXmlTags()
        {
            string text = "<PRE box>[text000]<FONT incolor=\"#88abda\" outcolor=\"BLACK\">test <U>shit</U></FONT></PRE>";
            var block = NSScript.ParseDialogueBlock(text);
        }

        [Fact]
        public void TestVerbatimText()
        {
            string eyes = "(<◎ >皿 <◎ >  Whose eyes are those eyes?)";
            string text = $"<PRE box>[text000]<PRE>{eyes}</PRE></PRE>";
            var block = NSScript.ParseDialogueBlock(text);
            var dialogueLine = block.Statements[0] as DialogueLine;

            Assert.NotNull(dialogueLine);
            var verbatimText = dialogueLine.Content.Children[0] as PXmlVerbatimText;
            Assert.NotNull(verbatimText);
            Assert.Equal(eyes, verbatimText.Text);
        }
    }
}
