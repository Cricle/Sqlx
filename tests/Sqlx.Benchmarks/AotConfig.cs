using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.NativeAot;

namespace Sqlx.Benchmarks;

/// <summary>
/// Configuration for Native AOT benchmarks (.NET 9)
/// Compares JIT vs Native AOT performance
/// </summary>
public class AotConfig : ManualConfig
{
    public AotConfig()
    {
        // Add default job (JIT) as baseline
        AddJob(Job.Default
            .WithId("JIT-Net9")
            .AsBaseline()
            .WithRuntime(CoreRuntime.Core90));

        // Add Native AOT job for .NET 9.0
        AddJob(Job.Default
            .WithId("NativeAOT-Net9")
            .WithRuntime(NativeAotRuntime.Net90)
            .WithToolchain(NativeAotToolchain.Net90));

        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}

/// <summary>
/// Configuration for .NET 10 Native AOT benchmarks
/// .NET 10 is LTS (Long Term Support) released November 11, 2025
/// Note: Waiting for BenchmarkDotNet to officially support .NET 10
/// </summary>
public class Net10AotConfig : ManualConfig
{
    public Net10AotConfig()
    {
        // Fallback to .NET 9 AOT until BenchmarkDotNet adds .NET 10 support
        AddJob(Job.Default
            .WithId("JIT-Net9")
            .AsBaseline()
            .WithRuntime(CoreRuntime.Core90));

        AddJob(Job.Default
            .WithId("NativeAOT-Net9")
            .WithRuntime(NativeAotRuntime.Net90)
            .WithToolchain(NativeAotToolchain.Net90));

        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
