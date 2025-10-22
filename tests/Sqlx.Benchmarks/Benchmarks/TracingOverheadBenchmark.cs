using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// 追踪开销性能测试
/// 对比 Sqlx 不同配置下的性能开销：
/// 1. 完整追踪（Activity + Stopwatch）
/// 2. 只有指标（Stopwatch）
/// 3. 零追踪（极致性能）
/// 4. 原始ADO.NET（基准）
/// 5. Dapper（对比）
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TracingOverheadBenchmark
{
    private SqliteConnection _connection = null!;
    private UserRepositoryWithTracing _repoWithTracing = null!;
    private UserRepositoryNoTracing _repoNoTracing = null!;
    private UserRepositoryMetricsOnly _repoMetricsOnly = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                salary REAL NOT NULL,
                is_active INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        cmd.ExecuteNonQuery();

        // 插入100条测试数据
        for (int i = 1; i <= 100; i++)
        {
            cmd.CommandText = $@"
                INSERT INTO users (name, email, age, salary, is_active, created_at)
                VALUES ('User{i}', 'user{i}@example.com', {20 + i % 50}, {30000 + i * 1000}, {i % 2}, '2024-01-{i % 28 + 1:D2}')";
            cmd.ExecuteNonQuery();
        }

        // 初始化三种不同配置的Repository
        _repoWithTracing = new UserRepositoryWithTracing(_connection);
        _repoNoTracing = new UserRepositoryNoTracing(_connection);
        _repoMetricsOnly = new UserRepositoryMetricsOnly(_connection);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ============================================================
    // 单行查询 - 追踪开销对比
    // ============================================================

    /// <summary>
    /// 基准：原始ADO.NET（无任何开销）
    /// </summary>
    [Benchmark(Baseline = true, Description = "Raw ADO.NET (基准)")]
    public User? RawAdoNet_SingleRow()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE id = 1";

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                Age = reader.GetInt32(3),
                Salary = (decimal)reader.GetDouble(4),
                IsActive = reader.GetInt32(5) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
            };
        }
        return null;
    }

    /// <summary>
    /// Sqlx - 零追踪（EnableTracing=false, EnableMetrics=false）
    /// 测试极致性能，无任何追踪和指标开销（默认使用硬编码索引）
    /// </summary>
    [Benchmark(Description = "Sqlx 零追踪")]
    public User? Sqlx_NoTracing_SingleRow()
    {
        return _repoNoTracing.GetByIdSync(1);
    }

    /// <summary>
    /// Sqlx - 只有指标（EnableTracing=false, EnableMetrics=true）
    /// 测试只有Stopwatch计时的性能影响
    /// </summary>
    [Benchmark(Description = "Sqlx 只有指标")]
    public User? Sqlx_MetricsOnly_SingleRow()
    {
        return _repoMetricsOnly.GetByIdSync(1);
    }

    /// <summary>
    /// Sqlx - 完整追踪（EnableTracing=true, EnableMetrics=true）
    /// 测试Activity追踪 + Stopwatch计时的完整开销
    /// </summary>
    [Benchmark(Description = "Sqlx 完整追踪")]
    public User? Sqlx_WithTracing_SingleRow()
    {
        return _repoWithTracing.GetByIdSync(1);
    }

    /// <summary>
    /// Dapper - 作为对比参考
    /// </summary>
    [Benchmark(Description = "Dapper")]
    public User? Dapper_SingleRow()
    {
        return _connection.QueryFirstOrDefault<User>(
            "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE id = 1");
    }

    // ============================================================
    // 多行查询 - 追踪开销对比
    // ============================================================

    /// <summary>
    /// 基准：原始ADO.NET - 多行查询（10条）
    /// </summary>
    [Benchmark(Description = "Raw ADO.NET 多行 (基准)")]
    public List<User> RawAdoNet_MultiRow()
    {
        var users = new List<User>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users LIMIT 10";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                Age = reader.GetInt32(3),
                Salary = (decimal)reader.GetDouble(4),
                IsActive = reader.GetInt32(5) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
            });
        }
        return users;
    }

    /// <summary>
    /// Sqlx - 零追踪 - 多行查询
    /// </summary>
    [Benchmark(Description = "Sqlx 零追踪 多行")]
    public List<User> Sqlx_NoTracing_MultiRow()
    {
        return _repoNoTracing.GetTopNSync(10);
    }

    /// <summary>
    /// Sqlx - 只有指标 - 多行查询
    /// </summary>
    [Benchmark(Description = "Sqlx 只有指标 多行")]
    public List<User> Sqlx_MetricsOnly_MultiRow()
    {
        return _repoMetricsOnly.GetTopNSync(10);
    }

    /// <summary>
    /// Sqlx - 完整追踪 - 多行查询
    /// </summary>
    [Benchmark(Description = "Sqlx 完整追踪 多行")]
    public List<User> Sqlx_WithTracing_MultiRow()
    {
        return _repoWithTracing.GetTopNSync(10);
    }

    /// <summary>
    /// Dapper - 多行查询
    /// </summary>
    [Benchmark(Description = "Dapper 多行")]
    public List<User> Dapper_MultiRow()
    {
        return _connection.Query<User>(
            "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users LIMIT 10").ToList();
    }

    // ============================================================
    // 复杂查询 - 追踪开销对比
    // ============================================================

    /// <summary>
    /// 基准：原始ADO.NET - 复杂查询（条件、排序）
    /// </summary>
    [Benchmark(Description = "Raw ADO.NET 复杂查询 (基准)")]
    public List<User> RawAdoNet_ComplexQuery()
    {
        var users = new List<User>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE age > 30 AND is_active = 1 LIMIT 20";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                Age = reader.GetInt32(3),
                Salary = (decimal)reader.GetDouble(4),
                IsActive = reader.GetInt32(5) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
            });
        }
        return users;
    }

    /// <summary>
    /// Sqlx - 零追踪 - 复杂查询
    /// </summary>
    [Benchmark(Description = "Sqlx 零追踪 复杂")]
    public List<User> Sqlx_NoTracing_ComplexQuery()
    {
        return _repoNoTracing.GetByAgeAndStatusSync(30, 1);
    }

    /// <summary>
    /// Sqlx - 只有指标 - 复杂查询
    /// </summary>
    [Benchmark(Description = "Sqlx 只有指标 复杂")]
    public List<User> Sqlx_MetricsOnly_ComplexQuery()
    {
        return _repoMetricsOnly.GetByAgeAndStatusSync(30, 1);
    }

    /// <summary>
    /// Sqlx - 完整追踪 - 复杂查询
    /// </summary>
    [Benchmark(Description = "Sqlx 完整追踪 复杂")]
    public List<User> Sqlx_WithTracing_ComplexQuery()
    {
        return _repoWithTracing.GetByAgeAndStatusSync(30, 1);
    }

    /// <summary>
    /// Dapper - 复杂查询
    /// </summary>
    [Benchmark(Description = "Dapper 复杂")]
    public List<User> Dapper_ComplexQuery()
    {
        return _connection.Query<User>(
            "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE age > @minAge AND is_active = @isActive LIMIT 20",
            new { minAge = 30, isActive = 1 }).ToList();
    }
}

