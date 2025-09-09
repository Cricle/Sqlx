// -----------------------------------------------------------------------
// <copyright file="SqlxBenchmarks.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Core;

namespace Sqlx.PerformanceBenchmarks;

/// <summary>
/// Comprehensive performance benchmarks for Sqlx operations.
/// </summary>
public class SqlxBenchmarks
{
    private readonly SQLiteConnection _connection;
    private readonly List<BenchmarkResult> _results = new();

    public SqlxBenchmarks()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase();
    }

    /// <summary>
    /// Runs all performance benchmarks and returns results.
    /// </summary>
    public async Task<BenchmarkReport> RunAllBenchmarksAsync()
    {
        Console.WriteLine("🚀 Starting Sqlx Performance Benchmarks");
        Console.WriteLine("========================================");

        // Warm up
        await WarmupAsync();

        // Core operation benchmarks
        await BenchmarkBasicOperationsAsync();
        await BenchmarkBulkOperationsAsync();
        await BenchmarkConcurrentOperationsAsync();
        await BenchmarkMemoryEfficiencyAsync();
        await BenchmarkTypeAnalyzerCacheAsync();

        // Generate report
        var report = new BenchmarkReport(_results, new object()); // 临时修复
        
        Console.WriteLine("\n📊 Benchmark Results Summary:");
        Console.WriteLine($"Total benchmarks: {_results.Count}");
        Console.WriteLine($"Average execution time: {_results.Average(r => r.ExecutionTimeMs):F2}ms");
        Console.WriteLine($"Best performing: {_results.OrderBy(r => r.ExecutionTimeMs).First().Name}");
        Console.WriteLine($"Memory efficient: {_results.OrderBy(r => r.MemoryAllocated).First().Name}");

        return report;
    }

    private async Task WarmupAsync()
    {
        Console.WriteLine("🔥 Warming up JIT compiler...");
        
        for (int i = 0; i < 100; i++)
        {
            await ExecuteSimpleQueryAsync();
            TypeAnalyzer.IsLikelyEntityType(typeof(TestEntity));
            MemoryOptimizer.ConcatenateEfficiently("test", "warmup", i.ToString());
        }

        // Clear caches after warmup
        TypeAnalyzer.ClearCaches();
        PerformanceMonitor.Reset();
        
        Console.WriteLine("✅ Warmup completed");
    }

    private async Task BenchmarkBasicOperationsAsync()
    {
        Console.WriteLine("\n📈 Benchmarking Basic Operations...");

        // Single insert benchmark
        var insertResult = await BenchmarkOperationAsync("Single Insert", 1000, async () =>
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
            cmd.Parameters.AddWithValue("@name", $"User_{Guid.NewGuid():N}");
            cmd.Parameters.AddWithValue("@email", $"user_{Guid.NewGuid():N}@test.com");
            await cmd.ExecuteNonQueryAsync();
        });
        _results.Add(insertResult);

        // Single select benchmark
        var selectResult = await BenchmarkOperationAsync("Single Select", 1000, async () =>
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users LIMIT 1";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("Id");
                var name = reader.GetString("Name");
                var email = reader.GetString("Email");
            }
        });
        _results.Add(selectResult);

        // Parameter binding benchmark
        var paramResult = await BenchmarkOperationAsync("Parameter Binding", 5000, () =>
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Name = @name";
            
            var param = cmd.CreateParameter();
            param.ParameterName = "@name";
            param.Value = "TestUser";
            cmd.Parameters.Add(param);
            
            return Task.FromResult(cmd.ExecuteScalar());
        });
        _results.Add(paramResult);
    }

    private async Task BenchmarkBulkOperationsAsync()
    {
        Console.WriteLine("\n📊 Benchmarking Bulk Operations...");

        // Bulk insert benchmark
        var bulkInsertResult = await BenchmarkOperationAsync("Bulk Insert (1000 records)", 10, async () =>
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
                    cmd.Parameters.AddWithValue("@name", $"BulkUser_{i}");
                    cmd.Parameters.AddWithValue("@email", $"bulk_{i}@test.com");
                    await cmd.ExecuteNonQueryAsync();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        });
        _results.Add(bulkInsertResult);

        // Bulk select benchmark
        var bulkSelectResult = await BenchmarkOperationAsync("Bulk Select (1000 records)", 100, async () =>
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users LIMIT 1000";
            using var reader = await cmd.ExecuteReaderAsync();
            
            var users = new List<TestEntity>();
            while (await reader.ReadAsync())
            {
                users.Add(new TestEntity
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Email = reader.GetString("Email")
                });
            }
            return users.Count;
        });
        _results.Add(bulkSelectResult);
    }

    private async Task BenchmarkConcurrentOperationsAsync()
    {
        Console.WriteLine("\n🔄 Benchmarking Concurrent Operations...");

        // Concurrent reads benchmark
        var concurrentResult = await BenchmarkOperationAsync("Concurrent Reads (10 tasks)", 50, async () =>
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var connection = new SQLiteConnection("Data Source=:memory:");
                    connection.Open();
                    
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM Users";
                    await cmd.ExecuteScalarAsync();
                }));
            }
            
            await Task.WhenAll(tasks);
        });
        _results.Add(concurrentResult);
    }

    private async Task BenchmarkMemoryEfficiencyAsync()
    {
        Console.WriteLine("\n💾 Benchmarking Memory Efficiency...");

        // String concatenation benchmark
        var stringConcatResult = BenchmarkOperation("String Concatenation", 10000, () =>
        {
            var parts = new[] { "SELECT", "*", "FROM", "Users", "WHERE", "Id", "=", "@id" };
            return MemoryOptimizer.ConcatenateEfficiently(parts);
        });
        _results.Add(stringConcatResult);

        // StringBuilder benchmark
        var stringBuilderResult = BenchmarkOperation("StringBuilder Operations", 10000, () =>
        {
            return MemoryOptimizer.BuildString(sb =>
            {
                sb.Append("SELECT * FROM Users");
                sb.Append(" WHERE Id = @id");
                sb.Append(" AND Active = 1");
                sb.Append(" ORDER BY CreatedAt DESC");
            });
        });
        _results.Add(stringBuilderResult);

        // Memory allocation benchmark
        var memoryResult = BenchmarkOperation("Memory Allocation Test", 1000, () =>
        {
            var measurement = MemoryMonitor.MeasureAllocation(() =>
            {
                var list = new List<TestEntity>();
                for (int i = 0; i < 100; i++)
                {
                    list.Add(new TestEntity { Id = i, Name = $"User{i}", Email = $"user{i}@test.com" });
                }
            });
            return measurement.AllocatedBytes;
        });
        _results.Add(memoryResult);
    }

    private async Task BenchmarkTypeAnalyzerCacheAsync()
    {
        Console.WriteLine("\n🧠 Benchmarking Type Analyzer Cache...");

        // Cache miss benchmark (first time)
        TypeAnalyzer.ClearCaches();
        var cacheMissResult = BenchmarkOperation("Type Analysis (Cache Miss)", 1000, () =>
        {
            return TypeAnalyzer.IsLikelyEntityType(typeof(TestEntity));
        });
        _results.Add(cacheMissResult);

        // Cache hit benchmark (subsequent calls)
        var cacheHitResult = BenchmarkOperation("Type Analysis (Cache Hit)", 10000, () =>
        {
            return TypeAnalyzer.IsLikelyEntityType(typeof(TestEntity));
        });
        _results.Add(cacheHitResult);

        // Entity type extraction benchmark
        var extractionResult = BenchmarkOperation("Entity Type Extraction", 5000, () =>
        {
            return TypeAnalyzer.ExtractEntityType(typeof(List<TestEntity>));
        });
        _results.Add(extractionResult);
    }

    private async Task<BenchmarkResult> BenchmarkOperationAsync<T>(string name, int iterations, Func<Task<T>> operation)
    {
        // Warmup
        await operation();
        await operation();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await operation();
        }

        stopwatch.Stop();
        var afterMemory = GC.GetTotalMemory(false);

        var result = new BenchmarkResult(
            name,
            iterations,
            stopwatch.ElapsedMilliseconds,
            (double)stopwatch.ElapsedMilliseconds / iterations,
            afterMemory - beforeMemory
        );

        Console.WriteLine($"  ✅ {name}: {result.ExecutionTimeMs:F2}ms avg, {result.MemoryAllocated} bytes allocated");
        return result;
    }

    private BenchmarkResult BenchmarkOperation<T>(string name, int iterations, Func<T> operation)
    {
        // Warmup
        operation();
        operation();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            operation();
        }

        stopwatch.Stop();
        var afterMemory = GC.GetTotalMemory(false);

        var result = new BenchmarkResult(
            name,
            iterations,
            stopwatch.ElapsedMilliseconds,
            (double)stopwatch.ElapsedMilliseconds / iterations,
            afterMemory - beforeMemory
        );

        Console.WriteLine($"  ✅ {name}: {result.ExecutionTimeMs:F2}ms avg, {result.MemoryAllocated} bytes allocated");
        return result;
    }

    private async Task ExecuteSimpleQueryAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        await cmd.ExecuteScalarAsync();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
        cmd.ExecuteNonQuery();

        // Insert some test data
        for (int i = 0; i < 100; i++)
        {
            cmd.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@name", $"TestUser{i}");
            cmd.Parameters.AddWithValue("@email", $"test{i}@example.com");
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

/// <summary>
/// Test entity for benchmarking.
/// </summary>
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Benchmark result for a single operation.
/// </summary>
public record BenchmarkResult(
    string Name,
    int Iterations,
    long TotalTimeMs,
    double ExecutionTimeMs,
    long MemoryAllocated
);

/// <summary>
/// Comprehensive benchmark report.
/// </summary>
public record BenchmarkReport(
    IReadOnlyList<BenchmarkResult> Results,
    object MemoryStats // 临时使用 object 类型
)
{
    public BenchmarkResult FastestOperation => Results.OrderBy(r => r.ExecutionTimeMs).First();
    public BenchmarkResult SlowestOperation => Results.OrderByDescending(r => r.ExecutionTimeMs).First();
    public BenchmarkResult MostMemoryEfficient => Results.OrderBy(r => r.MemoryAllocated).First();
    public double AverageExecutionTime => Results.Average(r => r.ExecutionTimeMs);
    public long TotalMemoryAllocated => Results.Sum(r => r.MemoryAllocated);
}
