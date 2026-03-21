using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks PlaceholderProcessor.ExtractParameters across representative SQL shapes.
/// This provides a baseline for the single-pass scanner implementation that skips
/// strings, comments, PostgreSQL casts, and dollar-quoted strings.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ParameterExtractionBenchmark
{
    private string _simpleSql = null!;
    private string _duplicateSql = null!;
    private string _stringAndCommentSql = null!;
    private string _postgresSql = null!;

    [GlobalSetup]
    public void Setup()
    {
        _simpleSql = "SELECT * FROM users WHERE id = @id AND name = @name AND age >= @minAge";
        _duplicateSql = "SELECT * FROM users WHERE id = @id OR parent_id = @id OR manager_id = @id";
        _stringAndCommentSql = """
            SELECT *
            FROM users
            WHERE note = '@ignored'
              AND id = @id
              -- @commented
              AND status = @status
              /* @ignored_block */
            """;
        _postgresSql = """
            SELECT created_at::text, $$@ignored$$, $tag$@also_ignored$tag$
            FROM users
            WHERE tenant_id = @tenantId AND user_id = @userId
            """;
    }

    [Benchmark(Baseline = true, Description = "Extract: simple SQL")]
    public IReadOnlyList<string> Extract_Simple()
    {
        return PlaceholderProcessor.ExtractParameters(_simpleSql);
    }

    [Benchmark(Description = "Extract: duplicate params")]
    public IReadOnlyList<string> Extract_Duplicates()
    {
        return PlaceholderProcessor.ExtractParameters(_duplicateSql);
    }

    [Benchmark(Description = "Extract: strings and comments")]
    public IReadOnlyList<string> Extract_StringsAndComments()
    {
        return PlaceholderProcessor.ExtractParameters(_stringAndCommentSql);
    }

    [Benchmark(Description = "Extract: PostgreSQL features")]
    public IReadOnlyList<string> Extract_PostgreSqlFeatures()
    {
        return PlaceholderProcessor.ExtractParameters(_postgresSql);
    }
}
