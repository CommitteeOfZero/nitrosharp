using NitroSharp.NsScript.VM;
using System;

namespace NitroSharp.NsScriptCompiler.Playground
{
    sealed class FuncImpl : BuiltInFunctions
    {
        public override void SetAlias(string entityName, string alias)
        {
            base.SetAlias(entityName, alias);
        }

        public override void WaitForInput()
        {
            Console.ReadLine();
        }
    }

    partial class Program
    {
        private const string ScriptFolder = "S:/ChaosContent/Noah/tests";

        static void Main(string[] args)
        {
            RunDebuggerUI();
        }

        //static void RunVM()
        //{
        //    var impl = new FuncImpl();
        //    var vm = new VirtualMachine(new FileSystemNsxModuleLocator(ScriptFolder), impl);
        //    vm.CreateThread("sampletext", "test", "main", true);
        //    vm.Run(CancellationToken.None);
        //    //vm.Tick(ref thread);
        //}

        static void RunDebuggerUI()
        {
            var debugger = new DebuggerUI();
            debugger.Run();
        }

        //static void RunCompiler()
        //{
        //    //bool s = GC.TryStartNoGCRegion(125829120);
        //    var sw = Stopwatch.StartNew();
        //    var compilation = new Compilation(ScriptFolder, ScriptFolder.Replace("nss", "nsx"), "_globals");
        //    SourceModuleSymbol boot = compilation.GetSourceModule("test.nss");
        //    compilation.Emit(boot);
        //    sw.Stop();
        //    Console.WriteLine(sw.Elapsed.TotalSeconds);
        //    //GC.EndNoGCRegion();
        //}
    }
}
