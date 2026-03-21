using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Sqlx.Annotations;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Microbenchmark for ColumnNameResolver hot paths.
/// It focuses on repeated property-to-column resolution with and without provider metadata.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ColumnNameResolverBenchmark
{
    private const int InnerLoopCount = 512;

    private IEntityProvider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _provider = new DynamicEntityProvider<ResolverBenchmarkEntity>();
    }

    [Benchmark(Baseline = true, Description = "Resolve mapped column", OperationsPerInvoke = InnerLoopCount)]
    public int Resolve_MappedColumn()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ColumnNameResolver.Resolve(_provider, "DisplayName").Length;
        }

        return length;
    }

    [Benchmark(Description = "Resolve second mapped column", OperationsPerInvoke = InnerLoopCount)]
    public int Resolve_SecondMappedColumn()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ColumnNameResolver.Resolve(_provider, "CreatedAt").Length;
        }

        return length;
    }

    [Benchmark(Description = "Resolve fallback snake_case", OperationsPerInvoke = InnerLoopCount)]
    public int Resolve_FallbackSnakeCase()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ColumnNameResolver.Resolve(null, "DisplayName").Length;
        }

        return length;
    }

    [Sqlx]
    public partial class ResolverBenchmarkEntity
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }

        public decimal AccountBalance { get; set; }
    }
}
