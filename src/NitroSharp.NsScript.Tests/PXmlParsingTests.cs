using NitroSharp.NsScript.Syntax.PXml;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class PXmlParsingTests
    {
        [Fact]
        public void ParseVoiceElement()
        {
            string text = "<voice name=\"sample_name\" class=\"sample_class\" src=\"sample_voice\">";
            var content = Parsing.ParsePXmlString(text);
            var voice = content.Children[0] as VoiceElement;
            Assert.NotNull(content);

            Assert.Equal(NsVoiceAction.Play, voice.Action);
            Assert.Equal("sample_name", voice.CharacterName);
            Assert.Equal("sample_voice", voice.FileName);
        }

        [Fact]
        public void ParseFontElementWithSimpleContent()
        {
            string text = "<FONT incolor=\"WHITE\" outcolor=\"BLACK\">Sample Text</FONT>";
            var content = Parsing.ParsePXmlString(text);

            var fontElement = content.Children[0] as ColorElement;
            Assert.NotNull(fontElement);
            var sampleContent = fontElement.Content.Children[0] as PXmlText;

            Assert.NotNull(sampleContent);
            Assert.Equal("Sample Text", sampleContent.Text);
        }

        [Fact]
        public void ParseElementWithMixedContent()
        {
            string text = "<FONT incolor=\"white\" outcolor=\"black\">Sample Text <RUBY text=\"sample_ruby_text\">sample_ruby_base</RUBY></FONT>";
            var content = Parsing.ParsePXmlString(text);

            var fontElement = content.Children[0] as ColorElement;
            Assert.NotNull(fontElement);
            Assert.Equal(2, fontElement.Content.Children.Length);

            var sampleText = fontElement.Content.Children[0] as PXmlText;
            Assert.NotNull(text);
            Assert.Equal("Sample Text ", sampleText.Text);

            var rubyElement = fontElement.Content.Children[1] as RubyElement;
            Assert.NotNull(rubyElement);

            Assert.Equal("sample_ruby_text", rubyElement.RubyText);
            Assert.NotNull(rubyElement.RubyBase);
        }
    }
}
