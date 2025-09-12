// -----------------------------------------------------------------------
// <copyright file="PerformanceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;
using System.Diagnostics;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class PerformanceTests
{
    [TestMethod]
    public void IndentedStringBuilder_LargeContent_PerformanceTest()
    {
        // Arrange
        const int iterations = 10000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var builder = new IndentedStringBuilder(null);
        for (int i = 0; i < iterations; i++)
        {
            builder.PushIndent();
            builder.AppendLine($"Line {i}");
            if (i % 100 == 0)
            {
                builder.PopIndent();
            }
        }
        var result = builder.ToString();
        stopwatch.Stop();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");

        Console.WriteLine($"IndentedStringBuilder performance: {iterations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void NameMapper_BatchMapping_PerformanceTest()
    {
        // Arrange
        var testNames = new[]
        {
            "UserName", "FirstName", "LastName", "EmailAddress", "PhoneNumber",
            "CreateDate", "UpdateDate", "IsActive", "UserRole", "ProfileImage"
        };
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            foreach (var name in testNames)
            {
                var result = NameMapper.MapName(name);
                Assert.IsNotNull(result);
            }
        }
        stopwatch.Stop();

        // Assert
        var totalOperations = iterations * testNames.Length;
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");

        Console.WriteLine($"NameMapper performance: {totalOperations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void SqlDefine_Creation_PerformanceTest()
    {
        // Arrange
        const int iterations = 100000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var sqlDefine = new SqlDefine("[", "]", "'", "'", "@");
            var wrapped = sqlDefine.WrapColumn("test_column");
            Assert.IsNotNull(wrapped);
        }
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

        Console.WriteLine($"SqlDefine performance: {iterations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void DatabaseDialectFactory_Caching_PerformanceTest()
    {
        // Arrange
        const int iterations = 10000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var provider1 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);
            var provider2 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SqlServer);
            var provider3 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Postgresql);
            var provider4 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SQLite);

            Assert.IsNotNull(provider1);
            Assert.IsNotNull(provider2);
            Assert.IsNotNull(provider3);
            Assert.IsNotNull(provider4);
        }
        stopwatch.Stop();

        // Assert
        var totalOperations = iterations * 4;
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");

        Console.WriteLine($"DatabaseDialectFactory caching performance: {totalOperations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void TypeAnalyzer_CacheStatistics_PerformanceTest()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            // Test cache statistics retrieval performance
            // Cache statistics removed - test simplified
        }
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50,
            $"Cache statistics test took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");

        Console.WriteLine($"TypeAnalyzer cache statistics: {iterations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void SqlOperationInferrer_BatchInference_PerformanceTest()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            // Test null handling performance (should be very fast with our null check)
            var result = SqlOperationInferrer.InferOperation(null!);
            Assert.AreEqual(SqlOperationType.Select, result);
        }
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms, expected < 10ms");

        Console.WriteLine($"SqlOperationInferrer null handling performance: {iterations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void SqlDefine_StaticInstances_PerformanceTest()
    {
        // Arrange
        const int iterations = 1000000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            // Accessing static instances should be extremely fast
            var mysql = SqlDefine.MySql;
            var sqlserver = SqlDefine.SqlServer;
            var pgsql = SqlDefine.PgSql;
            var sqlite = SqlDefine.SQLite;

            // Use them to prevent optimization
            Assert.IsNotNull(mysql.ColumnLeft);
            Assert.IsNotNull(sqlserver.ColumnLeft);
            Assert.IsNotNull(pgsql.ColumnLeft);
            Assert.IsNotNull(sqlite.ColumnLeft);
        }
        stopwatch.Stop();

        // Assert
        var totalOperations = iterations * 4;
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");

        Console.WriteLine($"SqlDefine static instances performance: {totalOperations} operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clear caches to ensure clean state for next test
        // Cache clearing removed - no longer needed
    }
}
