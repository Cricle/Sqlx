using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Sqlx.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}


