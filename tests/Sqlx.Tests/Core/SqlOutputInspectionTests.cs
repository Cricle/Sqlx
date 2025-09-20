// -----------------------------------------------------------------------
// <copyright file="SqlOutputInspectionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Tests to inspect actual SQL output and understand the real behavior.
/// </summary>
[TestClass]
public class SqlOutputInspectionTests
{
    /// <summary>
    /// Test entity for SQL output inspection.
    /// </summary>
    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    #region SQL Output Inspection Tests

    /// <summary>
    /// Inspects SQL Server SQL output to understand actual format.
    /// </summary>
    [TestMethod]
    public void InspectSqlServerOutput()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id > 10)
            .Where(e => e.Name == "Test")
            .Where(e => e.IsActive == true)
            .OrderBy(e => e.Name)
            .Take(50);

        var sql = expression.ToSql();
        var template = expression.ToTemplate();
        var whereClause = expression.ToWhereClause();
        var additionalClause = expression.ToAdditionalClause();

        // Output for inspection
        Console.WriteLine("=== SQL Server Output ===");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine($"Template SQL: {template.Sql}");
        Console.WriteLine($"WHERE Clause: {whereClause}");
        Console.WriteLine($"Additional Clause: {additionalClause}");
        Console.WriteLine($"Parameters Count: {template.Parameters.Count}");
        Console.WriteLine("========================");

        // Basic assertions
        Assert.IsNotNull(sql);
        Assert.IsNotNull(template.Sql);
    }

    /// <summary>
    /// Inspects MySQL SQL output to understand actual format.
    /// </summary>
    [TestMethod]
    public void InspectMySqlOutput()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestEntity>.ForMySql()
            .Where(e => e.Price >= 100.0m)
            .Where(e => e.CreatedDate >= DateTime.Today)
            .OrderByDescending(e => e.CreatedDate)
            .Skip(10)
            .Take(20);

        var sql = expression.ToSql();
        var template = expression.ToTemplate();

        // Output for inspection
        Console.WriteLine("=== MySQL Output ===");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine($"Template SQL: {template.Sql}");
        Console.WriteLine("====================");

        Assert.IsNotNull(sql);
        Assert.IsNotNull(template.Sql);
    }

    /// <summary>
    /// Inspects UPDATE SQL output to understand actual format.
    /// </summary>
    [TestMethod]
    public void InspectUpdateOutput()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Set(e => e.Name, "Updated Name")
            .Set(e => e.Price, 99.99m)
            .Set(e => e.IsActive, false)
            .Where(e => e.Id == 1);

        var sql = expression.ToSql();
        var template = expression.ToTemplate();

        // Output for inspection
        Console.WriteLine("=== UPDATE Output ===");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine($"Template SQL: {template.Sql}");
        Console.WriteLine("=====================");

        Assert.IsNotNull(sql);
        Assert.IsNotNull(template.Sql);
    }

    /// <summary>
    /// Inspects null handling to understand actual behavior.
    /// </summary>
    [TestMethod]
    public void InspectNullHandling()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Name == null)
            .Where(e => e.Name != null);

        var sql = expression.ToSql();

        // Output for inspection
        Console.WriteLine("=== Null Handling Output ===");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine("=============================");

        Assert.IsNotNull(sql);
    }

    /// <summary>
    /// Inspects boolean handling to understand actual behavior.
    /// </summary>
    [TestMethod]
    public void InspectBooleanHandling()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.IsActive)
            .Where(e => e.IsActive == true)
            .Where(e => e.IsActive == false)
            .Where(e => !e.IsActive);

        var sql = expression.ToSql();

        // Output for inspection
        Console.WriteLine("=== Boolean Handling Output ===");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine("===============================");

        Assert.IsNotNull(sql);
    }

    /// <summary>
    /// Inspects string handling to understand actual behavior.
    /// </summary>
    [TestMethod]
    public void InspectStringHandling()
    {
        // Arrange & Act
        var testStrings = new[]
        {
            "Simple String",
            "String with 'quotes'",
            "",
            "'; DROP TABLE Users; --"
        };

        foreach (var testString in testStrings)
        {
            var expression = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Name == testString);

            var sql = expression.ToSql();

            Console.WriteLine($"=== String: '{testString}' ===");
            Console.WriteLine($"SQL: {sql}");
            Console.WriteLine("=============================");
        }

        // Basic assertion
        Assert.IsTrue(true, "String inspection completed");
    }

    /// <summary>
    /// Inspects date handling to understand actual behavior.
    /// </summary>
    [TestMethod]
    public void InspectDateHandling()
    {
        // Arrange & Act
        var testDates = new[]
        {
            new DateTime(2023, 12, 25, 10, 30, 45),
            DateTime.MinValue,
            DateTime.MaxValue,
            DateTime.Today
        };

        foreach (var testDate in testDates)
        {
            var expression = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.CreatedDate == testDate);

            var sql = expression.ToSql();

            Console.WriteLine($"=== Date: {testDate} ===");
            Console.WriteLine($"SQL: {sql}");
            Console.WriteLine("========================");
        }

        Assert.IsTrue(true, "Date inspection completed");
    }

    /// <summary>
    /// Inspects decimal handling to understand actual behavior.
    /// </summary>
    [TestMethod]
    public void InspectDecimalHandling()
    {
        // Arrange & Act
        var testDecimals = new[]
        {
            0m,
            99.99m,
            decimal.MaxValue,
            decimal.MinValue,
            -123.456m
        };

        foreach (var testDecimal in testDecimals)
        {
            var expression = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Price == testDecimal);

            var sql = expression.ToSql();

            Console.WriteLine($"=== Decimal: {testDecimal} ===");
            Console.WriteLine($"SQL: {sql}");
            Console.WriteLine("===========================");
        }

        Assert.IsTrue(true, "Decimal inspection completed");
    }

    /// <summary>
    /// Inspects complex conditions to understand actual behavior.
    /// </summary>
    [TestMethod]
    public void InspectComplexConditions()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestEntity>.ForSqlServer();

        // Add multiple conditions
        for (int i = 0; i < 5; i++)
        {
            expression.Where(e => e.Id != i);
        }

        var sql = expression.ToSql();
        var whereClause = expression.ToWhereClause();

        Console.WriteLine("=== Complex Conditions Output ===");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine($"WHERE Clause: {whereClause}");
        Console.WriteLine($"SQL Length: {sql.Length}");

        // Count AND occurrences
        var andCount = sql.Split(" AND ").Length - 1;
        Console.WriteLine($"AND Count: {andCount}");
        Console.WriteLine("=================================");

        Assert.IsNotNull(sql);
    }

    /// <summary>
    /// Inspects all database dialects side by side.
    /// </summary>
    [TestMethod]
    public void InspectAllDialects()
    {
        // Arrange
        var dialects = new[]
        {
            ("SQL Server", ExpressionToSql<TestEntity>.ForSqlServer()),
            ("MySQL", ExpressionToSql<TestEntity>.ForMySql()),
            ("PostgreSQL", ExpressionToSql<TestEntity>.ForPostgreSQL()),
            ("Oracle", ExpressionToSql<TestEntity>.ForOracle()),
            ("DB2", ExpressionToSql<TestEntity>.ForDB2()),
            ("SQLite", ExpressionToSql<TestEntity>.ForSqlite())
        };

        Console.WriteLine("=== All Dialects Comparison ===");

        // Act & Output
        foreach (var (name, expression) in dialects)
        {
            expression
                .Where(e => e.Id > 0)
                .Where(e => e.Name != null)
                .OrderBy(e => e.Name)
                .Take(10);

            var sql = expression.ToSql();
            Console.WriteLine($"{name}: {sql}");
        }

        Console.WriteLine("===============================");
        Assert.IsTrue(true, "Dialect comparison completed");
    }

    #endregion
}
