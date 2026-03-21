using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Sqlx.Annotations;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Microbenchmark for TableNameResolver hot paths.
/// This focuses on cached table metadata lookups for static table names,
/// dynamic table name methods, and default type-name fallback.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TableNameResolverBenchmark
{
    private const int InnerLoopCount = 512;

    [GlobalSetup]
    public void Setup()
    {
        _ = TableNameResolver.Resolve(typeof(StaticTableEntity));
        _ = TableNameResolver.Resolve(typeof(DynamicTableEntity));
        _ = TableNameResolver.Resolve(typeof(DefaultTableEntity));
    }

    [Benchmark(Baseline = true, Description = "Resolve static table name", OperationsPerInvoke = InnerLoopCount)]
    public int Resolve_Static()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += TableNameResolver.Resolve(typeof(StaticTableEntity)).Length;
        }

        return length;
    }

    [Benchmark(Description = "Resolve dynamic method table name", OperationsPerInvoke = InnerLoopCount)]
    public int Resolve_DynamicMethod()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += TableNameResolver.Resolve(typeof(DynamicTableEntity)).Length;
        }

        return length;
    }

    [Benchmark(Description = "Resolve type-name fallback", OperationsPerInvoke = InnerLoopCount)]
    public int Resolve_TypeNameFallback()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += TableNameResolver.Resolve(typeof(DefaultTableEntity)).Length;
        }

        return length;
    }

    [Sqlx, TableName("benchmark_users")]
    public partial class StaticTableEntity
    {
        public int Id { get; set; }
    }

    [Sqlx, TableName("fallback_benchmark_users", Method = nameof(GetTableName))]
    public partial class DynamicTableEntity
    {
        public int Id { get; set; }

        public static string GetTableName() => "runtime_benchmark_users";
    }

    public class DefaultTableEntity
    {
        public int Id { get; set; }
    }
}
