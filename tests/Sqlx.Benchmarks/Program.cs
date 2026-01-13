using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Sqlx.Benchmarks.Benchmarks;

namespace Sqlx.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        if (args.Length == 0 || args.Contains("--all"))
        {
            Console.WriteLine("Running all benchmarks...");
            Console.WriteLine("This may take a while. Use --filter to run specific benchmarks.");
            Console.WriteLine();
            
            var config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);
            
            BenchmarkRunner.Run(new[]
            {
                typeof(SelectSingleBenchmark),
                typeof(SelectListBenchmark),
                typeof(InsertBenchmark),
                typeof(UpdateBenchmark),
                typeof(QueryWithFilterBenchmark),
                typeof(CountBenchmark),
                typeof(PaginationBenchmark)
            }, config);
        }
        else
        {
            // Run with BenchmarkSwitcher for filtering
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
