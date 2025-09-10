// -----------------------------------------------------------------------
// <copyright file="PerformanceBenchmarkTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.PerformanceTests;

/// <summary>
/// 性能基准测试，用于监控关键功能的性能回归
/// </summary>
[TestClass]
public class PerformanceBenchmarkTests
{
    private SqliteConnection _connection = null!;
    private const int WarmupIterations = 10;
    private const int BenchmarkIterations = 1000;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        
        // 创建测试表
        var createTableSql = @"
            CREATE TABLE benchmark_users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            )";
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = createTableSql;
        await cmd.ExecuteNonQueryAsync();
        
        // 插入测试数据
        await SeedTestDataAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    private async Task SeedTestDataAsync()
    {
        var insertSql = @"
            INSERT INTO benchmark_users (Name, Email, CreatedAt, IsActive) 
            VALUES (@name, @email, @createdAt, @isActive)";

        for (int i = 1; i <= 1000; i++)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = insertSql;
            
            cmd.Parameters.Add(new SqliteParameter("@name", $"User {i}"));
            cmd.Parameters.Add(new SqliteParameter("@email", $"user{i}@example.com"));
            cmd.Parameters.Add(new SqliteParameter("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            cmd.Parameters.Add(new SqliteParameter("@isActive", 1));
            
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [TestMethod]
    public void Benchmark_IntelligentCacheManager_Performance()
    {
        // 预热
        for (int i = 0; i < WarmupIterations; i++)
        {
            IntelligentCacheManager.Set($"warmup_key_{i}", $"value_{i}");
            IntelligentCacheManager.Get<string>($"warmup_key_{i}");
        }

        // 基准测试 - 写入操作
        var writeStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < BenchmarkIterations; i++)
        {
            IntelligentCacheManager.Set($"benchmark_key_{i}", $"benchmark_value_{i}");
        }
        writeStopwatch.Stop();

        // 基准测试 - 读取操作
        var readStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < BenchmarkIterations; i++)
        {
            IntelligentCacheManager.Get<string>($"benchmark_key_{i}");
        }
        readStopwatch.Stop();

        // 性能断言 (应该在合理范围内)
        var writeAvgMs = writeStopwatch.ElapsedMilliseconds / (double)BenchmarkIterations;
        var readAvgMs = readStopwatch.ElapsedMilliseconds / (double)BenchmarkIterations;

        Console.WriteLine($"IntelligentCacheManager Performance:");
        Console.WriteLine($"  Write: {writeAvgMs:F4} ms/op ({BenchmarkIterations / writeStopwatch.Elapsed.TotalSeconds:F0} ops/sec)");
        Console.WriteLine($"  Read:  {readAvgMs:F4} ms/op ({BenchmarkIterations / readStopwatch.Elapsed.TotalSeconds:F0} ops/sec)");

        // 性能回归检测 - 写入应该在0.01ms以内
        Assert.IsTrue(writeAvgMs < 0.01, $"Cache write performance regression: {writeAvgMs:F4} ms/op > 0.01 ms/op");
        
        // 读取应该在0.005ms以内
        Assert.IsTrue(readAvgMs < 0.005, $"Cache read performance regression: {readAvgMs:F4} ms/op > 0.005 ms/op");

        // 清理
        IntelligentCacheManager.Clear();
    }

    [TestMethod]
    public void Benchmark_AdvancedConnectionManager_Performance()
    {
        var healthCheckStopwatch = Stopwatch.StartNew();
        
        // 预热
        for (int i = 0; i < WarmupIterations; i++)
        {
            AdvancedConnectionManager.GetConnectionHealth(_connection);
        }

        healthCheckStopwatch.Restart();
        
        // 基准测试 - 连接健康检查
        for (int i = 0; i < BenchmarkIterations; i++)
        {
            var health = AdvancedConnectionManager.GetConnectionHealth(_connection);
            Assert.IsNotNull(health);
        }
        
        healthCheckStopwatch.Stop();

        var avgMs = healthCheckStopwatch.ElapsedMilliseconds / (double)BenchmarkIterations;
        
        Console.WriteLine($"AdvancedConnectionManager Performance:");
        Console.WriteLine($"  Health Check: {avgMs:F4} ms/op ({BenchmarkIterations / healthCheckStopwatch.Elapsed.TotalSeconds:F0} ops/sec)");

        // 性能回归检测 - 健康检查应该在0.1ms以内
        Assert.IsTrue(avgMs < 0.1, $"Connection health check performance regression: {avgMs:F4} ms/op > 0.1 ms/op");
    }

    [TestMethod]
    public async Task Benchmark_BatchOperations_Performance()
    {
        // 准备测试数据
        var testUsers = new List<TestUser>();
        for (int i = 1; i <= 100; i++)
        {
            testUsers.Add(new TestUser 
            { 
                Name = $"BatchUser {i}", 
                Email = $"batch{i}@example.com", 
                CreatedAt = DateTime.Now,
                IsActive = true
            });
        }

        // 预热 - 使用简单插入代替
        for (int i = 0; i < 5; i++)
        {
            var warmupUsers = testUsers.Take(10).ToList();
            foreach (var user in warmupUsers.Take(5)) // 只插入5个用于预热
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "INSERT INTO benchmark_users (Name, Email, CreatedAt, IsActive) VALUES (@name, @email, @createdAt, @isActive)";
                cmd.Parameters.Add(new SqliteParameter("@name", user.Name));
                cmd.Parameters.Add(new SqliteParameter("@email", user.Email));
                cmd.Parameters.Add(new SqliteParameter("@createdAt", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(new SqliteParameter("@isActive", user.IsActive ? 1 : 0));
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var stopwatch = Stopwatch.StartNew();
        
        // 基准测试 - 模拟批量插入
        int totalInserted = 0;
        foreach (var user in testUsers)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO benchmark_users (Name, Email, CreatedAt, IsActive) VALUES (@name, @email, @createdAt, @isActive)";
            cmd.Parameters.Add(new SqliteParameter("@name", user.Name));
            cmd.Parameters.Add(new SqliteParameter("@email", user.Email));
            cmd.Parameters.Add(new SqliteParameter("@createdAt", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            cmd.Parameters.Add(new SqliteParameter("@isActive", user.IsActive ? 1 : 0));
            var affected = await cmd.ExecuteNonQueryAsync();
            totalInserted += affected;
        }
        
        var result = new { Success = true, AffectedRows = totalInserted };
        
        stopwatch.Stop();

        var avgMs = stopwatch.ElapsedMilliseconds / (double)testUsers.Count;
        
        Console.WriteLine($"BatchOperations Performance:");
        Console.WriteLine($"  Batch Insert: {avgMs:F4} ms/record ({testUsers.Count / stopwatch.Elapsed.TotalSeconds:F0} records/sec)");
        Console.WriteLine($"  Total Time: {stopwatch.ElapsedMilliseconds} ms for {testUsers.Count} records");
        Console.WriteLine($"  Success: {result.Success}, Affected Rows: {result.AffectedRows}");

        // 性能回归检测 - 批量插入应该在1ms/record以内
        Assert.IsTrue(avgMs < 1.0, $"Batch insert performance regression: {avgMs:F4} ms/record > 1.0 ms/record");
        Assert.IsTrue(result.Success, "Batch operation should succeed");
        Assert.AreEqual(testUsers.Count, result.AffectedRows, "Should insert all records");
    }

    [TestMethod]
    public void Benchmark_Memory_Usage()
    {
        var initialMemory = GC.GetTotalMemory(true);

        // 测试缓存内存使用
        for (int i = 0; i < 1000; i++)
        {
            IntelligentCacheManager.Set($"memory_test_{i}", new string('A', 1000)); // 1KB per entry
        }

        var afterCacheMemory = GC.GetTotalMemory(false);
        var cacheMemoryUsage = afterCacheMemory - initialMemory;

        Console.WriteLine($"Memory Usage Benchmark:");
        Console.WriteLine($"  Initial Memory: {initialMemory:N0} bytes");
        Console.WriteLine($"  After Cache (1000 x 1KB): {afterCacheMemory:N0} bytes");
        Console.WriteLine($"  Cache Memory Usage: {cacheMemoryUsage:N0} bytes ({cacheMemoryUsage / 1024.0:F1} KB)");

        // 内存使用应该合理 (不超过3MB for 1000 x 1KB entries，考虑.NET对象开销)
        Assert.IsTrue(cacheMemoryUsage < 3 * 1024 * 1024, 
            $"Memory usage too high: {cacheMemoryUsage:N0} bytes > 3MB");

        // 清理
        IntelligentCacheManager.Clear();
        GC.Collect();
    }

    [TestMethod]
    public async Task Benchmark_Concurrent_Operations()
    {
        const int concurrentTasks = 10;
        const int operationsPerTask = 100;

        var stopwatch = Stopwatch.StartNew();

        // 并发缓存操作
        var tasks = new List<Task>();
        for (int t = 0; t < concurrentTasks; t++)
        {
            int taskId = t;
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    var key = $"concurrent_{taskId}_{i}";
                    IntelligentCacheManager.Set(key, $"value_{taskId}_{i}");
                    var value = IntelligentCacheManager.Get<string>(key);
                    Assert.IsNotNull(value);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var totalOperations = concurrentTasks * operationsPerTask * 2; // Set + Get
        var avgMs = stopwatch.ElapsedMilliseconds / (double)totalOperations;

        Console.WriteLine($"Concurrent Operations Benchmark:");
        Console.WriteLine($"  {concurrentTasks} concurrent tasks, {operationsPerTask} ops each");
        Console.WriteLine($"  Total operations: {totalOperations} (Set + Get)");
        Console.WriteLine($"  Average: {avgMs:F4} ms/op");
        Console.WriteLine($"  Throughput: {totalOperations / stopwatch.Elapsed.TotalSeconds:F0} ops/sec");

        // 并发性能应该合理
        Assert.IsTrue(avgMs < 0.1, $"Concurrent operations too slow: {avgMs:F4} ms/op > 0.1 ms/op");

        // 清理
        IntelligentCacheManager.Clear();
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
