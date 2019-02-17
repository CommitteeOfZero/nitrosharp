using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

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
            var list = GlobalVarLookupTable.Load(File.OpenRead("S:/globals"));
            list.TryLookupSystemVariable("SYSTEM_present_preprocess", out int index);
            //RunCompiler();
            //RunVM();
        }

        static void RunVM()
        {
            var impl = new FuncImpl();
            var vm = new VirtualMachine(new FileSystemNsxModuleLocator(ScriptFolder), impl);
            vm.CreateThread("sampletext", "test", "main", true);
            vm.Run(CancellationToken.None);
            //vm.Tick(ref thread);
        }

        static void RunDisasm()
        {
            //using var script = File.OpenRead("S:/ChaosContent/Noah/nsx/boot.nsx");
            ////var boot = NsxModule.LoadModule(script);

            //foreach (string import in boot.Imports)
            //{
            //    Console.WriteLine(import);
            //}

            //ref readonly SubroutineRuntimeInformation srti =
            //    ref boot.GetSubroutineRuntimeInformation(1);



            //var someFunc = boot.GetSubroutine(0);
            //var disasm = new BodyDisassembler(someFunc.Code);

            //var sw = Stopwatch.StartNew();
            //for (int i = 0; i < boot.StringCount; i++)
            //{
            //    boot.GetString((ushort)i);
            //}
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

            //Opcode opcode = Opcode.Nop;
            //int n = 0;
            //do
            //{
            //    opcode = disasm.NextOpcode();
            //    disasm.SkipOperands();

            //    Console.WriteLine(opcode.ToString());
            //    n++;
            //} while (opcode != Opcode.Return);
        }

        static void RunDebuggerUI()
        {
            var debugger = new DebuggerUI();
            debugger.Run();
        }

        static void RunCompiler()
        {
            //bool s = GC.TryStartNoGCRegion(125829120);
            var sw = Stopwatch.StartNew();
            var compilation = new Compilation(ScriptFolder);
            SourceModuleSymbol boot = compilation.GetSourceModule("test.nss");
            compilation.Emit(boot);
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalSeconds);
            //GC.EndNoGCRegion();
        }

        
    }
}
