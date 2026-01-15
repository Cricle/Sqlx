using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Dapper;
using Sqlx.Benchmarks.Benchmarks;

[module: DapperAot]

namespace Sqlx.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Determine which config to use
        IConfig config;
        
        if (args.Contains("--aot"))
        {
            Console.WriteLine("Running with Native AOT configuration...");
            config = new AotConfig();
            args = args.Where(a => a != "--aot").ToArray();
        }
        else if (args.Contains("--net10-aot"))
        {
            Console.WriteLine("Running with .NET 10 Native AOT configuration...");
            Console.WriteLine("Note: Using .NET 9 AOT until BenchmarkDotNet officially supports .NET 10");
            config = new Net10AotConfig();
            args = args.Where(a => a != "--net10-aot").ToArray();
        }
        else
        {
            config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
        
        // Add console logger to see progress
        config = ManualConfig.CreateEmpty()
            .AddLogger(ConsoleLogger.Default)
            .AddColumnProvider(config.GetColumnProviders().ToArray())
            .AddExporter(config.GetExporters().ToArray())
            .AddDiagnoser(config.GetDiagnosers().ToArray())
            .AddAnalyser(config.GetAnalysers().ToArray())
            .AddValidator(config.GetValidators().ToArray())
            .AddJob(config.GetJobs().ToArray())
            .WithOptions(config.Options);
        
        if (args.Length == 0 || args.Contains("--all"))
        {
            Console.WriteLine("Running all benchmarks...");
            Console.WriteLine("Use --filter to run specific benchmarks.");
            Console.WriteLine("Use --aot to run with Native AOT (.NET 9).");
            Console.WriteLine("Use --net10-aot to run with .NET 10 Native AOT.");
            Console.WriteLine();
            
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
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
