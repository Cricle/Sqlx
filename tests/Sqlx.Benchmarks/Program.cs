using BenchmarkDotNet.Running;
using Sqlx.Benchmarks.Benchmarks;

namespace Sqlx.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sqlx Performance Benchmarks");
        Console.WriteLine("============================\n");
        
        if (args.Length > 0 && args[0] == "--list")
        {
            Console.WriteLine("Available benchmarks:");
            Console.WriteLine("1. SelectSingleBenchmark - Single row SELECT performance");
            Console.WriteLine("2. SelectListBenchmark - Multiple rows SELECT performance");
            Console.WriteLine("3. BatchInsertBenchmark - Batch INSERT performance (Sqlx advantage!)");
            Console.WriteLine("\nRun with --filter <BenchmarkName> to run specific benchmark");
            Console.WriteLine("Run without arguments to run all benchmarks");
            return;
        }
        
        var config = BenchmarkDotNet.Configs.DefaultConfig.Instance;
        
        if (args.Length > 0 && args[0] == "--filter" && args.Length > 1)
        {
            var filter = args[1].ToLower();
            switch (filter)
            {
                case "select":
                case "selectsingle":
                    BenchmarkRunner.Run<SelectSingleBenchmark>(config);
                    break;
                case "list":
                case "selectlist":
                    BenchmarkRunner.Run<SelectListBenchmark>(config);
                    break;
                case "batch":
                case "batchinsert":
                    BenchmarkRunner.Run<BatchInsertBenchmark>(config);
                    break;
                default:
                    Console.WriteLine($"Unknown benchmark: {filter}");
                    Console.WriteLine("Use --list to see available benchmarks");
                    break;
            }
        }
        else
        {
            // Run all benchmarks
            Console.WriteLine("Running ALL benchmarks...\n");
            BenchmarkRunner.Run<SelectSingleBenchmark>(config);
            BenchmarkRunner.Run<SelectListBenchmark>(config);
            BenchmarkRunner.Run<BatchInsertBenchmark>(config);
        }
        
        Console.WriteLine("\nBenchmarks complete!");
        Console.WriteLine("Results saved to BenchmarkDotNet.Artifacts/results/");
    }
}
