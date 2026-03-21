using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Sqlx.Expressions;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Microbenchmark for snake_case conversion hot paths.
/// This separates cached steady-state lookups from the uncached core algorithm.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SnakeCaseConversionBenchmark
{
    private const int InnerLoopCount = 512;

    private string _alreadyLowercase = null!;
    private string _mixedCase = null!;
    private string _acronymBoundary = null!;

    [GlobalSetup]
    public void Setup()
    {
        _alreadyLowercase = "display_name";
        _mixedCase = "DisplayName";
        _acronymBoundary = "UseHTTPProtocol";

        _ = ExpressionHelper.ConvertToSnakeCase(_alreadyLowercase);
        _ = ExpressionHelper.ConvertToSnakeCase(_mixedCase);
        _ = ExpressionHelper.ConvertToSnakeCase(_acronymBoundary);
    }

    [Benchmark(Baseline = true, Description = "SnakeCase cached lowercase", OperationsPerInvoke = InnerLoopCount)]
    public int Cached_Lowercase()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ExpressionHelper.ConvertToSnakeCase(_alreadyLowercase).Length;
        }

        return length;
    }

    [Benchmark(Description = "SnakeCase cached acronym", OperationsPerInvoke = InnerLoopCount)]
    public int Cached_AcronymBoundary()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ExpressionHelper.ConvertToSnakeCase(_acronymBoundary).Length;
        }

        return length;
    }

    [Benchmark(Description = "SnakeCase core mixed case", OperationsPerInvoke = InnerLoopCount)]
    public int Core_MixedCase()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ExpressionHelper.ConvertToSnakeCaseCore(_mixedCase).Length;
        }

        return length;
    }

    [Benchmark(Description = "SnakeCase core acronym", OperationsPerInvoke = InnerLoopCount)]
    public int Core_AcronymBoundary()
    {
        var length = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            length += ExpressionHelper.ConvertToSnakeCaseCore(_acronymBoundary).Length;
        }

        return length;
    }
}
