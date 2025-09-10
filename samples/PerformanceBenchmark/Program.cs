// -----------------------------------------------------------------------
// Sqlx Performance Benchmark - 展示核心性能特性
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Core;

namespace Sqlx.PerformanceBenchmark;

/// <summary>
/// 简单的用户实体用于性能测试
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// 性能基准测试运行器
/// </summary>
public class BenchmarkRunner
{
    private readonly SQLiteConnection _connection;
    
    public BenchmarkRunner()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        SetupDatabase();
    }
    
    private void SetupDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                Age INTEGER NOT NULL,
                Department TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }
    
    /// <summary>
    /// 运行综合性能基准测试
    /// </summary>
    public async Task RunBenchmarksAsync()
    {
        Console.WriteLine("🚀 Sqlx 性能基准测试");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();
        
        await TestRawPerformance();
        await TestCachePerformance();
        await TestBatchOperations();
        await TestConnectionManagement();
        
        Console.WriteLine();
        Console.WriteLine("🎉 所有基准测试完成！");
        ShowCacheStatistics();
    }
    
    private async Task TestRawPerformance()
    {
        Console.WriteLine("⚡ 1. 原始性能测试");
        Console.WriteLine("-".PadRight(40, '-'));
        
        var sw = Stopwatch.StartNew();
        
        // 插入测试
        for (int i = 1; i <= 1000; i++)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO users (Name, Email, Age, Department) VALUES (@name, @email, @age, @dept)";
            cmd.Parameters.AddWithValue("@name", $"User{i}");
            cmd.Parameters.AddWithValue("@email", $"user{i}@test.com");
            cmd.Parameters.AddWithValue("@age", 20 + (i % 40));
            cmd.Parameters.AddWithValue("@dept", $"Dept{i % 5}");
            await cmd.ExecuteNonQueryAsync();
        }
        
        var insertTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"📝 插入 1000 条记录: {insertTime}ms");
        
        // 查询测试
        sw.Restart();
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM users";
        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
        }
        
        var selectTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"👁️ 查询 {count} 条记录: {selectTime}ms");
        Console.WriteLine($"⚡ 平均插入时间: {(double)insertTime / 1000:F2}ms/条");
        Console.WriteLine();
    }
    
    private Task TestCachePerformance()
    {
        Console.WriteLine("🧠 2. 缓存性能测试");
        Console.WriteLine("-".PadRight(40, '-'));
        
        // 测试缓存系统
        var cacheKey = "test_query_results";
        
        // 第一次查询（缓存未命中）
        var sw = Stopwatch.StartNew();
        var cached = IntelligentCacheManager.Get<string>(cacheKey);
        if (cached == null)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM users";
            var result = cmd.ExecuteScalar()?.ToString() ?? "0";
            IntelligentCacheManager.Set(cacheKey, result);
            cached = result;
        }
        var firstTime = sw.ElapsedMilliseconds;
        
        // 第二次查询（缓存命中）
        sw.Restart();
        var cachedResult = IntelligentCacheManager.Get<string>(cacheKey);
        if (cachedResult == null)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM users";
            cachedResult = cmd.ExecuteScalar()?.ToString() ?? "0";
            IntelligentCacheManager.Set(cacheKey, cachedResult);
        }
        var secondTime = sw.ElapsedMilliseconds;
        
        var speedup = firstTime > 0 ? (double)firstTime / Math.Max(secondTime, 1) : 1;
        
        Console.WriteLine($"🆕 首次查询 (缓存未命中): {firstTime}ms");
        Console.WriteLine($"⚡ 缓存查询 (缓存命中): {secondTime}ms");
        Console.WriteLine($"🎯 性能提升: {speedup:F1}x");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }
    
    private async Task TestBatchOperations()
    {
        Console.WriteLine("📦 3. 批量操作测试");
        Console.WriteLine("-".PadRight(40, '-'));
        
        // 准备批量数据
        var users = GenerateUsers(5000);
        
        // 使用事务进行批量插入
        var sw = Stopwatch.StartNew();
        using var transaction = _connection.BeginTransaction();
        try
        {
            foreach (var user in users)
            {
                using var cmd = _connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = "INSERT INTO users (Name, Email, Age, Department) VALUES (@name, @email, @age, @dept)";
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@age", user.Age);
                cmd.Parameters.AddWithValue("@dept", user.Department);
                await cmd.ExecuteNonQueryAsync();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        
        var batchTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"📝 批量插入 {users.Count} 条记录: {batchTime}ms");
        Console.WriteLine($"⚡ 平均批量插入时间: {(double)batchTime / users.Count:F3}ms/条");
        Console.WriteLine();
    }
    
    private async Task TestConnectionManagement()
    {
        Console.WriteLine("🔌 4. 连接管理测试");
        Console.WriteLine("-".PadRight(40, '-'));
        
        var sw = Stopwatch.StartNew();
        
        // 测试连接健康状况
        var health = AdvancedConnectionManager.GetConnectionHealth(_connection);
        Console.WriteLine($"🏥 连接健康状况: {health}");
        
        // 测试连接确保机制
        await AdvancedConnectionManager.EnsureConnectionOpenAsync(_connection);
        
        // 执行多个并发查询测试连接稳定性
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM users WHERE Age > @age";
                cmd.Parameters.AddWithValue("@age", 30);
                await cmd.ExecuteScalarAsync();
            }));
        }
        
        await Task.WhenAll(tasks);
        
        var connectionTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"🔄 10个并发查询: {connectionTime}ms");
        Console.WriteLine($"⚡ 平均查询时间: {(double)connectionTime / 10:F2}ms");
        Console.WriteLine();
    }
    
    private List<User> GenerateUsers(int count)
    {
        var random = new Random(42);
        var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance" };
        
        return Enumerable.Range(1, count)
            .Select(i => new User
            {
                Name = $"BatchUser{i:D5}",
                Email = $"batch{i:D5}@test.com",
                Age = 22 + random.Next(38),
                Department = departments[random.Next(departments.Length)]
            })
            .ToList();
    }
    
    private void ShowCacheStatistics()
    {
        var stats = IntelligentCacheManager.GetStatistics();
        Console.WriteLine("📊 缓存统计信息:");
        Console.WriteLine($"   命中次数: {stats.HitCount:N0}");
        Console.WriteLine($"   未命中次数: {stats.MissCount:N0}");
        if (stats.HitCount + stats.MissCount > 0)
        {
            var hitRatio = (double)stats.HitCount / (stats.HitCount + stats.MissCount);
            Console.WriteLine($"   命中率: {hitRatio:P2}");
        }
        Console.WriteLine($"   缓存条目数: {stats.EntryCount:N0}");
    }
}

/// <summary>
/// 程序入口点
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🌟 Sqlx 性能基准测试套件");
        Console.WriteLine("展示 Sqlx 的高性能特性和企业级功能");
        Console.WriteLine();
        
        try
        {
            var benchmark = new BenchmarkRunner();
            await benchmark.RunBenchmarksAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 基准测试失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}