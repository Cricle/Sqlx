using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Dapper;
using Sqlx.Benchmarks.Benchmarks;

[module: DapperAot]

namespace Sqlx.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--all"))
        {
            Console.WriteLine("Running all benchmarks...");
            Console.WriteLine("Use --filter to run specific benchmarks.");
            Console.WriteLine();
            
            var config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(Job.Default.WithRuntime(NativeAotRuntime.Net90));
            
            BenchmarkRunner.Run(new[]
            {
                typeof(SelectSingleBenchmark),
                typeof(SelectListBenchmark),
                typeof(InsertBenchmark),
                typeof(UpdateBenchmark),
                typeof(DeleteBenchmark),
                typeof(QueryWithFilterBenchmark),
                typeof(CountBenchmark),
                typeof(PaginationBenchmark),
                typeof(StaticOrdinalsBenchmark),
                typeof(StaticOrdinalsFirstOrDefaultBenchmark),
                typeof(BatchInsertBenchmark)
            }, config);
        }
        else
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
