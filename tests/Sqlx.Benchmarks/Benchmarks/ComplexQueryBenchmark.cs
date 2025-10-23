using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// 复杂查询性能测试
/// 测试涉及 JOIN、聚合、排序等复杂操作的查询性能
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ComplexQueryBenchmark
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

        // 插入500条测试数据
        for (int i = 1; i <= 500; i++)
        {
            cmd.CommandText = $@"
                INSERT INTO users (name, email, age, salary, is_active, created_at)
                VALUES ('User{i}', 'user{i}@example.com', {20 + i % 50}, {30000 + i * 100}, {i % 2}, '2024-01-{i % 28 + 1:D2}')";
            cmd.ExecuteNonQuery();
        }

        // 初始化Sqlx Repository（强制启用追踪和指标，性能影响微小）
        _userRepository = new UserRepository(_connection);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ============================================================
    // 复杂查询 - 多条件筛选 + 排序 + 限制
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 复杂查询
    /// </summary>
    [Benchmark(Baseline = true)]
    public List<User> RawAdoNet_ComplexQuery()
    {
        var users = new List<User>(20);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, email, age, salary, is_active, created_at, updated_at 
            FROM users 
            WHERE is_active = @isActive AND age BETWEEN @minAge AND @maxAge
            ORDER BY salary DESC, name ASC
            LIMIT @limit";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@isActive";
        p1.Value = 1;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@minAge";
        p2.Value = 25;
        cmd.Parameters.Add(p2);

        var p3 = cmd.CreateParameter();
        p3.ParameterName = "@maxAge";
        p3.Value = 45;
        cmd.Parameters.Add(p3);

        var p4 = cmd.CreateParameter();
        p4.ParameterName = "@limit";
        p4.Value = 20;
        cmd.Parameters.Add(p4);

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
    /// Dapper - 复杂查询
    /// </summary>
    [Benchmark]
    public List<User> Dapper_ComplexQuery()
    {
        return _connection.Query<User>(@"
            SELECT id, name, email, age, salary, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt 
            FROM users 
            WHERE is_active = @isActive AND age BETWEEN @minAge AND @maxAge
            ORDER BY salary DESC, name ASC
            LIMIT @limit",
            new { isActive = 1, minAge = 25, maxAge = 45, limit = 20 }).ToList();
    }

    /// <summary>
    /// Sqlx - 复杂查询（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public List<User> Sqlx_ComplexQuery()
    {
        return _userRepository.ComplexQuerySync(25, 45, 20);
    }

    // ============================================================
    // 模拟复杂业务查询 - 多次查询组合
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 模拟复杂业务场景（多次查询）
    /// </summary>
    [Benchmark]
    public int RawAdoNet_BusinessScenario()
    {
        int totalCount = 0;

        // 1. 查询活跃用户
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM users WHERE is_active = 1";
            totalCount = Convert.ToInt32(cmd.ExecuteScalar());
        }

        // 2. 查询高薪用户
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE salary > 50000 LIMIT 5";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                totalCount++;
            }
        }

        // 3. 更新统计信息（模拟）
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT AVG(salary) FROM users WHERE is_active = 1";
            cmd.ExecuteScalar();
        }

        return totalCount;
    }

    /// <summary>
    /// Dapper - 模拟复杂业务场景（多次查询）
    /// </summary>
    [Benchmark]
    public int Dapper_BusinessScenario()
    {
        // 1. 查询活跃用户
        int totalCount = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM users WHERE is_active = 1");

        // 2. 查询高薪用户
        var highSalaryUsers = _connection.Query<User>(
            "SELECT id, name, email, age, salary, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt FROM users WHERE salary > 50000 LIMIT 5").ToList();
        totalCount += highSalaryUsers.Count;

        // 3. 更新统计信息（模拟）
        _connection.ExecuteScalar<decimal>("SELECT AVG(salary) FROM users WHERE is_active = 1");

        return totalCount;
    }

    /// <summary>
    /// Sqlx - 模拟复杂业务场景（多次查询）
    /// 注意：这里使用生成的repository方法
    /// </summary>
    [Benchmark]
    public int Sqlx_BusinessScenario()
    {
        // 1. 查询活跃用户 - 使用GetByAgeAndStatusSync模拟
        var activeUsers = _userRepository.GetByAgeAndStatusSync(0, 1);
        int totalCount = activeUsers.Count;

        // 2. 查询高薪用户 - 使用ComplexQuerySync
        var highSalaryUsers = _userRepository.ComplexQuerySync(20, 70, 5);
        totalCount += highSalaryUsers.Count;

        // 3. 再次查询 - 测试多次调用
        var allUsers = _userRepository.GetTopNSync(100);

        return totalCount;
    }
}
