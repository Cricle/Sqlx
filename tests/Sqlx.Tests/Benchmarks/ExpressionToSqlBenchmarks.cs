// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBenchmarks.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Benchmarks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Benchmark tests for ExpressionToSql performance measurement and regression detection.
/// </summary>
[TestClass]
public class ExpressionToSqlBenchmarks
{
    /// <summary>
    /// Benchmark entity with various property types.
    /// </summary>
    public class BenchmarkEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int? OptionalId { get; set; }
        public decimal? OptionalPrice { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    #region Factory Method Benchmarks

    /// <summary>
    /// Benchmarks factory method creation performance.
    /// </summary>
    [TestMethod]
    public void Benchmark_FactoryMethodCreation()
    {
        // Arrange
        const int iterations = 10000;
        var results = new Dictionary<string, long>();

        var factories = new Dictionary<string, Func<ExpressionToSql<BenchmarkEntity>>>
        {
            ["SqlServer"] = () => ExpressionToSql<BenchmarkEntity>.ForSqlServer(),
            ["MySQL"] = () => ExpressionToSql<BenchmarkEntity>.ForMySql(),
            ["PostgreSQL"] = () => ExpressionToSql<BenchmarkEntity>.ForPostgreSQL(),
            ["Oracle"] = () => ExpressionToSql<BenchmarkEntity>.ForOracle(),
            ["DB2"] = () => ExpressionToSql<BenchmarkEntity>.ForDB2(),
            ["SQLite"] = () => ExpressionToSql<BenchmarkEntity>.ForSqlite()
        };

        // Act & Measure
        foreach (var kvp in factories)
        {
            var dialectName = kvp.Key;
            var factory = kvp.Value;

            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                using var expression = factory();
                // Just create and dispose
            }
            
            stopwatch.Stop();
            results[dialectName] = stopwatch.ElapsedMilliseconds;
        }

        // Assert & Report
        foreach (var kvp in results)
        {
            Assert.IsTrue(kvp.Value < 2000, $"{kvp.Key} factory creation took {kvp.Value}ms for {iterations} iterations, expected < 2000ms");
            Console.WriteLine($"{kvp.Key}: {kvp.Value}ms ({kvp.Value * 1000.0 / iterations:F2}μs per creation)");
        }

        var avgTime = results.Values.Average();
        Console.WriteLine($"Average factory creation time: {avgTime:F1}ms for {iterations} iterations");
    }

    #endregion

    #region Simple Query Benchmarks

    /// <summary>
    /// Benchmarks simple SELECT query generation.
    /// </summary>
    [TestMethod]
    public void Benchmark_SimpleSelectQuery()
    {
        // Arrange
        const int iterations = 5000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.Name)
                .Take(10);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql);
        }

        stopwatch.Stop();

        // Assert & Report
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000, 
            $"Simple SELECT generation took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations, expected < 3000ms");
        
        Console.WriteLine($"Simple SELECT: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F2}μs per query");
    }

    /// <summary>
    /// Benchmarks simple UPDATE query generation.
    /// </summary>
    [TestMethod]
    public void Benchmark_SimpleUpdateQuery()
    {
        // Arrange
        const int iterations = 5000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Set(e => e.Name, $"Name_{i}")
                .Set(e => e.IsActive, i % 2 == 0)
                .Where(e => e.Id == i);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql);
        }

        stopwatch.Stop();

        // Assert & Report
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000, 
            $"Simple UPDATE generation took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations, expected < 3000ms");
        
        Console.WriteLine($"Simple UPDATE: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F2}μs per query");
    }

    #endregion

    #region Complex Query Benchmarks

    /// <summary>
    /// Benchmarks complex query with many conditions.
    /// </summary>
    [TestMethod]
    public void Benchmark_ComplexQueryGeneration()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.Name != null)
                .Where(e => e.IsActive == true)
                .Where(e => e.Price >= 10.0m)
                .Where(e => e.Category != "Excluded")
                .Where(e => e.CreatedDate >= DateTime.Today.AddDays(-30))
                .Where(e => e.OptionalId != null)
                .OrderBy(e => e.Name)
                .OrderByDescending(e => e.CreatedDate)
                .OrderBy(e => e.Price)
                .Skip(i * 10)
                .Take(25);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql);
        }

        stopwatch.Stop();

        // Assert & Report
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Complex query generation took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations, expected < 5000ms");
        
        Console.WriteLine($"Complex Query: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F2}μs per query");
    }

    /// <summary>
    /// Benchmarks UPDATE with many SET clauses.
    /// </summary>
    [TestMethod]
    public void Benchmark_ComplexUpdateGeneration()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Set(e => e.Name, $"Updated_{i}")
                .Set(e => e.Price, i * 1.5m)
                .Set(e => e.IsActive, i % 2 == 0)
                .Set(e => e.Category, $"Category_{i % 5}")
                .Set(e => e.Description, $"Description for item {i}")
                .Set(e => e.ModifiedDate, DateTime.Now)
                .Set(e => e.OptionalPrice, i > 500 ? (decimal?)i * 2.0m : null)
                .Where(e => e.Id == i)
                .Where(e => e.IsActive == true);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql);
        }

        stopwatch.Stop();

        // Assert & Report
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Complex UPDATE generation took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations, expected < 5000ms");
        
        Console.WriteLine($"Complex UPDATE: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F2}μs per query");
    }

    #endregion

    #region Template Generation Benchmarks

    /// <summary>
    /// Benchmarks template generation performance.
    /// </summary>
    [TestMethod]
    public void Benchmark_TemplateGeneration()
    {
        // Arrange
        const int iterations = 3000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.Name == $"Test_{i}")
                .OrderBy(e => e.CreatedDate)
                .Take(20);

            var template = expression.ToTemplate();
            Assert.IsNotNull(template.Sql);
            Assert.IsNotNull(template.Parameters);
        }

        stopwatch.Stop();

        // Assert & Report
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 4000, 
            $"Template generation took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations, expected < 4000ms");
        
        Console.WriteLine($"Template Generation: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F2}μs per template");
    }

    /// <summary>
    /// Benchmarks template caching performance.
    /// </summary>
    [TestMethod]
    public void Benchmark_TemplateCaching()
    {
        // Arrange
        const int iterations = 10000;
        using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
            .Where(e => e.Id > 0)
            .Where(e => e.IsActive == true)
            .OrderBy(e => e.Name)
            .Take(10);

        // Generate template once to initialize any caching
        var initialTemplate = expression.ToTemplate();
        Assert.IsNotNull(initialTemplate.Sql);

        var stopwatch = Stopwatch.StartNew();

        // Act - Call ToTemplate many times (should use cache if implemented)
        for (int i = 0; i < iterations; i++)
        {
            var template = expression.ToTemplate();
            Assert.IsNotNull(template.Sql);
        }

        stopwatch.Stop();

        // Assert & Report
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, 
            $"Template caching took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations, expected < 500ms");
        
        Console.WriteLine($"Template Caching: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F2}μs per cached template");
    }

    #endregion

    #region Cross-Dialect Performance Comparison

    /// <summary>
    /// Benchmarks performance across all database dialects.
    /// </summary>
    [TestMethod]
    public void Benchmark_CrossDialectPerformance()
    {
        // Arrange
        const int iterations = 2000;
        var results = new Dictionary<string, long>();

        var dialects = new Dictionary<string, Func<ExpressionToSql<BenchmarkEntity>>>
        {
            ["SqlServer"] = () => ExpressionToSql<BenchmarkEntity>.ForSqlServer(),
            ["MySQL"] = () => ExpressionToSql<BenchmarkEntity>.ForMySql(),
            ["PostgreSQL"] = () => ExpressionToSql<BenchmarkEntity>.ForPostgreSQL(),
            ["Oracle"] = () => ExpressionToSql<BenchmarkEntity>.ForOracle(),
            ["DB2"] = () => ExpressionToSql<BenchmarkEntity>.ForDB2(),
            ["SQLite"] = () => ExpressionToSql<BenchmarkEntity>.ForSqlite()
        };

        // Act & Measure
        foreach (var kvp in dialects)
        {
            var dialectName = kvp.Key;
            var factory = kvp.Value;

            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                using var expression = factory()
                    .Where(e => e.Id > i)
                    .Where(e => e.IsActive == true)
                    .Where(e => e.Price >= 10.0m)
                    .OrderBy(e => e.Name)
                    .OrderByDescending(e => e.CreatedDate)
                    .Skip(i % 100)
                    .Take(25);

                var sql = expression.ToSql();
                Assert.IsNotNull(sql);
            }
            
            stopwatch.Stop();
            results[dialectName] = stopwatch.ElapsedMilliseconds;
        }

        // Assert & Report
        var maxTime = results.Values.Max();
        var minTime = results.Values.Min();
        var avgTime = results.Values.Average();

        foreach (var kvp in results)
        {
            Assert.IsTrue(kvp.Value < 6000, $"{kvp.Key} took {kvp.Value}ms for {iterations} iterations, expected < 6000ms");
            Console.WriteLine($"{kvp.Key}: {kvp.Value}ms ({kvp.Value * 1000.0 / iterations:F2}μs per query)");
        }

        Console.WriteLine($"Performance Summary: Min={minTime}ms, Max={maxTime}ms, Avg={avgTime:F1}ms");
        
        // Performance should be reasonably similar across dialects (within 4x)
        Assert.IsTrue(maxTime <= minTime * 4, 
            $"Performance difference too large: max={maxTime}ms, min={minTime}ms");
    }

    #endregion

    #region Memory Usage Benchmarks

    /// <summary>
    /// Benchmarks memory usage during query generation.
    /// </summary>
    [TestMethod]
    public void Benchmark_MemoryUsage()
    {
        // Arrange
        const int iterations = 1000;

        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.Name != null)
                .Where(e => e.IsActive == true)
                .Set(e => e.Name, $"Updated_{i}")
                .Set(e => e.Price, i * 1.5m)
                .OrderBy(e => e.CreatedDate)
                .Take(10);

            var sql = expression.ToSql();
            var template = expression.ToTemplate();
            
            Assert.IsNotNull(sql);
            Assert.IsNotNull(template.Sql);
        }

        stopwatch.Stop();

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - initialMemory;

        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfterCleanup = GC.GetTotalMemory(false);

        // Assert & Report
        var averageMemoryPerOperation = memoryUsed / iterations;
        
        Assert.IsTrue(averageMemoryPerOperation < 50000, // 50KB per operation
            $"Average memory per operation: {averageMemoryPerOperation:N0} bytes, expected < 50KB");

        Console.WriteLine($"Memory Usage Benchmark:");
        Console.WriteLine($"  Operations: {iterations}");
        Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Memory used: {memoryUsed:N0} bytes");
        Console.WriteLine($"  Average per operation: {averageMemoryPerOperation:N0} bytes");
        Console.WriteLine($"  Memory after cleanup: {memoryAfterCleanup - initialMemory:N0} bytes");
    }

    #endregion

    #region Regression Detection Tests

    /// <summary>
    /// Baseline performance test for regression detection.
    /// </summary>
    [TestMethod]
    public void Benchmark_RegressionBaseline()
    {
        // Arrange
        const int warmupIterations = 100;
        const int benchmarkIterations = 2000;

        // Warmup
        for (int i = 0; i < warmupIterations; i++)
        {
            using var warmupExpression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .OrderBy(e => e.Name)
                .Take(10);
            var warmupSql = warmupExpression.ToSql();
        }

        // Benchmark
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < benchmarkIterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.Name)
                .Take(10);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql);
        }

        stopwatch.Stop();

        // Assert & Report - Baseline expectations
        var totalTime = stopwatch.ElapsedMilliseconds;
        var averageTime = totalTime / (double)benchmarkIterations;

        Console.WriteLine($"Regression Baseline Results:");
        Console.WriteLine($"  Total time: {totalTime}ms");
        Console.WriteLine($"  Average per query: {averageTime:F3}ms");
        Console.WriteLine($"  Queries per second: {benchmarkIterations * 1000.0 / totalTime:F0}");

        // Baseline expectations - adjust these based on acceptable performance
        Assert.IsTrue(totalTime < 4000, $"Baseline took {totalTime}ms, expected < 4000ms");
        Assert.IsTrue(averageTime < 2.0, $"Average per query {averageTime:F3}ms, expected < 2.0ms");
    }

    #endregion

    #region Stress Tests

    /// <summary>
    /// Stress test with high iteration count.
    /// </summary>
    [TestMethod]
    public void Benchmark_StressTest()
    {
        // Arrange
        const int iterations = 20000;
        var startTime = DateTime.Now;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<BenchmarkEntity>.ForSqlServer()
                .Where(e => e.Id == i % 1000)
                .Where(e => e.IsActive == (i % 2 == 0))
                .OrderBy(e => e.Name)
                .Take(10);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql);
            
            // Occasional template generation
            if (i % 100 == 0)
            {
                var template = expression.ToTemplate();
                Assert.IsNotNull(template.Sql);
            }
        }

        var duration = DateTime.Now - startTime;

        // Assert & Report
        Assert.IsTrue(duration.TotalMilliseconds < 15000, 
            $"Stress test took {duration.TotalMilliseconds}ms for {iterations} iterations, expected < 15000ms");

        Console.WriteLine($"Stress Test Results:");
        Console.WriteLine($"  Iterations: {iterations:N0}");
        Console.WriteLine($"  Total time: {duration.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Average: {duration.TotalMilliseconds / iterations:F3}ms per query");
        Console.WriteLine($"  Throughput: {iterations * 1000.0 / duration.TotalMilliseconds:F0} queries/second");
    }

    #endregion
}
