using BenchmarkDotNet.Running;
using NitroSharp.NsScriptNew;
using NitroSharp.NsScriptNew.Symbols;
using NitroSharp.NsScriptNew.Text;
using System;
using System.IO;
using System.Linq;

namespace NitroSharp.NsScriptCompiler.Playground
{
    class Program
    {
        private const string PathBase = "S:/ChaosContent/Noah";

        static void Main(string[] args)
        {
            var session = new NsScriptSession(LocateSourceFile);
            var tree = session.GetSyntaxTree("nss/boot.nss");

            ModuleSymbol module = session.GetModuleSymbol(tree);
            var functions = module.ReferencedSourceFiles.SelectMany(x => x.Functions);
            functions = functions.Concat(module.SourceFile.Functions);

            var arr = functions.ToArray();
            Meow(session, arr);

            var titleset = module.LookupFunction("TitleLogo");
            var boundTree = session.BindMember(titleset);
        }

        private static void Meow(NsScriptSession session, FunctionSymbol[] functions)
        {
            foreach (var function in functions)
            {
                session.BindMember(function);
            }
        }

        private static SourceText LocateSourceFile(FilePath relativePath)
        {
            string fullPath = PathBase + "/" + relativePath.Path;
            return SourceText.From(File.OpenRead(fullPath), fullPath);
        }
    }
}
