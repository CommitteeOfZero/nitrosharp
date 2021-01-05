using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    public class TestDiscoverer : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var ctx = new CompilationContext();
            SourceFileSymbol file = ctx.TestModule.RootSourceFile;
            foreach (FunctionSymbol func in file.Functions)
            {
                if (func.Parameters.IsEmpty && !func.Declaration.Name.Value.StartsWith("priv_"))
                {
                    yield return new[] { file.Name, func.Name };
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class MockBuiltInImpl : BuiltInFunctions
    {
        public override void AssertTrue(bool value)
        {
            if (!value)
            {
                VM.TerminateThread(CurrentThread);
            }
            Assert.True(value);
        }
    }

    public sealed class CompilationContext
    {
        public CompilationContext()
        {
            string assemblyDir = Environment.CurrentDirectory;
            NsxDir = Path.Combine(assemblyDir, "nsx");
            if (Directory.Exists(NsxDir))
            {
                Directory.Delete(NsxDir, recursive: true);
            }
            Directory.CreateDirectory(NsxDir);
            Compilation = new Compilation(
                new DefaultSourceReferenceResolver(assemblyDir),
                NsxDir,
                GlobalsFileName,
                Encoding.UTF8
            );
            TestModule = Compilation.GetSourceModule("langtests.nss");
        }

        public Compilation Compilation { get; }
        public SourceModuleSymbol TestModule { get; }
        public string NsxDir { get; }
        public string GlobalsFileName => "_globals";
    }

    public sealed class TestContext
    {
        public TestContext()
        {
            var compCtx = new CompilationContext();
            TestModule = compCtx.TestModule;
            compCtx.Compilation.Emit(new[] { TestModule });
            using FileStream globals = File.OpenRead(
                Path.Combine(compCtx.NsxDir, compCtx.GlobalsFileName)
            );
            VM = new NsScriptVM(new FileSystemNsxModuleLocator(compCtx.NsxDir), globals);
            BuiltInFunctions = new MockBuiltInImpl();
        }

        public NsScriptVM VM { get; }
        public BuiltInFunctions BuiltInFunctions { get; }
        public SourceModuleSymbol TestModule { get; }
    }

    public class LanguageTests : IClassFixture<TestContext>
    {
        private readonly TestContext _context;

        public LanguageTests(TestContext context)
        {
            _context = context;
        }

        [Theory]
        [ClassData(typeof(TestDiscoverer))]
        public void Test(string module, string function)
        {
            NsScriptProcess process = _context.VM.CreateProcess(module, function);
            _context.VM.Run(process, _context.BuiltInFunctions, CancellationToken.None);
        }
    }
}
