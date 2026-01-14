using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Linq;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for IQueryable SQL generation performance.
/// Tests the speed of generating SQL from LINQ expressions.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SqlQueryBenchmark
{
    #region Simple Query Benchmarks

    [Benchmark(Description = "Simple SELECT *")]
    public string SimpleSelect()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>().ToSql();
    }

    [Benchmark(Description = "WHERE single condition")]
    public string WhereSingle()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.Id == 1)
            .ToSql();
    }

    [Benchmark(Description = "WHERE multiple AND")]
    public string WhereMultipleAnd()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.IsActive && u.Age >= 18 && u.Age <= 65)
            .ToSql();
    }

    [Benchmark(Description = "WHERE with OR")]
    public string WhereWithOr()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.Age < 18 || u.Age > 65 || !u.IsActive)
            .ToSql();
    }

    #endregion

    #region Complex Query Benchmarks

    [Benchmark(Description = "Full chain: Where+Select+OrderBy+Take+Skip")]
    public string FullChain()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.IsActive)
            .Where(u => u.Age >= 18)
            .Select(u => new { u.Id, u.Name, u.Age })
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .Skip(10)
            .Take(20)
            .ToSql();
    }

    [Benchmark(Description = "String functions")]
    public string StringFunctions()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.Name.Contains("test") && u.Name.ToUpper() == "TEST")
            .ToSql();
    }

    [Benchmark(Description = "Math functions")]
    public string MathFunctions()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => Math.Abs(u.Age) > 0 && Math.Round((double)u.Balance) > 1000)
            .ToSql();
    }

    [Benchmark(Description = "Conditional expression")]
    public string ConditionalExpression()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Select(u => u.Age >= 18 ? "Adult" : "Minor")
            .ToSql();
    }

    [Benchmark(Description = "Coalesce expression")]
    public string CoalesceExpression()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => (u.Email ?? "default@test.com") != "")
            .ToSql();
    }

    #endregion

    #region Parameterized Query Benchmarks

    [Benchmark(Description = "Parameterized simple")]
    public (string, Dictionary<string, object?>) ParameterizedSimple()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.Id == 1)
            .ToSqlWithParameters();
    }

    [Benchmark(Description = "Parameterized complex")]
    public (string, Dictionary<string, object?>) ParameterizedComplex()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.IsActive && u.Age >= 18 && u.Name == "test")
            .ToSqlWithParameters();
    }

    #endregion

    #region Multi-Dialect Benchmarks

    [Benchmark(Description = "SQLite dialect")]
    public string SqliteDialect()
    {
        return SqlQuery.ForSqlite<BenchmarkEntity>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10)
            .ToSql();
    }

    [Benchmark(Description = "SqlServer dialect")]
    public string SqlServerDialect()
    {
        return SqlQuery.ForSqlServer<BenchmarkEntity>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10)
            .ToSql();
    }

    [Benchmark(Description = "MySql dialect")]
    public string MySqlDialect()
    {
        return SqlQuery.ForMySql<BenchmarkEntity>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10)
            .ToSql();
    }

    [Benchmark(Description = "PostgreSQL dialect")]
    public string PostgreSqlDialect()
    {
        return SqlQuery.ForPostgreSQL<BenchmarkEntity>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10)
            .ToSql();
    }

    #endregion
}

/// <summary>
/// Entity for benchmark tests.
/// </summary>
public class BenchmarkEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}
