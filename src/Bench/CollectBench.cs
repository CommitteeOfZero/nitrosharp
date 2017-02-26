using BenchmarkDotNet.Attributes;
using SciAdvNet.NSScript.Execution;

namespace Bench
{
    public class CollectBench
    {
        [Benchmark]
        public void Run()
        {
            var interpreter = new NSScriptInterpreter(new ScriptLocator(), new NssBuiltIns());
            interpreter.Run("nss/ch01_007_円山町殺人現場");
            var calls = interpreter.PendingBuiltInCalls;
        }
    }
}
