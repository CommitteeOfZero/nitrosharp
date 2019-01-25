//using BenchmarkDotNet.Attributes;
//using NitroSharp.NsScriptNew;
//using System.IO;

//namespace NitroSharp.NsScriptCompiler.Playground
//{
//    [MemoryDiagnoser]
//    public class LexerBenchmark
//    {
//        private readonly NitroSharp.NsScriptNew.Text.SourceText _sourceText;
//        private readonly NsScript.Text.SourceText _oldSourceText;

//        public LexerBenchmark()
//        {
//            using (var stream = File.OpenRead("boot.nss"))
//            {
//                _sourceText = NitroSharp.NsScriptNew.Text.SourceText.From(stream, stream.Name);
//                stream.Seek(0, SeekOrigin.Begin);
//                _oldSourceText = NitroSharp.NsScript.Text.SourceText.From(stream, stream.Name);
//            }
//        }

//        [Benchmark]
//        public void ParseOld()
//        {
//            var syntaxTree = NsScript.Parsing.ParseText(_oldSourceText);
//        }

//        [Benchmark]
//        public void ParseNew()
//        {
//            var syntaxTree = Parsing.ParseText(_sourceText);
//        }
//    }
//}
