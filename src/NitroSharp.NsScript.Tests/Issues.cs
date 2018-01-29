using System.Linq;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class Issues
    {
        [Fact]
        public void ParseCommaDotSeparatedArgumentList()
        {
            string text = "FadeDelete(\"痛い\", 150,. true);";
            var expr = Parsing.ParseExpression(text);
        }

        [Fact]
        public void ParseSemicolonTerminatedIncludeDirective()
        {
            string text = "#include \"foo.nss\";";
            var script = Parsing.ParseScript(text);
            var fileRef = script.FileReferences.SingleOrDefault();
            Assert.Equal("foo.nss", fileRef);
        }
    }
}
