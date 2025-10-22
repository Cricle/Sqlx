using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// 复杂查询性能测试
/// 测试 JOIN、聚合、分页等复杂查询场景
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ComplexQueryBenchmark
{
    private SqliteConnection _connection = null!;

    public class UserWithDepartment
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class AggregateResult
    {
        public int DepartmentId { get; set; }
        public int UserCount { get; set; }
        public double AvgSalary { get; set; }
        public double MaxSalary { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();

        // 创建部门表
        cmd.CommandText = @"
            CREATE TABLE departments (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                location TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        // 创建用户表
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                salary REAL NOT NULL,
                department_id INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY (department_id) REFERENCES departments(id)
            )";
        cmd.ExecuteNonQuery();

        // 插入部门数据
        for (int i = 1; i <= 10; i++)
        {
            cmd.CommandText = $"INSERT INTO departments (id, name, location) VALUES ({i}, 'Dept{i}', 'Location{i}')";
            cmd.ExecuteNonQuery();
        }

        // 插入用户数据（每个部门10个用户）
        for (int i = 1; i <= 100; i++)
        {
            int deptId = (i - 1) % 10 + 1;
            cmd.CommandText = $@"
                INSERT INTO users (id, name, email, age, salary, department_id, is_active, created_at)
                VALUES ({i}, 'User{i}', 'user{i}@example.com', {20 + i % 50}, {30000 + i * 500}, {deptId}, {i % 2}, '2024-01-01')";
            cmd.ExecuteNonQuery();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ============================================================
    // JOIN 查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - JOIN查询
    /// </summary>
    [Benchmark(Baseline = true)]
    public List<UserWithDepartment> RawAdoNet_Join()
    {
        var results = new List<UserWithDepartment>(100);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.id, u.name, u.email, d.id, d.name
            FROM users u
            INNER JOIN departments d ON u.department_id = d.id
            WHERE u.is_active = 1";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new UserWithDepartment
            {
                UserId = reader.GetInt32(0),
                UserName = reader.GetString(1),
                Email = reader.GetString(2),
                DepartmentId = reader.GetInt32(3),
                DepartmentName = reader.GetString(4)
            });
        }
        return results;
    }

    /// <summary>
    /// Dapper - JOIN查询
    /// </summary>
    [Benchmark]
    public List<UserWithDepartment> Dapper_Join()
    {
        return _connection.Query<UserWithDepartment>(@"
            SELECT u.id as UserId, u.name as UserName, u.email as Email,
                   d.id as DepartmentId, d.name as DepartmentName
            FROM users u
            INNER JOIN departments d ON u.department_id = d.id
            WHERE u.is_active = 1").ToList();
    }

    /// <summary>
    /// Sqlx模拟 - JOIN查询
    /// </summary>
    [Benchmark]
    public List<UserWithDepartment> Sqlx_Join()
    {
        var results = new List<UserWithDepartment>(100);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.id, u.name, u.email, d.id, d.name
            FROM users u
            INNER JOIN departments d ON u.department_id = d.id
            WHERE u.is_active = @isActive";

        var p = cmd.CreateParameter();
        p.ParameterName = "@isActive";
        p.Value = 1;
        p.DbType = DbType.Int32;
        cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new UserWithDepartment
            {
                UserId = reader.GetInt32(0),
                UserName = reader.GetString(1),
                Email = reader.GetString(2),
                DepartmentId = reader.GetInt32(3),
                DepartmentName = reader.GetString(4)
            });
        }
        return results;
    }

    // ============================================================
    // 聚合查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 聚合查询
    /// </summary>
    [Benchmark]
    public List<AggregateResult> RawAdoNet_Aggregate()
    {
        var results = new List<AggregateResult>(10);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT department_id, COUNT(*) as user_count,
                   AVG(salary) as avg_salary, MAX(salary) as max_salary
            FROM users
            GROUP BY department_id
            HAVING COUNT(*) > 0
            ORDER BY department_id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new AggregateResult
            {
                DepartmentId = reader.GetInt32(0),
                UserCount = reader.GetInt32(1),
                AvgSalary = reader.GetDouble(2),
                MaxSalary = reader.GetDouble(3)
            });
        }
        return results;
    }

    /// <summary>
    /// Dapper - 聚合查询
    /// </summary>
    [Benchmark]
    public List<AggregateResult> Dapper_Aggregate()
    {
        return _connection.Query<AggregateResult>(@"
            SELECT department_id as DepartmentId, COUNT(*) as UserCount,
                   AVG(salary) as AvgSalary, MAX(salary) as MaxSalary
            FROM users
            GROUP BY department_id
            HAVING COUNT(*) > 0
            ORDER BY department_id").ToList();
    }

    /// <summary>
    /// Sqlx模拟 - 聚合查询
    /// </summary>
    [Benchmark]
    public List<AggregateResult> Sqlx_Aggregate()
    {
        var results = new List<AggregateResult>(10);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT department_id, COUNT(*) as user_count,
                   AVG(salary) as avg_salary, MAX(salary) as max_salary
            FROM users
            GROUP BY department_id
            HAVING COUNT(*) > 0
            ORDER BY department_id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new AggregateResult
            {
                DepartmentId = reader.GetInt32(0),
                UserCount = reader.GetInt32(1),
                AvgSalary = reader.GetDouble(2),
                MaxSalary = reader.GetDouble(3)
            });
        }
        return results;
    }

    // ============================================================
    // 分页查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 分页查询
    /// </summary>
    [Benchmark]
    public List<UserWithDepartment> RawAdoNet_Paging()
    {
        var results = new List<UserWithDepartment>(10);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.id, u.name, u.email, d.id, d.name
            FROM users u
            INNER JOIN departments d ON u.department_id = d.id
            ORDER BY u.id
            LIMIT @limit OFFSET @offset";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@limit";
        p1.Value = 10;
        p1.DbType = DbType.Int32;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@offset";
        p2.Value = 20;
        p2.DbType = DbType.Int32;
        cmd.Parameters.Add(p2);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new UserWithDepartment
            {
                UserId = reader.GetInt32(0),
                UserName = reader.GetString(1),
                Email = reader.GetString(2),
                DepartmentId = reader.GetInt32(3),
                DepartmentName = reader.GetString(4)
            });
        }
        return results;
    }

    /// <summary>
    /// Dapper - 分页查询
    /// </summary>
    [Benchmark]
    public List<UserWithDepartment> Dapper_Paging()
    {
        return _connection.Query<UserWithDepartment>(@"
            SELECT u.id as UserId, u.name as UserName, u.email as Email,
                   d.id as DepartmentId, d.name as DepartmentName
            FROM users u
            INNER JOIN departments d ON u.department_id = d.id
            ORDER BY u.id
            LIMIT @Limit OFFSET @Offset",
            new { Limit = 10, Offset = 20 }).ToList();
    }

    /// <summary>
    /// Sqlx模拟 - 分页查询
    /// </summary>
    [Benchmark]
    public List<UserWithDepartment> Sqlx_Paging()
    {
        var results = new List<UserWithDepartment>(10);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.id, u.name, u.email, d.id, d.name
            FROM users u
            INNER JOIN departments d ON u.department_id = d.id
            ORDER BY u.id
            LIMIT @limit OFFSET @offset";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@limit";
        p1.Value = 10;
        p1.DbType = DbType.Int32;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@offset";
        p2.Value = 20;
        p2.DbType = DbType.Int32;
        cmd.Parameters.Add(p2);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new UserWithDepartment
            {
                UserId = reader.GetInt32(0),
                UserName = reader.GetString(1),
                Email = reader.GetString(2),
                DepartmentId = reader.GetInt32(3),
                DepartmentName = reader.GetString(4)
            });
        }
        return results;
    }

    // ============================================================
    // 子查询
    // ============================================================

    /// <summary>
    /// 原始ADO.NET - 子查询
    /// </summary>
    [Benchmark]
    public int RawAdoNet_Subquery()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*)
            FROM users
            WHERE salary > (SELECT AVG(salary) FROM users)";

        return (int)(long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// Dapper - 子查询
    /// </summary>
    [Benchmark]
    public int Dapper_Subquery()
    {
        return _connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM users
            WHERE salary > (SELECT AVG(salary) FROM users)");
    }

    /// <summary>
    /// Sqlx模拟 - 子查询
    /// </summary>
    [Benchmark]
    public int Sqlx_Subquery()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*)
            FROM users
            WHERE salary > (SELECT AVG(salary) FROM users)";

        return (int)(long)cmd.ExecuteScalar()!;
    }
}


