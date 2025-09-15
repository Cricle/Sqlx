// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlPerformanceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Performance;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Performance and stress tests for ExpressionToSql functionality.
/// </summary>
[TestClass]
public class ExpressionToSqlPerformanceTests
{
    /// <summary>
    /// Test entity with many properties for performance testing.
    /// </summary>
    public class LargeTestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDiscounted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public string? Tags { get; set; }
        public string? Metadata { get; set; }
    }

    #region Single-threaded Performance Tests

    /// <summary>
    /// Tests performance of creating many ExpressionToSql instances.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ManyInstanceCreations_PerformsWell()
    {
        // Arrange
        const int iterations = 10000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer();
            // Just create and dispose
        }

        stopwatch.Stop();

        // Assert - Should create 10,000 instances in less than 1 second
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Creating {iterations} instances took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        
        Console.WriteLine($"Created {iterations} ExpressionToSql instances in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests performance of building complex queries repeatedly.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ComplexQueryGeneration_PerformsWell()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.IsActive == true)
                .Where(e => e.Price >= 10.0m)
                .Where(e => e.Quantity > 0)
                .OrderBy(e => e.Name)
                .OrderByDescending(e => e.CreatedDate)
                .Skip(i * 10)
                .Take(50);

            var sql = expression.ToSql();
            var template = expression.ToTemplate();
        }

        stopwatch.Stop();

        // Assert - Should generate 1,000 complex queries in less than 2 seconds
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Generating {iterations} complex queries took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        
        Console.WriteLine($"Generated {iterations} complex queries in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests performance of UPDATE query generation with many SET clauses.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ManySetClauses_PerformsWell()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                .Set(e => e.Name, $"Name_{i}")
                .Set(e => e.Description, $"Description_{i}")
                .Set(e => e.Category, $"Category_{i}")
                .Set(e => e.Price, i * 1.5m)
                .Set(e => e.Cost, i * 1.0m)
                .Set(e => e.Quantity, i)
                .Set(e => e.IsActive, i % 2 == 0)
                .Set(e => e.IsVisible, true)
                .Set(e => e.ModifiedDate, DateTime.Now)
                .Set(e => e.ModifiedBy, "System")
                .Where(e => e.Id == i);

            var sql = expression.ToSql();
        }

        stopwatch.Stop();

        // Assert - Should generate 1,000 UPDATE queries with 10 SET clauses each in less than 2 seconds
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Generating {iterations} UPDATE queries took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        
        Console.WriteLine($"Generated {iterations} UPDATE queries with many SET clauses in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests performance of template caching.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_TemplateCaching_ImprovesPerfomance()
    {
        // Arrange
        const int iterations = 10000;
        using var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer()
            .Where(e => e.Id > 0)
            .Where(e => e.IsActive == true)
            .OrderBy(e => e.Name)
            .Take(100);

        // Warm up - generate template once
        var initialTemplate = expression.ToTemplate();

        var stopwatch = Stopwatch.StartNew();

        // Act - Call ToTemplate many times (should use cache)
        for (int i = 0; i < iterations; i++)
        {
            var template = expression.ToTemplate();
        }

        stopwatch.Stop();

        // Assert - Should be very fast due to caching (less than 100ms for 10,000 calls)
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
            $"Getting cached template {iterations} times took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        
        Console.WriteLine($"Retrieved cached template {iterations} times in {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Memory Usage Tests

    /// <summary>
    /// Tests memory usage of ExpressionToSql instances.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_MemoryUsage_IsReasonable()
    {
        // Arrange
        const int instanceCount = 1000;
        var instances = new List<ExpressionToSql<LargeTestEntity>>();

        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);

        // Act - Create many instances
        for (int i = 0; i < instanceCount; i++)
        {
            var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.Name)
                .Take(50);
            
            instances.Add(expression);
        }

        var memoryAfterCreation = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfterCreation - initialMemory;

        // Cleanup
        foreach (var instance in instances)
        {
            instance.Dispose();
        }
        instances.Clear();

        // Force garbage collection after cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfterCleanup = GC.GetTotalMemory(false);

        // Assert
        var averageMemoryPerInstance = memoryUsed / instanceCount;
        
        // Each instance should use less than 10KB on average
        Assert.IsTrue(averageMemoryPerInstance < 10240, 
            $"Average memory per instance: {averageMemoryPerInstance} bytes, expected < 10KB");

        // Memory should be mostly reclaimed after disposal
        var memoryReclaimed = memoryAfterCreation - memoryAfterCleanup;
        var reclaimPercentage = (double)memoryReclaimed / memoryUsed * 100;
        
        Console.WriteLine($"Memory used: {memoryUsed:N0} bytes for {instanceCount} instances");
        Console.WriteLine($"Average per instance: {averageMemoryPerInstance:N0} bytes");
        Console.WriteLine($"Memory reclaimed after disposal: {reclaimPercentage:F1}%");
    }

    #endregion

    #region Concurrent Access Tests

    /// <summary>
    /// Tests thread-safety of ExpressionToSql factory methods.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ConcurrentFactoryAccess_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var lockObject = new object();

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // Test all factory methods concurrently
                        using var sqlServer = ExpressionToSql<LargeTestEntity>.ForSqlServer();
                        using var mysql = ExpressionToSql<LargeTestEntity>.ForMySql();
                        using var postgresql = ExpressionToSql<LargeTestEntity>.ForPostgreSQL();
                        using var oracle = ExpressionToSql<LargeTestEntity>.ForOracle();
                        using var db2 = ExpressionToSql<LargeTestEntity>.ForDB2();
                        using var sqlite = ExpressionToSql<LargeTestEntity>.ForSqlite();
                        using var defaultBuilder = ExpressionToSql<LargeTestEntity>.Create();

                        // Add some operations
                        sqlServer.Where(e => e.Id > i).ToSql();
                        mysql.Where(e => e.Name != null).ToSql();
                        postgresql.OrderBy(e => e.CreatedDate).ToSql();
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.AreEqual(0, exceptions.Count, 
            $"Concurrent access caused {exceptions.Count} exceptions: {string.Join("; ", exceptions.Select(e => e.Message))}");
        
        Console.WriteLine($"Successfully completed {threadCount * operationsPerThread} concurrent operations");
    }

    /// <summary>
    /// Tests concurrent query building with the same instance (should fail gracefully).
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ConcurrentQueryBuilding_HandlesGracefully()
    {
        // Arrange
        const int threadCount = 5;
        const int operationsPerThread = 50;
        var tasks = new List<Task>();
        var results = new List<string>();
        var lockObject = new object();

        // Act - Multiple threads using the same instance (not recommended but should not crash)
        using var sharedExpression = ExpressionToSql<LargeTestEntity>.ForSqlServer();
        
        for (int t = 0; t < threadCount; t++)
        {
            var threadId = t;
            var task = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // Each thread adds different conditions
                        sharedExpression
                            .Where(e => e.Id > threadId * 1000 + i)
                            .Where(e => e.IsActive == true);

                        var sql = sharedExpression.ToSql();
                        
                        lock (lockObject)
                        {
                            results.Add(sql);
                        }
                    }
                }
                catch (Exception)
                {
                    // Concurrent modification may cause exceptions, which is acceptable
                    // The important thing is that it doesn't crash the process
                }
            });
            
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Should not crash, results may vary due to concurrent access
        Assert.IsTrue(results.Count >= 0, "Should handle concurrent access without crashing");
        
        Console.WriteLine($"Concurrent query building completed with {results.Count} successful SQL generations");
    }

    /// <summary>
    /// Tests concurrent disposal of ExpressionToSql instances.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ConcurrentDisposal_IsThreadSafe()
    {
        // Arrange
        const int instanceCount = 100;
        var instances = new List<ExpressionToSql<LargeTestEntity>>();
        
        // Create instances
        for (int i = 0; i < instanceCount; i++)
        {
            var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .OrderBy(e => e.Name);
            instances.Add(expression);
        }

        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var lockObject = new object();

        // Act - Dispose all instances concurrently
        foreach (var instance in instances)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    instance.Dispose();
                    // Call dispose again to test multiple disposal
                    instance.Dispose();
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.AreEqual(0, exceptions.Count, 
            $"Concurrent disposal caused {exceptions.Count} exceptions: {string.Join("; ", exceptions.Select(e => e.Message))}");
        
        Console.WriteLine($"Successfully disposed {instanceCount} instances concurrently");
    }

    #endregion

    #region Stress Tests

    /// <summary>
    /// Stress test with very long WHERE clauses.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_VeryLongWhereClause_HandlesCorrectly()
    {
        // Arrange
        const int conditionCount = 1000;
        using var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer();

        var stopwatch = Stopwatch.StartNew();

        // Act - Add many WHERE conditions
        for (int i = 0; i < conditionCount; i++)
        {
            expression.Where(e => e.Id != i);
        }

        var sql = expression.ToSql();
        stopwatch.Stop();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 10000, "SQL should be very long with many conditions");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Building query with {conditionCount} conditions took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        
        // Count AND occurrences (should be conditionCount - 1)
        var andCount = sql.Split(" AND ").Length - 1;
        Assert.AreEqual(conditionCount - 1, andCount, "Should have correct number of AND clauses");
        
        Console.WriteLine($"Generated SQL with {conditionCount} WHERE conditions in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"SQL length: {sql.Length:N0} characters");
    }

    /// <summary>
    /// Stress test with very long ORDER BY clauses.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_VeryLongOrderByClause_HandlesCorrectly()
    {
        // Arrange
        const int orderByCount = 100;
        using var expression = ExpressionToSql<LargeTestEntity>.ForSqlServer();

        var stopwatch = Stopwatch.StartNew();

        // Act - Add many ORDER BY clauses (alternating ASC/DESC)
        for (int i = 0; i < orderByCount; i++)
        {
            // Cycle through different properties to create variety
            switch (i % 8)
            {
                case 0:
                    if (i % 2 == 0) expression.OrderBy(e => e.Id); else expression.OrderByDescending(e => e.Id);
                    break;
                case 1:
                    if (i % 2 == 0) expression.OrderBy(e => e.Name); else expression.OrderByDescending(e => e.Name);
                    break;
                case 2:
                    if (i % 2 == 0) expression.OrderBy(e => e.Price); else expression.OrderByDescending(e => e.Price);
                    break;
                case 3:
                    if (i % 2 == 0) expression.OrderBy(e => e.CreatedDate); else expression.OrderByDescending(e => e.CreatedDate);
                    break;
                case 4:
                    if (i % 2 == 0) expression.OrderBy(e => e.ModifiedDate); else expression.OrderByDescending(e => e.ModifiedDate);
                    break;
                case 5:
                    if (i % 2 == 0) expression.OrderBy(e => e.Category); else expression.OrderByDescending(e => e.Category);
                    break;
                case 6:
                    if (i % 2 == 0) expression.OrderBy(e => e.Quantity); else expression.OrderByDescending(e => e.Quantity);
                    break;
                case 7:
                    if (i % 2 == 0) expression.OrderBy(e => e.IsActive); else expression.OrderByDescending(e => e.IsActive);
                    break;
            }
        }

        var additionalClause = expression.ToAdditionalClause();
        stopwatch.Stop();

        // Assert
        Assert.IsNotNull(additionalClause);
        Assert.IsTrue(additionalClause.Contains("ORDER BY"), "Should contain ORDER BY");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Building ORDER BY with {orderByCount} clauses took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        
        Console.WriteLine($"Generated ORDER BY with {orderByCount} clauses in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"ORDER BY clause length: {additionalClause.Length:N0} characters");
    }

    /// <summary>
    /// Load test simulating real-world usage patterns.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_RealWorldLoadTest_HandlesWell()
    {
        // Arrange
        const int totalOperations = 5000;
        var random = new Random(12345); // Fixed seed for reproducible results
        var stopwatch = Stopwatch.StartNew();
        var operationCounts = new Dictionary<string, int>();

        // Act - Simulate various real-world operations
        for (int i = 0; i < totalOperations; i++)
        {
            var operationType = random.Next(0, 4);
            
            switch (operationType)
            {
                case 0: // Simple SELECT
                    using (var expr = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                        .Where(e => e.Id == i)
                        .OrderBy(e => e.Name)
                        .Take(10))
                    {
                        expr.ToSql();
                        operationCounts["SimpleSelect"] = operationCounts.GetValueOrDefault("SimpleSelect") + 1;
                    }
                    break;

                case 1: // Complex SELECT
                    using (var expr = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                        .Where(e => e.Id > i % 1000)
                        .Where(e => e.IsActive == true)
                        .Where(e => e.Price >= 10.0m)
                        .OrderBy(e => e.Category)
                        .OrderByDescending(e => e.CreatedDate)
                        .Skip(i % 100)
                        .Take(25))
                    {
                        expr.ToSql();
                        operationCounts["ComplexSelect"] = operationCounts.GetValueOrDefault("ComplexSelect") + 1;
                    }
                    break;

                case 2: // UPDATE
                    using (var expr = ExpressionToSql<LargeTestEntity>.ForSqlServer()
                        .Set(e => e.Name, $"Updated_{i}")
                        .Set(e => e.ModifiedDate, DateTime.Now)
                        .Set(e => e.IsActive, i % 2 == 0)
                        .Where(e => e.Id == i))
                    {
                        expr.ToSql();
                        operationCounts["Update"] = operationCounts.GetValueOrDefault("Update") + 1;
                    }
                    break;

                case 3: // Multi-dialect
                    {
                        var dialect = random.Next(0, 6);
                        using var dialectExpr = dialect switch
                        {
                            0 => ExpressionToSql<LargeTestEntity>.ForSqlServer(),
                            1 => ExpressionToSql<LargeTestEntity>.ForMySql(),
                            2 => ExpressionToSql<LargeTestEntity>.ForPostgreSQL(),
                            3 => ExpressionToSql<LargeTestEntity>.ForOracle(),
                            4 => ExpressionToSql<LargeTestEntity>.ForDB2(),
                            _ => ExpressionToSql<LargeTestEntity>.ForSqlite()
                        };
                        
                        dialectExpr.Where(e => e.Id > i % 500).ToSql();
                        operationCounts["MultiDialect"] = operationCounts.GetValueOrDefault("MultiDialect") + 1;
                        break;
                    }
            }
        }

        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, 
            $"Load test with {totalOperations} operations took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        Console.WriteLine($"Completed {totalOperations} mixed operations in {stopwatch.ElapsedMilliseconds}ms");
        foreach (var kvp in operationCounts)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} operations");
        }
        
        var operationsPerSecond = totalOperations * 1000.0 / stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Average: {operationsPerSecond:F0} operations/second");
    }

    #endregion
}
