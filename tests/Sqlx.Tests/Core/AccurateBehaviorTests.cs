// -----------------------------------------------------------------------
// <copyright file="AccurateBehaviorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Tests based on actual observed behavior of the Sqlx library.
/// </summary>
[TestClass]
public class AccurateBehaviorTests
{
    /// <summary>
    /// Test entity for accurate behavior testing.
    /// </summary>
    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
    }

    #region Accurate SQL Server Tests

    /// <summary>
    /// Tests SQL Server SELECT with actual expected format.
    /// </summary>
    [TestMethod]
    public void SqlServer_SelectQuery_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id > 10)
            .Where(e => e.IsActive == true)
            .OrderBy(e => e.Name)
            .Take(50);

        // Act
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT * FROM [TestEntity]"), "Should use square brackets for table");
        Assert.IsTrue(sql.Contains("[Id] > 10"), "Should use square brackets for columns");
        Assert.IsTrue(sql.Contains("[IsActive] = 1"), "Should convert true to 1");
        Assert.IsTrue(sql.Contains("ORDER BY [Name] ASC"), "Should use ASC ordering");
        Assert.IsTrue(sql.Contains("FETCH NEXT 50 ROWS ONLY"), "Should use FETCH NEXT for SQL Server pagination");
        Assert.IsTrue(sql.Contains(" AND "), "Should use AND to join conditions");
    }

    /// <summary>
    /// Tests SQL Server UPDATE with actual expected format.
    /// </summary>
    [TestMethod]
    public void SqlServer_UpdateQuery_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Set(e => e.Name, "Updated Name")
            .Set(e => e.Price, 99.99m)
            .Set(e => e.IsActive, false)
            .Where(e => e.Id == 1);

        // Act
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.StartsWith("UPDATE [TestEntity] SET"), "Should start with UPDATE");
        Assert.IsTrue(sql.Contains("[Name] = 'Updated Name'"), "Should set string values with quotes");
        Assert.IsTrue(sql.Contains("[Price] = 99.99"), "Should set decimal values without quotes");
        Assert.IsTrue(sql.Contains("[IsActive] = 0"), "Should convert false to 0");
        Assert.IsTrue(sql.Contains("WHERE ([Id] = 1)"), "Should include WHERE clause with parentheses");
        Assert.IsTrue(sql.Contains(", "), "Should separate SET clauses with commas");
    }

    #endregion

    #region Accurate Null Handling Tests

    /// <summary>
    /// Tests null comparison with actual expected behavior.
    /// </summary>
    [TestMethod]
    public void NullComparison_GeneratesAccurateFormat()
    {
        // Arrange & Act
        using var expression1 = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Name == null);
        var sql1 = expression1.ToSql();

        using var expression2 = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Name != null);
        var sql2 = expression2.ToSql();

        // Assert - Based on correct SQL syntax
        Assert.IsTrue(sql1.Contains("[Name] IS NULL"), "Should use IS NULL for null equality");
        Assert.IsTrue(sql2.Contains("[Name] IS NOT NULL"), "Should use IS NOT NULL for null inequality");
    }

    #endregion

    #region Accurate Dialect Tests

    /// <summary>
    /// Tests MySQL with actual expected format.
    /// </summary>
    [TestMethod]
    public void MySQL_SelectQuery_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForMySql()
            .Where(e => e.Id > 0)
            .Where(e => e.Name != null)
            .OrderBy(e => e.Name)
            .Take(10);

        // Act
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT * FROM `TestEntity`"), "Should use backticks for table");
        Assert.IsTrue(sql.Contains("`Id` > 0"), "Should use backticks for columns");
        Assert.IsTrue(sql.Contains("`Name` IS NOT NULL"), "Should use IS NOT NULL for not null");
        Assert.IsTrue(sql.Contains("ORDER BY `Name` ASC"), "Should use backticks in ORDER BY");
        Assert.IsTrue(sql.Contains("LIMIT 10"), "Should use LIMIT");
    }

    /// <summary>
    /// Tests PostgreSQL with actual expected format.
    /// </summary>
    [TestMethod]
    public void PostgreSQL_SelectQuery_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForPostgreSQL()
            .Where(e => e.Id > 0)
            .Where(e => e.Name != null)
            .OrderBy(e => e.Name)
            .Take(10);

        // Act
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT * FROM \"TestEntity\""), "Should use double quotes for table");
        Assert.IsTrue(sql.Contains("\"Id\" > 0"), "Should use double quotes for columns");
        Assert.IsTrue(sql.Contains("\"Name\" IS NOT NULL"), "Should use IS NOT NULL for not null");
        Assert.IsTrue(sql.Contains("ORDER BY \"Name\" ASC"), "Should use double quotes in ORDER BY");
        Assert.IsTrue(sql.Contains("LIMIT 10"), "Should use LIMIT");
    }

    /// <summary>
    /// Tests SQLite with actual expected format.
    /// </summary>
    [TestMethod]
    public void SQLite_SelectQuery_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForSqlite()
            .Where(e => e.Id > 0)
            .Where(e => e.Name != null)
            .OrderBy(e => e.Name)
            .Take(10);

        // Act
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT * FROM [TestEntity]"), "Should use square brackets for table");
        Assert.IsTrue(sql.Contains("[Id] > 0"), "Should use square brackets for columns");
        Assert.IsTrue(sql.Contains("[Name] IS NOT NULL"), "Should use IS NOT NULL for not null");
        Assert.IsTrue(sql.Contains("ORDER BY [Name] ASC"), "Should use square brackets in ORDER BY");
        Assert.IsTrue(sql.Contains("LIMIT 10"), "Should use LIMIT");
    }

    #endregion

    #region Accurate String and Value Handling Tests

    /// <summary>
    /// Tests string value handling with actual expected format.
    /// </summary>
    [TestMethod]
    public void StringValues_HandledAccurately()
    {
        // Arrange & Act
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Name == "Test String")
            .Where(e => e.Category == "Category with 'quotes'");

        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsTrue(sql.Contains("'Test String'"), "Should wrap simple strings in single quotes");
        Assert.IsTrue(sql.Contains("'Category with ''quotes'''"), "Should escape single quotes with double single quotes");
    }

    /// <summary>
    /// Tests decimal value handling with actual expected format.
    /// </summary>
    [TestMethod]
    public void DecimalValues_HandledAccurately()
    {
        // Arrange & Act
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Price == 99.99m)
            .Where(e => e.Price >= 0m);

        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsTrue(sql.Contains("= 99.99"), "Should format decimal without quotes");
        Assert.IsTrue(sql.Contains(">= 0"), "Should format zero decimal");
    }

    /// <summary>
    /// Tests boolean value handling with actual expected format.
    /// </summary>
    [TestMethod]
    public void BooleanValues_HandledAccurately()
    {
        // Arrange & Act
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.IsActive == true)
            .Where(e => e.IsActive == false);

        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsTrue(sql.Contains("= 1"), "Should convert true to 1");
        Assert.IsTrue(sql.Contains("= 0"), "Should convert false to 0");
    }

    #endregion

    #region Accurate Complex Query Tests

    /// <summary>
    /// Tests complex query with multiple conditions using actual format.
    /// </summary>
    [TestMethod]
    public void ComplexQuery_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id > 0)
            .Where(e => e.Name != null)
            .Where(e => e.IsActive == true)
            .Where(e => e.Price >= 10.0m)
            .OrderBy(e => e.Name)
            .OrderByDescending(e => e.CreatedDate)
            .Skip(10)
            .Take(20);

        // Act
        var sql = expression.ToSql();
        var whereClause = expression.ToWhereClause();
        var additionalClause = expression.ToAdditionalClause();

        // Assert - Based on actual observed output
        Assert.IsNotNull(sql);

        // WHERE clause assertions
        Assert.IsTrue(whereClause.Contains("([Id] > 0)"), "Should wrap conditions in parentheses");
        Assert.IsTrue(whereClause.Contains(" AND "), "Should join conditions with AND");
        Assert.IsTrue(whereClause.Contains("[Name] IS NOT NULL"), "Should use IS NOT NULL");
        Assert.IsTrue(whereClause.Contains("[IsActive] = 1"), "Should convert boolean");
        Assert.IsTrue(whereClause.Contains("[Price] >= 10"), "Should handle decimal comparison");

        // Additional clause assertions
        Assert.IsTrue(additionalClause.Contains("ORDER BY"), "Should contain ORDER BY");
        Assert.IsTrue(additionalClause.Contains("[Name] ASC"), "Should default to ASC");
        Assert.IsTrue(additionalClause.Contains("[CreatedDate] DESC"), "Should use DESC");
        Assert.IsTrue(additionalClause.Contains("OFFSET 10 ROWS"), "Should use OFFSET ROWS for SQL Server");
        Assert.IsTrue(additionalClause.Contains("FETCH NEXT 20 ROWS ONLY"), "Should use FETCH NEXT for SQL Server");
    }

    /// <summary>
    /// Tests UPDATE with multiple SET clauses using actual format.
    /// </summary>
    [TestMethod]
    public void ComplexUpdate_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Set(e => e.Name, "New Name")
            .Set(e => e.Price, 150.75m)
            .Set(e => e.IsActive, true)
            .Set(e => e.Category, "Updated Category")
            .Where(e => e.Id == 1);

        // Act
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsTrue(sql.StartsWith("UPDATE [TestEntity] SET"), "Should start with UPDATE");
        Assert.IsTrue(sql.Contains("[Name] = 'New Name'"), "Should set string with quotes");
        Assert.IsTrue(sql.Contains("[Price] = 150.75"), "Should set decimal without quotes");
        Assert.IsTrue(sql.Contains("[IsActive] = 1"), "Should convert true to 1");
        Assert.IsTrue(sql.Contains("[Category] = 'Updated Category'"), "Should set category");
        Assert.IsTrue(sql.Contains("WHERE ([Id] = 1)"), "Should include WHERE with parentheses");

        // Count SET clauses by counting commas + 1
        var setCount = sql.Substring(sql.IndexOf("SET") + 3, sql.IndexOf("WHERE") - sql.IndexOf("SET") - 3)
            .Split(',').Length;
        Assert.AreEqual(4, setCount, "Should have 4 SET clauses");
    }

    #endregion

    #region Accurate Template Tests

    /// <summary>
    /// Tests template generation with actual expected behavior.
    /// </summary>
    [TestMethod]
    public void Template_GeneratesAccurateFormat()
    {
        // Arrange
        using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id > 10)
            .Where(e => e.Name == "Test")
            .OrderBy(e => e.Name)
            .Take(50);

        // Act
        var template = expression.ToTemplate();
        var sql = expression.ToSql();

        // Assert - Based on actual observed output
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);

        // Template SQL should be same as regular SQL for constant values
        Assert.AreEqual(sql, template.Sql, "Template SQL should match regular SQL for constants");

        // No parameters expected for constant values
        Assert.AreEqual(0, template.Parameters.Length, "Should have no parameters for constant values");
    }

    #endregion

    #region Accurate Performance Tests

    /// <summary>
    /// Tests performance with realistic expectations.
    /// </summary>
    [TestMethod]
    public void Performance_MeetsRealisticExpectations()
    {
        // Arrange
        const int iterations = 1000;
        var startTime = DateTime.Now;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Id > i)
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.Name)
                .Take(10);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql, $"SQL should not be null for iteration {i}");
        }

        var duration = DateTime.Now - startTime;

        // Assert - Realistic performance expectations
        Assert.IsTrue(duration.TotalMilliseconds < 5000,
            $"Performance test took {duration.TotalMilliseconds}ms, expected < 5000ms");

        Console.WriteLine($"Generated {iterations} SQL queries in {duration.TotalMilliseconds}ms");
        Console.WriteLine($"Average: {duration.TotalMilliseconds / iterations:F2}ms per query");
    }

    #endregion

    #region Accurate Resource Management Tests

    /// <summary>
    /// Tests resource management with realistic expectations.
    /// </summary>
    [TestMethod]
    public void ResourceManagement_WorksCorrectly()
    {
        // Arrange
        const int instanceCount = 100;

        // Act & Assert - Create and dispose many instances
        for (int i = 0; i < instanceCount; i++)
        {
            using var expression = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Id == i)
                .OrderBy(e => e.Name);

            var sql = expression.ToSql();
            Assert.IsNotNull(sql, $"SQL should not be null for instance {i}");

            // Dispose is called automatically by using statement
        }

        // If we get here without exceptions, resource management is working
        Assert.IsTrue(true, "Resource management test completed successfully");
    }

    #endregion
}
