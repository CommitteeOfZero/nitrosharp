using BenchmarkDotNet.Running;
using System;

namespace NitroSharp.NsScriptCompiler.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.ReadLine();
            //new LexerBenchmark().ParseNew();
            //Console.ReadLine();
            BenchmarkRunner.Run<LexerBenchmark>();
        }
    }
}
