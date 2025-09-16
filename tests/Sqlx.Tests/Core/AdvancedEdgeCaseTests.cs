// -----------------------------------------------------------------------
// <copyright file="AdvancedEdgeCaseTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Advanced tests for edge cases, boundary conditions, and unusual scenarios.
/// </summary>
[TestClass]
public class AdvancedEdgeCaseTests
{
    /// <summary>
    /// Test entity with various property types for edge case testing.
    /// </summary>
    public class EdgeCaseTestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? NullableDecimal { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public bool? NullableBool { get; set; }
        public Guid UniqueId { get; set; }
        public byte ByteValue { get; set; }
        public short ShortValue { get; set; }
        public long LongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
    }

    /// <summary>
    /// Entity with minimal properties.
    /// </summary>
    public class MinimalEntity
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Entity with many properties to test large objects.
    /// </summary>
    public class LargeEntity
    {
        public int Id { get; set; }
        public string? Property01 { get; set; }
        public string? Property02 { get; set; }
        public string? Property03 { get; set; }
        public string? Property04 { get; set; }
        public string? Property05 { get; set; }
        public string? Property06 { get; set; }
        public string? Property07 { get; set; }
        public string? Property08 { get; set; }
        public string? Property09 { get; set; }
        public string? Property10 { get; set; }
        public decimal Value01 { get; set; }
        public decimal Value02 { get; set; }
        public decimal Value03 { get; set; }
        public decimal Value04 { get; set; }
        public decimal Value05 { get; set; }
        public DateTime Date01 { get; set; }
        public DateTime Date02 { get; set; }
        public DateTime Date03 { get; set; }
        public bool Flag01 { get; set; }
        public bool Flag02 { get; set; }
        public bool Flag03 { get; set; }
    }

    #region Minimal Entity Tests

    /// <summary>
    /// Tests ExpressionToSql with minimal entity having only one property.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_MinimalEntity_WorksCorrectly()
    {
        // Act
        using var expression = ExpressionToSql<MinimalEntity>.ForSqlServer()
            .Where(e => e.Id > 0)
            .OrderBy(e => e.Id)
            .Take(1);

        var sql = expression.ToSql();
        var template = expression.ToTemplate();

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null for minimal entity");
        Assert.IsTrue(sql.Contains("SELECT * FROM [MinimalEntity]"), "Should contain correct table name");
        Assert.IsTrue(sql.Contains("[Id] > 0"), "Should contain WHERE clause");
        Assert.IsTrue(sql.Contains("ORDER BY [Id] ASC"), "Should contain ORDER BY clause");
        Assert.IsTrue(sql.Contains("FETCH NEXT 1 ROWS ONLY"), "Should contain FETCH NEXT clause for SQL Server");
        
        Assert.IsNotNull(template.Sql, "Template SQL should not be null");
        Assert.IsNotNull(template.Parameters, "Template parameters should not be null");
    }

    /// <summary>
    /// Tests UPDATE operations on minimal entity.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_MinimalEntityUpdate_WorksCorrectly()
    {
        // Act
        using var expression = ExpressionToSql<MinimalEntity>.ForSqlServer()
            .Set(e => e.Id, 999)
            .Where(e => e.Id == 1);

        var sql = expression.ToSql();

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null for minimal entity update");
        Assert.IsTrue(sql.StartsWith("UPDATE [MinimalEntity] SET"), "Should start with UPDATE");
        Assert.IsTrue(sql.Contains("[Id] = 999"), "Should contain SET clause");
        Assert.IsTrue(sql.Contains("WHERE") && sql.Contains("[Id] = 1"), "Should contain WHERE clause");
    }

    #endregion

    #region Large Entity Tests

    /// <summary>
    /// Tests ExpressionToSql with entity having many properties.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_LargeEntity_HandlesEfficientlyShortTest()
    {
        // Arrange - Create a complex query with multiple conditions
        using var expression = ExpressionToSql<LargeEntity>.ForSqlServer()
            .Where(e => e.Id > 0)
            .Where(e => e.Property01 != null)
            .Where(e => e.Value01 >= 100)
            .Where(e => e.Flag01 == true)
            .OrderBy(e => e.Property01)
            .OrderByDescending(e => e.Date01)
            .Skip(10)
            .Take(20);

        var startTime = DateTime.Now;

        // Act
        var sql = expression.ToSql();
        var template = expression.ToTemplate();

        var duration = DateTime.Now - startTime;

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null for large entity");
        Assert.IsTrue(sql.Contains("SELECT * FROM [LargeEntity]"), "Should contain correct table name");
        Assert.IsTrue(sql.Contains("[Property01] IS NOT NULL"), "Should handle null checks");
        Assert.IsTrue(sql.Contains("[Value01] >= 100"), "Should handle decimal comparisons");
        Assert.IsTrue(sql.Contains("[Flag01] = 1"), "Should handle boolean comparisons");
        
        // Should complete quickly even with large entity
        Assert.IsTrue(duration.TotalMilliseconds < 100, 
            $"Large entity query took {duration.TotalMilliseconds}ms, should be < 100ms");
        
        Assert.IsNotNull(template.Sql, "Template SQL should not be null");
        Assert.IsNotNull(template.Parameters, "Template parameters should not be null");
    }

    /// <summary>
    /// Tests UPDATE with many SET clauses on large entity.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_LargeEntityManySetClauses_HandlesEfficiently()
    {
        // Arrange
        using var expression = ExpressionToSql<LargeEntity>.ForSqlServer()
            .Set(e => e.Property01, "Value1")
            .Set(e => e.Property02, "Value2")
            .Set(e => e.Property03, "Value3")
            .Set(e => e.Value01, 100.5m)
            .Set(e => e.Value02, 200.75m)
            .Set(e => e.Flag01, true)
            .Set(e => e.Flag02, false)
            .Set(e => e.Date01, DateTime.Now)
            .Where(e => e.Id == 1);

        var startTime = DateTime.Now;

        // Act
        var sql = expression.ToSql();

        var duration = DateTime.Now - startTime;

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null for large entity update");
        Assert.IsTrue(sql.StartsWith("UPDATE [LargeEntity] SET"), "Should start with UPDATE");
        Assert.IsTrue(sql.Contains("[Property01] = 'Value1'"), "Should contain string SET clause");
        Assert.IsTrue(sql.Contains("[Value01] = 100.5"), "Should contain decimal SET clause");
        Assert.IsTrue(sql.Contains("[Flag01] = 1"), "Should contain boolean SET clause");
        
        // Count SET clauses
        var setCount = sql.Split(" = ").Length - 1;
        Assert.IsTrue(setCount >= 8, "Should have at least 8 SET clauses");
        
        // Should complete quickly
        Assert.IsTrue(duration.TotalMilliseconds < 100, 
            $"Large entity update took {duration.TotalMilliseconds}ms, should be < 100ms");
    }

    #endregion

    #region Numeric Type Edge Cases

    /// <summary>
    /// Tests handling of various numeric types and their edge values.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_NumericTypeEdgeCases_HandledCorrectly()
    {
        // Test byte values
        using var byteExpression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.ByteValue == byte.MinValue)
            .Where(e => e.ByteValue != byte.MaxValue);
        var byteSql = byteExpression.ToSql();
        Assert.IsTrue(byteSql.Contains("= 0"), "Should handle byte.MinValue");
        Assert.IsTrue(byteSql.Contains("!= 255") || byteSql.Contains("<> 255"), "Should handle byte.MaxValue");

        // Test short values
        using var shortExpression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.ShortValue == short.MinValue)
            .Where(e => e.ShortValue != short.MaxValue);
        var shortSql = shortExpression.ToSql();
        Assert.IsTrue(shortSql.Contains("-32768"), "Should handle short.MinValue");
        Assert.IsTrue(shortSql.Contains("32767"), "Should handle short.MaxValue");

        // Test long values
        using var longExpression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.LongValue == long.MinValue)
            .Where(e => e.LongValue != long.MaxValue);
        var longSql = longExpression.ToSql();
        Assert.IsNotNull(longSql, "Should handle long.MinValue and long.MaxValue");

        // Test float values
        using var floatExpression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.FloatValue == float.MinValue)
            .Where(e => e.FloatValue != float.MaxValue)
            .Where(e => e.FloatValue != float.NaN)
            .Where(e => e.FloatValue != float.PositiveInfinity)
            .Where(e => e.FloatValue != float.NegativeInfinity);
        var floatSql = floatExpression.ToSql();
        Assert.IsNotNull(floatSql, "Should handle float edge values");

        // Test double values
        using var doubleExpression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.DoubleValue == double.MinValue)
            .Where(e => e.DoubleValue != double.MaxValue);
        var doubleSql = doubleExpression.ToSql();
        Assert.IsNotNull(doubleSql, "Should handle double edge values");
    }

    /// <summary>
    /// Tests handling of special floating point values.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_SpecialFloatingPointValues_HandledGracefully()
    {
        // Arrange
        var specialFloatValues = new[]
        {
            0.0f,
            -0.0f,
            1.0f,
            -1.0f,
            float.Epsilon,
            -float.Epsilon
        };

        var specialDoubleValues = new[]
        {
            0.0,
            -0.0,
            1.0,
            -1.0,
            double.Epsilon,
            -double.Epsilon
        };

        // Test float values
        foreach (var value in specialFloatValues)
        {
            using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
                .Where(e => e.FloatValue == value);
            var sql = expression.ToSql();
            Assert.IsNotNull(sql, $"Should handle float value: {value}");
        }

        // Test double values
        foreach (var value in specialDoubleValues)
        {
            using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
                .Where(e => e.DoubleValue == value);
            var sql = expression.ToSql();
            Assert.IsNotNull(sql, $"Should handle double value: {value}");
        }
    }

    #endregion

    #region GUID and Special Type Tests

    /// <summary>
    /// Tests handling of GUID values.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_GuidValues_HandledCorrectly()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        var newGuid = Guid.NewGuid();
        var specificGuid = new Guid("12345678-1234-5678-9012-123456789012");

        // Test empty GUID
        using var expression1 = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.UniqueId == emptyGuid);
        var sql1 = expression1.ToSql();
        Assert.IsTrue(sql1.Contains("00000000-0000-0000-0000-000000000000"), 
            "Should format empty GUID correctly");

        // Test specific GUID
        using var expression2 = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.UniqueId == specificGuid);
        var sql2 = expression2.ToSql();
        Assert.IsTrue(sql2.Contains("12345678-1234-5678-9012-123456789012"), 
            "Should format specific GUID correctly");

        // Test GUID in UPDATE
        using var expression3 = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Set(e => e.UniqueId, newGuid)
            .Where(e => e.Id == 1);
        var sql3 = expression3.ToSql();
        Assert.IsTrue(sql3.Contains(newGuid.ToString()), "Should handle GUID in SET clause");
    }

    #endregion

    #region Nullable Type Edge Cases

    /// <summary>
    /// Tests complex scenarios with nullable types.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_NullableTypeComplexScenarios_HandledCorrectly()
    {
        // Test nullable decimal with null coalescing
        using var expression1 = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.NullableDecimal == null)
            .Where(e => e.NullableDecimal != null)
            .Where(e => e.NullableDecimal > 0)
            .Where(e => e.NullableDecimal <= 100);
        var sql1 = expression1.ToSql();
        Assert.IsTrue(sql1.Contains("IS NULL"), "Should use IS NULL for nullable decimal");
        Assert.IsTrue(sql1.Contains("IS NOT NULL"), "Should use IS NOT NULL for nullable decimal");

        // Test nullable DateTime
        using var expression2 = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.NullableDateTime == null)
            .Where(e => e.NullableDateTime >= DateTime.Today);
        var sql2 = expression2.ToSql();
        Assert.IsTrue(sql2.Contains("IS NULL"), "Should use IS NULL for nullable DateTime");

        // Test nullable bool
        using var expression3 = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.NullableBool == null)
            .Where(e => e.NullableBool == true)
            .Where(e => e.NullableBool == false);
        var sql3 = expression3.ToSql();
        Assert.IsTrue(sql3.Contains("IS NULL"), "Should use IS NULL for nullable bool");
    }

    /// <summary>
    /// Tests SET operations with nullable types.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_NullableTypeSetOperations_HandledCorrectly()
    {
        // Act
        using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Set(e => e.NullableDecimal, (decimal?)null)
            .Set(e => e.NullableDateTime, (DateTime?)null)
            .Set(e => e.NullableBool, (bool?)null)
            .Set(e => e.NullableDecimal, (decimal?)99.99m)
            .Set(e => e.NullableDateTime, (DateTime?)DateTime.Now)
            .Set(e => e.NullableBool, (bool?)true)
            .Where(e => e.Id == 1);

        var sql = expression.ToSql();

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null for nullable SET operations");
        Assert.IsTrue(sql.Contains("= NULL"), "Should set nullable fields to NULL");
        Assert.IsTrue(sql.Contains("= 99.99"), "Should set nullable decimal to value");
        Assert.IsTrue(sql.Contains("= 1"), "Should set nullable bool to value");
    }

    #endregion

    #region Extreme Query Complexity Tests

    /// <summary>
    /// Tests extremely complex queries with many nested conditions.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ExtremelyComplexQuery_HandlesGracefully()
    {
        // Arrange
        using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer();

        // Add many complex conditions
        for (int i = 0; i < 10; i++)
        {
            expression.Where(e => 
                (e.Id > i && e.Name != null) || 
                (e.NullableDecimal >= i * 10 && e.NullableBool == true) ||
                (e.NullableDateTime >= DateTime.Now.AddDays(-i) && e.ByteValue < i * 5));
        }

        // Add multiple ORDER BY clauses
        expression
            .OrderBy(e => e.Name)
            .OrderByDescending(e => e.NullableDecimal)
            .OrderBy(e => e.NullableDateTime)
            .OrderByDescending(e => e.Id);

        expression.Skip(100).Take(50);

        var startTime = DateTime.Now;

        // Act
        var sql = expression.ToSql();
        var template = expression.ToTemplate();

        var duration = DateTime.Now - startTime;

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null for extremely complex query");
        Assert.IsTrue(sql.Length > 2000, "SQL should be very long for complex query");
        Assert.IsTrue(sql.Contains("AND"), "Should contain AND operators");
        Assert.IsTrue(sql.Contains("OR"), "Should contain OR operators");
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should contain ORDER BY");
        
        // Should still complete in reasonable time
        Assert.IsTrue(duration.TotalMilliseconds < 1000, 
            $"Extremely complex query took {duration.TotalMilliseconds}ms, should be < 1000ms");
        
        Assert.IsNotNull(template.Sql, "Template SQL should not be null");
        Assert.IsNotNull(template.Parameters, "Template parameters should not be null");
    }

    /// <summary>
    /// Tests query with maximum reasonable number of parameters.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_MaximumParameters_HandlesCorrectly()
    {
        // Arrange
        using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer();

        // Add many parameterized conditions
        for (int i = 0; i < 100; i++)
        {
            expression.Where(e => e.Id != i);
            expression.Where(e => e.Name != $"Name_{i}");
        }

        // Act
        var sql = expression.ToSql();
        var template = expression.ToTemplate();

        // Assert
        Assert.IsNotNull(sql, "SQL should not be null with many parameters");
        Assert.IsNotNull(template.Sql, "Template SQL should not be null");
        Assert.IsNotNull(template.Parameters, "Template parameters should not be null");
        
        // Should handle many conditions
        var conditionCount = sql.Split(" AND ").Length - 1;
        Assert.IsTrue(conditionCount >= 199, "Should have many AND conditions");
    }

    #endregion

    #region Error Recovery and Resilience Tests

    /// <summary>
    /// Tests behavior when mixing valid and edge case operations.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_MixedValidAndEdgeCaseOperations_HandlesGracefully()
    {
        // Act - Mix normal and edge case operations
        using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
            .Where(e => e.Id > 0) // Normal condition
            .Where(e => e.FloatValue == float.Epsilon) // Edge case
            .Where(e => e.Name != null) // Normal condition
            .Where(e => e.UniqueId == Guid.Empty) // Edge case
            .OrderBy(e => e.Name) // Normal ordering
            .OrderByDescending(e => e.NullableDateTime) // Edge case (nullable)
            .Take(10); // Normal limit

        var sql = expression.ToSql();

        // Assert
        Assert.IsNotNull(sql, "Should handle mixed operations gracefully");
        Assert.IsTrue(sql.Contains("WHERE"), "Should contain WHERE clause");
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should contain ORDER BY clause");
        Assert.IsTrue(sql.Contains("FETCH NEXT") || sql.Contains("LIMIT"), "Should contain pagination clause");
    }

    /// <summary>
    /// Tests behavior with rapid creation and disposal of many instances.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_RapidCreateAndDispose_HandlesCorrectly()
    {
        // Arrange
        const int rapidIterations = 1000;
        var startTime = DateTime.Now;

        // Act - Rapidly create and dispose instances
        for (int i = 0; i < rapidIterations; i++)
        {
            using var expression = ExpressionToSql<EdgeCaseTestEntity>.ForSqlServer()
                .Where(e => e.Id == i)
                .OrderBy(e => e.Name);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql, $"SQL should not be null for iteration {i}");
        }

        var duration = DateTime.Now - startTime;

        // Assert
        Assert.IsTrue(duration.TotalMilliseconds < 5000, 
            $"Rapid create/dispose took {duration.TotalMilliseconds}ms, should be < 5000ms");
        
        System.Console.WriteLine($"Completed {rapidIterations} rapid create/dispose cycles in {duration.TotalMilliseconds}ms");
    }

    #endregion
}
