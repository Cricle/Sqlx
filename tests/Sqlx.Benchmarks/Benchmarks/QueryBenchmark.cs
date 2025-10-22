using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// 查询性能测试
/// 对比 Sqlx、Dapper、原始 ADO.NET 的查询性能
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class QueryBenchmark
{
    private SqliteConnection _connection = null!;
    private IUserRepository _userRepository = null!;

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

        // 初始化Sqlx Repository (使用源生成的实现)
        _userRepository = new UserRepository(_connection);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ============================================================
    // 单行查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 单行查询
    /// </summary>
    [Benchmark(Baseline = true)]
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
    /// Dapper - 单行查询
    /// </summary>
    [Benchmark]
    public User? Dapper_SingleRow()
    {
        return _connection.QueryFirstOrDefault<User>(
            "SELECT id, name, email, age, salary, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt FROM users WHERE id = 1");
    }

    /// <summary>
    /// Sqlx - 单行查询（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public User? Sqlx_SingleRow()
    {
        return _userRepository.GetByIdSync(1);
    }

    // ============================================================
    // 多行查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 多行查询
    /// </summary>
    [Benchmark]
    public List<User> RawAdoNet_MultiRow()
    {
        var users = new List<User>(10);
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
    /// Dapper - 多行查询
    /// </summary>
    [Benchmark]
    public List<User> Dapper_MultiRow()
    {
        return _connection.Query<User>(
            "SELECT id, name, email, age, salary, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt FROM users LIMIT 10").ToList();
    }

    /// <summary>
    /// Sqlx - 多行查询（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public List<User> Sqlx_MultiRow()
    {
        return _userRepository.GetTopNSync(10);
    }

    // ============================================================
    // 大量数据查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 全表查询（100行）
    /// </summary>
    [Benchmark]
    public List<User> RawAdoNet_FullTable()
    {
        var users = new List<User>(100);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users";

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
    /// Dapper - 全表查询（100行）
    /// </summary>
    [Benchmark]
    public List<User> Dapper_FullTable()
    {
        return _connection.Query<User>(
            "SELECT id, name, email, age, salary, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt FROM users").ToList();
    }

    /// <summary>
    /// Sqlx - 全表查询（100行）（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public List<User> Sqlx_FullTable()
    {
        return _userRepository.GetAllSync();
    }

    // ============================================================
    // 带参数查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 参数化查询
    /// </summary>
    [Benchmark]
    public List<User> RawAdoNet_WithParams()
    {
        var users = new List<User>(10);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE age > @minAge AND is_active = @isActive";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@minAge";
        p1.Value = 30;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@isActive";
        p2.Value = 1;
        cmd.Parameters.Add(p2);

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
    /// Dapper - 参数化查询
    /// </summary>
    [Benchmark]
    public List<User> Dapper_WithParams()
    {
        return _connection.Query<User>(
            "SELECT id, name, email, age, salary, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt FROM users WHERE age > @minAge AND is_active = @isActive",
            new { minAge = 30, isActive = 1 }).ToList();
    }

    /// <summary>
    /// Sqlx - 参数化查询（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public List<User> Sqlx_WithParams()
    {
        return _userRepository.GetByAgeAndStatusSync(30, 1);
    }
}
