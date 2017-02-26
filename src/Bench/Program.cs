using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Bench
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = ManualConfig
           .Create(DefaultConfig.Instance)
           .With(Job.Core)
           .With(ExecutionValidator.FailOnError);

            BenchmarkRunner.Run<CollectBench>(config);
        }
    }
}
