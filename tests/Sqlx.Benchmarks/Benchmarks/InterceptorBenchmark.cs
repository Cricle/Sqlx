using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Interceptors;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// 拦截器性能测试
/// 测试拦截器对SQL执行性能的影响
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class InterceptorBenchmark
{
    private SqliteConnection _connection = null!;
    private const string Sql = "SELECT 1";

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                salary REAL NOT NULL,
                is_active INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        cmd.ExecuteNonQuery();

        // 插入测试数据
        cmd.CommandText = @"
            INSERT INTO users (id, name, email, age, salary, is_active, created_at)
            VALUES (1, 'Test User', 'test@example.com', 30, 50000.0, 1, '2024-01-01')";
        cmd.ExecuteNonQuery();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        SqlxInterceptors.Clear();
        _connection?.Dispose();
    }

    /// <summary>
    /// 基准：原始ADO.NET查询（无拦截器）
    /// </summary>
    [Benchmark(Baseline = true)]
    public int RawAdoNet()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM users WHERE id = 1";
        return (int)(long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// 0个拦截器（拦截器功能禁用）
    /// </summary>
    [Benchmark]
    public int NoInterceptor_Disabled()
    {
        SqlxInterceptors.IsEnabled = false;

        var ctx = new SqlxExecutionContext(
            "GetUser",
            "UserRepository",
            "SELECT id FROM users WHERE id = 1");

        int result;
        try
        {
            SqlxInterceptors.OnExecuting(ref ctx); // 快速退出

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users WHERE id = 1";
            result = (int)(long)cmd.ExecuteScalar()!;

            ctx.EndTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            SqlxInterceptors.OnExecuted(ref ctx); // 快速退出
        }
        catch (Exception ex)
        {
            ctx.Exception = ex;
            SqlxInterceptors.OnFailed(ref ctx); // 快速退出
            throw;
        }

        return result;
    }

    /// <summary>
    /// 0个拦截器（拦截器功能启用但无注册）
    /// </summary>
    [Benchmark]
    public int NoInterceptor_Enabled()
    {
        SqlxInterceptors.IsEnabled = true;
        SqlxInterceptors.Clear();

        var ctx = new SqlxExecutionContext(
            "GetUser",
            "UserRepository",
            "SELECT id FROM users WHERE id = 1");

        int result;
        try
        {
            SqlxInterceptors.OnExecuting(ref ctx); // Count = 0，快速退出

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users WHERE id = 1";
            result = (int)(long)cmd.ExecuteScalar()!;

            ctx.EndTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            SqlxInterceptors.OnExecuted(ref ctx);
        }
        catch (Exception ex)
        {
            ctx.Exception = ex;
            SqlxInterceptors.OnFailed(ref ctx);
            throw;
        }

        return result;
    }

    /// <summary>
    /// 1个拦截器（Activity追踪）
    /// </summary>
    [Benchmark]
    public int OneInterceptor_Activity()
    {
        SqlxInterceptors.IsEnabled = true;
        SqlxInterceptors.Clear();
        SqlxInterceptors.Add(new ActivityInterceptor());

        var ctx = new SqlxExecutionContext(
            "GetUser",
            "UserRepository",
            "SELECT id FROM users WHERE id = 1");

        int result;
        try
        {
            SqlxInterceptors.OnExecuting(ref ctx);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users WHERE id = 1";
            result = (int)(long)cmd.ExecuteScalar()!;

            ctx.EndTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            SqlxInterceptors.OnExecuted(ref ctx);
        }
        catch (Exception ex)
        {
            ctx.Exception = ex;
            SqlxInterceptors.OnFailed(ref ctx);
            throw;
        }

        return result;
    }

    /// <summary>
    /// 1个拦截器（简单计数器）
    /// </summary>
    [Benchmark]
    public int OneInterceptor_Counter()
    {
        SqlxInterceptors.IsEnabled = true;
        SqlxInterceptors.Clear();
        SqlxInterceptors.Add(new CounterInterceptor());

        var ctx = new SqlxExecutionContext(
            "GetUser",
            "UserRepository",
            "SELECT id FROM users WHERE id = 1");

        int result;
        try
        {
            SqlxInterceptors.OnExecuting(ref ctx);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users WHERE id = 1";
            result = (int)(long)cmd.ExecuteScalar()!;

            ctx.EndTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            SqlxInterceptors.OnExecuted(ref ctx);
        }
        catch (Exception ex)
        {
            ctx.Exception = ex;
            SqlxInterceptors.OnFailed(ref ctx);
            throw;
        }

        return result;
    }

    /// <summary>
    /// 3个拦截器
    /// </summary>
    [Benchmark]
    public int ThreeInterceptors()
    {
        SqlxInterceptors.IsEnabled = true;
        SqlxInterceptors.Clear();
        SqlxInterceptors.Add(new ActivityInterceptor());
        SqlxInterceptors.Add(new CounterInterceptor());
        SqlxInterceptors.Add(new NoOpInterceptor());

        var ctx = new SqlxExecutionContext(
            "GetUser",
            "UserRepository",
            "SELECT id FROM users WHERE id = 1");

        int result;
        try
        {
            SqlxInterceptors.OnExecuting(ref ctx);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users WHERE id = 1";
            result = (int)(long)cmd.ExecuteScalar()!;

            ctx.EndTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            SqlxInterceptors.OnExecuted(ref ctx);
        }
        catch (Exception ex)
        {
            ctx.Exception = ex;
            SqlxInterceptors.OnFailed(ref ctx);
            throw;
        }

        return result;
    }

    /// <summary>
    /// 8个拦截器（最大数量）
    /// </summary>
    [Benchmark]
    public int EightInterceptors_Max()
    {
        SqlxInterceptors.IsEnabled = true;
        SqlxInterceptors.Clear();
        for (int i = 0; i < 8; i++)
        {
            SqlxInterceptors.Add(new NoOpInterceptor());
        }

        var ctx = new SqlxExecutionContext(
            "GetUser",
            "UserRepository",
            "SELECT id FROM users WHERE id = 1");

        int result;
        try
        {
            SqlxInterceptors.OnExecuting(ref ctx);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users WHERE id = 1";
            result = (int)(long)cmd.ExecuteScalar()!;

            ctx.EndTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            SqlxInterceptors.OnExecuted(ref ctx);
        }
        catch (Exception ex)
        {
            ctx.Exception = ex;
            SqlxInterceptors.OnFailed(ref ctx);
            throw;
        }

        return result;
    }
}

/// <summary>
/// 简单计数器拦截器（用于测试）
/// </summary>
public class CounterInterceptor : ISqlxInterceptor
{
    private int _count;

    public void OnExecuting(ref SqlxExecutionContext context)
    {
        System.Threading.Interlocked.Increment(ref _count);
    }

    public void OnExecuted(ref SqlxExecutionContext context) { }
    public void OnFailed(ref SqlxExecutionContext context) { }
}

/// <summary>
/// 空操作拦截器（用于测试）
/// </summary>
public class NoOpInterceptor : ISqlxInterceptor
{
    public void OnExecuting(ref SqlxExecutionContext context) { }
    public void OnExecuted(ref SqlxExecutionContext context) { }
    public void OnFailed(ref SqlxExecutionContext context) { }
}

