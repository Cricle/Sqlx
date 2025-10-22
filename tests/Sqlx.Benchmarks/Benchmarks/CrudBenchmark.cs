using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// CRUD 操作性能测试
/// 对比 INSERT/UPDATE/DELETE 操作的性能
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CrudBenchmark
{
    private SqliteConnection _connection = null!;
    private int _insertId = 1000;

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

        // 插入初始数据
        for (int i = 1; i <= 100; i++)
        {
            cmd.CommandText = $@"
                INSERT INTO users (name, email, age, salary, is_active, created_at)
                VALUES ('User{i}', 'user{i}@example.com', {20 + i % 50}, {30000 + i * 1000}, {i % 2}, '2024-01-01')";
            cmd.ExecuteNonQuery();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ============================================================
    // INSERT 操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 单条插入
    /// </summary>
    [Benchmark(Baseline = true)]
    public int RawAdoNet_Insert()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, email, age, salary, is_active, created_at)
            VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
            SELECT last_insert_rowid();";

        AddParameter(cmd, "@name", DbType.String, $"NewUser{_insertId}");
        AddParameter(cmd, "@email", DbType.String, $"newuser{_insertId}@example.com");
        AddParameter(cmd, "@age", DbType.Int32, 25);
        AddParameter(cmd, "@salary", DbType.Double, 50000.0);
        AddParameter(cmd, "@isActive", DbType.Int32, 1);
        AddParameter(cmd, "@createdAt", DbType.String, "2024-01-01");

        var result = (int)(long)cmd.ExecuteScalar()!;
        _insertId++;
        return result;
    }

    /// <summary>
    /// Dapper - 单条插入
    /// </summary>
    [Benchmark]
    public int Dapper_Insert()
    {
        var sql = @"
            INSERT INTO users (name, email, age, salary, is_active, created_at)
            VALUES (@Name, @Email, @Age, @Salary, @IsActive, @CreatedAt);
            SELECT last_insert_rowid();";

        var result = _connection.ExecuteScalar<int>(sql, new
        {
            Name = $"NewUser{_insertId}",
            Email = $"newuser{_insertId}@example.com",
            Age = 25,
            Salary = 50000.0,
            IsActive = 1,
            CreatedAt = "2024-01-01"
        });

        _insertId++;
        return result;
    }

    /// <summary>
    /// Sqlx模拟 - 单条插入
    /// </summary>
    [Benchmark]
    public int Sqlx_Insert()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, email, age, salary, is_active, created_at)
            VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
            SELECT last_insert_rowid();";

        AddParameter(cmd, "@name", DbType.String, $"NewUser{_insertId}");
        AddParameter(cmd, "@email", DbType.String, $"newuser{_insertId}@example.com");
        AddParameter(cmd, "@age", DbType.Int32, 25);
        AddParameter(cmd, "@salary", DbType.Double, 50000.0);
        AddParameter(cmd, "@isActive", DbType.Int32, 1);
        AddParameter(cmd, "@createdAt", DbType.String, "2024-01-01");

        var result = (int)(long)cmd.ExecuteScalar()!;
        _insertId++;
        return result;
    }

    // ============================================================
    // UPDATE 操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 单条更新
    /// </summary>
    [Benchmark]
    public int RawAdoNet_Update()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE users
            SET salary = @salary, updated_at = @updatedAt
            WHERE id = @id";

        AddParameter(cmd, "@salary", DbType.Double, 60000.0);
        AddParameter(cmd, "@updatedAt", DbType.String, "2024-06-01");
        AddParameter(cmd, "@id", DbType.Int32, 1);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Dapper - 单条更新
    /// </summary>
    [Benchmark]
    public int Dapper_Update()
    {
        return _connection.Execute(
            "UPDATE users SET salary = @Salary, updated_at = @UpdatedAt WHERE id = @Id",
            new { Salary = 60000.0, UpdatedAt = "2024-06-01", Id = 1 });
    }

    /// <summary>
    /// Sqlx模拟 - 单条更新
    /// </summary>
    [Benchmark]
    public int Sqlx_Update()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE users
            SET salary = @salary, updated_at = @updatedAt
            WHERE id = @id";

        AddParameter(cmd, "@salary", DbType.Double, 60000.0);
        AddParameter(cmd, "@updatedAt", DbType.String, "2024-06-01");
        AddParameter(cmd, "@id", DbType.Int32, 1);

        return cmd.ExecuteNonQuery();
    }

    // ============================================================
    // DELETE 操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 条件删除
    /// </summary>
    [Benchmark]
    public int RawAdoNet_Delete()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE id > @id";

        AddParameter(cmd, "@id", DbType.Int32, 1000);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Dapper - 条件删除
    /// </summary>
    [Benchmark]
    public int Dapper_Delete()
    {
        return _connection.Execute("DELETE FROM users WHERE id > @Id", new { Id = 1000 });
    }

    /// <summary>
    /// Sqlx模拟 - 条件删除
    /// </summary>
    [Benchmark]
    public int Sqlx_Delete()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE id > @id";

        AddParameter(cmd, "@id", DbType.Int32, 1000);

        return cmd.ExecuteNonQuery();
    }

    // ============================================================
    // 批量操作
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 批量插入（10条）
    /// </summary>
    [Benchmark]
    public int RawAdoNet_BulkInsert()
    {
        int count = 0;
        using var transaction = _connection.BeginTransaction();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO users (name, email, age, salary, is_active, created_at)
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt)";

            for (int i = 0; i < 10; i++)
            {
                cmd.Parameters.Clear();
                AddParameter(cmd, "@name", DbType.String, $"BulkUser{_insertId + i}");
                AddParameter(cmd, "@email", DbType.String, $"bulk{_insertId + i}@example.com");
                AddParameter(cmd, "@age", DbType.Int32, 25);
                AddParameter(cmd, "@salary", DbType.Double, 50000.0);
                AddParameter(cmd, "@isActive", DbType.Int32, 1);
                AddParameter(cmd, "@createdAt", DbType.String, "2024-01-01");

                count += cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            _insertId += 10;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return count;
    }

    /// <summary>
    /// Dapper - 批量插入（10条）
    /// </summary>
    [Benchmark]
    public int Dapper_BulkInsert()
    {
        var users = new List<object>();
        for (int i = 0; i < 10; i++)
        {
            users.Add(new
            {
                Name = $"BulkUser{_insertId + i}",
                Email = $"bulk{_insertId + i}@example.com",
                Age = 25,
                Salary = 50000.0,
                IsActive = 1,
                CreatedAt = "2024-01-01"
            });
        }

        int count;
        using var transaction = _connection.BeginTransaction();
        try
        {
            count = _connection.Execute(
                @"INSERT INTO users (name, email, age, salary, is_active, created_at)
                  VALUES (@Name, @Email, @Age, @Salary, @IsActive, @CreatedAt)",
                users, transaction);

            transaction.Commit();
            _insertId += 10;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return count;
    }

    /// <summary>
    /// Sqlx模拟 - 批量插入（10条）
    /// </summary>
    [Benchmark]
    public int Sqlx_BulkInsert()
    {
        int count = 0;
        using var transaction = _connection.BeginTransaction();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO users (name, email, age, salary, is_active, created_at)
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt)";

            for (int i = 0; i < 10; i++)
            {
                cmd.Parameters.Clear();
                AddParameter(cmd, "@name", DbType.String, $"BulkUser{_insertId + i}");
                AddParameter(cmd, "@email", DbType.String, $"bulk{_insertId + i}@example.com");
                AddParameter(cmd, "@age", DbType.Int32, 25);
                AddParameter(cmd, "@salary", DbType.Double, 50000.0);
                AddParameter(cmd, "@isActive", DbType.Int32, 1);
                AddParameter(cmd, "@createdAt", DbType.String, "2024-01-01");

                count += cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            _insertId += 10;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return count;
    }

    // ============================================================
    // 辅助方法
    // ============================================================

    private static void AddParameter(IDbCommand cmd, string name, DbType type, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.DbType = type;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}


