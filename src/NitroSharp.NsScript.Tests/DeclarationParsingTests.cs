using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class DeclarationParsingTests
    {
        [Fact]
        public void ParseChapterDeclaration()
        {
            string text = "chapter main{}";
            var chapter = Parsing.ParseMemberDeclaration(text) as Chapter;

            Assert.NotNull(chapter);
            Assert.Equal(SyntaxNodeKind.Chapter, chapter.Kind);
            Assert.Equal("main", chapter.Name.OriginalName);

            string toStringResult = Helpers.RemoveNewLineCharacters(chapter.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseSceneDeclaration()
        {
            string text = "scene TestScene{}";
            var scene = Parsing.ParseMemberDeclaration(text) as Scene;

            Assert.NotNull(scene);
            Assert.Equal(SyntaxNodeKind.Scene, scene.Kind);
            Assert.Equal("TestScene", scene.Name.OriginalName);
        }

        [Fact]
        public void ParseFunctionDeclaration()
        {
            string text = "function Test(){}";
            var function = Parsing.ParseMemberDeclaration(text) as Function;

            Assert.NotNull(function);
            Assert.Equal(SyntaxNodeKind.Function, function.Kind);
            Assert.Equal("Test", function.Name.OriginalName);
            Assert.Empty(function.Parameters);

            string toStringResult = Helpers.RemoveNewLineCharacters(function.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseFunctionDeclarationWithIntParameter()
        {
            string text = "function Test(intParam){}";
            var function = Parsing.ParseMemberDeclaration(text) as Function;

            Assert.NotNull(function);
            Assert.Single(function.Parameters);
            var p = function.Parameters[0];
            Assert.Equal(SyntaxNodeKind.Parameter, p.Kind);
            Assert.Equal("intParam", p.Name.OriginalName);
            Assert.Equal(p.Name.OriginalName, p.Name.Value);
            Assert.Equal(SigilKind.None, p.Name.Sigil);

            string toStringResult = Helpers.RemoveNewLineCharacters(function.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseFunctionDeclarationWithStringParameter()
        {
            string text = "function Test(\"stringParam\"){}";
            var function = Parsing.ParseMemberDeclaration(text) as Function;

            Assert.NotNull(function);
            Assert.Single(function.Parameters);
            var p = function.Parameters[0];
            Assert.Equal(SyntaxNodeKind.Parameter, p.Kind);
            Assert.Equal("\"stringParam\"", p.Name.OriginalName);
            Assert.Equal("stringParam", p.Name.Value);
            Assert.Equal(SigilKind.None, p.Name.Sigil);

            string toStringResult = Helpers.RemoveNewLineCharacters(function.ToString());
            Assert.Equal(text, toStringResult);
        }

        [Fact]
        public void ParseFunctionDeclarationWithStringParameterWithSigil()
        {
            TestFunctionWithStringParameterWithSigilImpl("\"$stringParam\"", "stringParam", SigilKind.Dollar);
            TestFunctionWithStringParameterWithSigilImpl("\"#stringParam\"", "stringParam", SigilKind.Hash);
        }

        private void TestFunctionWithStringParameterWithSigilImpl(string fullName, string simplifiedName, SigilKind sigil)
        {
            string text = $"function Test({fullName}){{}}";
            var function = Parsing.ParseMemberDeclaration(text) as Function;

            Assert.NotNull(function);
            Assert.Single(function.Parameters);
            var p = function.Parameters[0];
            Assert.Equal(SyntaxNodeKind.Parameter, p.Kind);
            Assert.Equal(fullName, p.Name.OriginalName);
            Assert.Equal(simplifiedName, p.Name.Value);
            Assert.Equal(sigil, p.Name.Sigil);

            string toStringResult = Helpers.RemoveNewLineCharacters(function.ToString());
            Assert.Equal(text, toStringResult);
        }
    }
}
