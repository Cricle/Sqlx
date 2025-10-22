using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// 性能基准测试
/// 对比不同ORM的性能：
/// 1. Raw ADO.NET（基准）
/// 2. Sqlx（强制启用追踪和指标）
/// 3. Dapper（对比）
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TracingOverheadBenchmark
{
    private SqliteConnection _connection = null!;
    private UserRepository _repo = null!;

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

        // 初始化Sqlx Repository
        _repo = new UserRepository(_connection);
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
    /// 基准：原始ADO.NET（使用参数化查询，公平对比）
    /// </summary>
    [Benchmark(Baseline = true, Description = "Raw ADO.NET (基准)")]
    public User? RawAdoNet_SingleRow()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE id = @id";

        // 参数化查询
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = 1;
        cmd.Parameters.Add(param);

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
    /// Sqlx - 强制启用追踪和指标
    /// 包含Activity追踪、Stopwatch计时、硬编码索引访问、智能IsDBNull检查
    /// 性能影响微小<0.1μs，提供完整可观测性
    /// </summary>
    [Benchmark(Description = "Sqlx")]
    public User? Sqlx_SingleRow()
    {
        return _repo.GetByIdSync(1);
    }

    /// <summary>
    /// Dapper - 作为对比参考（使用参数化查询）
    /// </summary>
    [Benchmark(Description = "Dapper")]
    public User? Dapper_SingleRow()
    {
        return _connection.QueryFirstOrDefault<User>(
            "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE id = @id",
            new { id = 1 });
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
    /// Sqlx - 多行查询（强制启用追踪和指标）
    /// </summary>
    [Benchmark(Description = "Sqlx 多行")]
    public List<User> Sqlx_MultiRow()
    {
        return _repo.GetTopNSync(10);
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
    /// Sqlx - 复杂查询（强制启用追踪和指标）
    /// </summary>
    [Benchmark(Description = "Sqlx 复杂")]
    public List<User> Sqlx_ComplexQuery()
    {
        return _repo.GetByAgeAndStatusSync(30, 1);
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

