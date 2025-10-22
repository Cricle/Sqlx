using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// CRUD操作性能测试
/// 对比 Sqlx、Dapper、原始 ADO.NET 的增删改查性能
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CrudBenchmark
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

        // 初始化Sqlx Repository
        _userRepository = new UserRepository(_connection);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // 每次迭代前清空表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users";
        cmd.ExecuteNonQuery();
    }

    // ============================================================
    // INSERT 操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 插入
    /// </summary>
    [Benchmark(Baseline = true)]
    public int RawAdoNet_Insert()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, email, age, salary, is_active, created_at)
            VALUES (@name, @email, @age, @salary, @isActive, @createdAt)";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@name";
        p1.Value = "TestUser";
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@email";
        p2.Value = "test@example.com";
        cmd.Parameters.Add(p2);

        var p3 = cmd.CreateParameter();
        p3.ParameterName = "@age";
        p3.Value = 30;
        cmd.Parameters.Add(p3);

        var p4 = cmd.CreateParameter();
        p4.ParameterName = "@salary";
        p4.Value = 50000.0;
        cmd.Parameters.Add(p4);

        var p5 = cmd.CreateParameter();
        p5.ParameterName = "@isActive";
        p5.Value = 1;
        cmd.Parameters.Add(p5);

        var p6 = cmd.CreateParameter();
        p6.ParameterName = "@createdAt";
        p6.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        cmd.Parameters.Add(p6);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Dapper - 插入
    /// </summary>
    [Benchmark]
    public int Dapper_Insert()
    {
        return _connection.Execute(@"
            INSERT INTO users (name, email, age, salary, is_active, created_at)
            VALUES (@name, @email, @age, @salary, @isActive, @createdAt)",
            new
            {
                name = "TestUser",
                email = "test@example.com",
                age = 30,
                salary = 50000.0,
                isActive = 1,
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }

    /// <summary>
    /// Sqlx - 插入（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public int Sqlx_Insert()
    {
        return _userRepository.InsertSync(
            "TestUser",
            "test@example.com",
            30,
            50000m,
            1,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    // ============================================================
    // UPDATE 操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 更新
    /// </summary>
    [Benchmark]
    public int RawAdoNet_Update()
    {
        // 先插入一条记录
        using (var insertCmd = _connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO users (name, email, age, salary, is_active, created_at) VALUES ('OldUser', 'old@example.com', 25, 40000, 1, '2024-01-01')";
            insertCmd.ExecuteNonQuery();
        }

        // 更新记录
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE users SET name = @name, email = @email, age = @age, salary = @salary, updated_at = @updatedAt
            WHERE id = 1";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@name";
        p1.Value = "UpdatedUser";
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@email";
        p2.Value = "updated@example.com";
        cmd.Parameters.Add(p2);

        var p3 = cmd.CreateParameter();
        p3.ParameterName = "@age";
        p3.Value = 35;
        cmd.Parameters.Add(p3);

        var p4 = cmd.CreateParameter();
        p4.ParameterName = "@salary";
        p4.Value = 60000.0;
        cmd.Parameters.Add(p4);

        var p5 = cmd.CreateParameter();
        p5.ParameterName = "@updatedAt";
        p5.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        cmd.Parameters.Add(p5);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Dapper - 更新
    /// </summary>
    [Benchmark]
    public int Dapper_Update()
    {
        // 先插入一条记录
        _connection.Execute("INSERT INTO users (name, email, age, salary, is_active, created_at) VALUES ('OldUser', 'old@example.com', 25, 40000, 1, '2024-01-01')");

        // 更新记录
        return _connection.Execute(@"
            UPDATE users SET name = @name, email = @email, age = @age, salary = @salary, updated_at = @updatedAt
            WHERE id = 1",
            new
            {
                name = "UpdatedUser",
                email = "updated@example.com",
                age = 35,
                salary = 60000.0,
                updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }

    /// <summary>
    /// Sqlx - 更新（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public int Sqlx_Update()
    {
        // 先插入一条记录
        _userRepository.InsertSync("OldUser", "old@example.com", 25, 40000m, 1, "2024-01-01");

        // 更新记录
        return _userRepository.UpdateSync(
            1,
            "UpdatedUser",
            "updated@example.com",
            35,
            60000m,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    // ============================================================
    // DELETE 操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 删除
    /// </summary>
    [Benchmark]
    public int RawAdoNet_Delete()
    {
        // 先插入一条记录
        using (var insertCmd = _connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO users (name, email, age, salary, is_active, created_at) VALUES ('ToDelete', 'delete@example.com', 30, 50000, 1, '2024-01-01')";
            insertCmd.ExecuteNonQuery();
        }

        // 删除记录
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE id = 1";

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Dapper - 删除
    /// </summary>
    [Benchmark]
    public int Dapper_Delete()
    {
        // 先插入一条记录
        _connection.Execute("INSERT INTO users (name, email, age, salary, is_active, created_at) VALUES ('ToDelete', 'delete@example.com', 30, 50000, 1, '2024-01-01')");

        // 删除记录
        return _connection.Execute("DELETE FROM users WHERE id = @id", new { id = 1 });
    }

    /// <summary>
    /// Sqlx - 删除（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public int Sqlx_Delete()
    {
        // 先插入一条记录
        _userRepository.InsertSync("ToDelete", "delete@example.com", 30, 50000m, 1, "2024-01-01");

        // 删除记录
        return _userRepository.DeleteSync(1);
    }

    // ============================================================
    // 批量INSERT
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 批量插入10条
    /// </summary>
    [Benchmark]
    public int RawAdoNet_BatchInsert()
    {
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO users (name, email, age, salary, is_active, created_at)
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt)";

            cmd.Parameters.Add(CreateParameter(cmd, "@name", $"User{i}"));
            cmd.Parameters.Add(CreateParameter(cmd, "@email", $"user{i}@example.com"));
            cmd.Parameters.Add(CreateParameter(cmd, "@age", 20 + i));
            cmd.Parameters.Add(CreateParameter(cmd, "@salary", 30000.0 + i * 1000));
            cmd.Parameters.Add(CreateParameter(cmd, "@isActive", 1));
            cmd.Parameters.Add(CreateParameter(cmd, "@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            count += cmd.ExecuteNonQuery();
        }
        return count;
    }

    /// <summary>
    /// Dapper - 批量插入10条
    /// </summary>
    [Benchmark]
    public int Dapper_BatchInsert()
    {
        var users = Enumerable.Range(0, 10).Select(i => new
        {
            name = $"User{i}",
            email = $"user{i}@example.com",
            age = 20 + i,
            salary = 30000.0 + i * 1000,
            isActive = 1,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        return _connection.Execute(@"
            INSERT INTO users (name, email, age, salary, is_active, created_at)
            VALUES (@name, @email, @age, @salary, @isActive, @createdAt)", users);
    }

    /// <summary>
    /// Sqlx - 批量插入10条（使用源生成器生成的代码）
    /// </summary>
    [Benchmark]
    public int Sqlx_BatchInsert()
    {
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            count += _userRepository.InsertSync(
                $"User{i}",
                $"user{i}@example.com",
                20 + i,
                30000m + i * 1000,
                1,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        return count;
    }

    private IDbDataParameter CreateParameter(IDbCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        return param;
    }
}
