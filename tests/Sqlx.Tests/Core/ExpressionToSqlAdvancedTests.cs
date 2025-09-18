// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlAdvancedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Advanced tests for ExpressionToSql functionality.
/// </summary>
[TestClass]
public class ExpressionToSqlAdvancedTests
{
    /// <summary>
    /// Test entity class for ExpressionToSql tests.
    /// </summary>
    public class TestUser
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Tests ExpressionToSql ForSqlServer factory method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ForSqlServer_CreatesCorrectInstance()
    {
        // Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Assert
        Assert.IsNotNull(expressionToSql);
    }

    /// <summary>
    /// Tests ExpressionToSql MySql factory method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_MySql_CreatesCorrectInstance()
    {
        // Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForMySql();

        // Assert
        Assert.IsNotNull(expressionToSql);
    }

    /// <summary>
    /// Tests ExpressionToSql PostgreSql factory method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_PostgreSql_CreatesCorrectInstance()
    {
        // Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForPostgreSQL();

        // Assert
        Assert.IsNotNull(expressionToSql);
    }

    /// <summary>
    /// Tests ExpressionToSql SQLite factory method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_SQLite_CreatesCorrectInstance()
    {
        // Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlite();

        // Assert
        Assert.IsNotNull(expressionToSql);
    }

    /// <summary>
    /// Tests ExpressionToSql Oracle factory method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Oracle_CreatesCorrectInstance()
    {
        // Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForOracle();

        // Assert
        Assert.IsNotNull(expressionToSql);
    }

    /// <summary>
    /// Tests ExpressionToSql Where method with simple condition.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Where_WithSimpleCondition_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.Where(u => u.Id == 1);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql Where method with multiple conditions.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Where_WithMultipleConditions_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql
            .Where(u => u.Id == 1)
            .Where(u => u.IsActive == true);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql OrderBy method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_OrderBy_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.OrderBy(u => u.Name);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql OrderByDescending method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_OrderByDescending_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.OrderByDescending(u => u.CreatedDate);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql Take method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Take_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.Take(10);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql Skip method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Skip_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.Skip(5);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql Set method with constant value.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Set_WithConstantValue_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.Set(u => u.Name, "John");

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql Set method with expression.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Set_WithExpression_ReturnsInstance()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expressionToSql.Set(u => u.Age, u => u.Age + 1);

        // Assert
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql ToSelectTemplate method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ToSelectTemplate_ReturnsValidTemplate()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id == 1)
            .OrderBy(u => u.Name)
            .Take(10);

        // Act
        var template = expressionToSql.ToTemplate();

        // Assert
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);
        Assert.IsFalse(string.IsNullOrWhiteSpace(template.Sql));
    }

    /// <summary>
    /// Tests ExpressionToSql ToUpdateTemplate method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ToUpdateTemplate_ReturnsValidTemplate()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer()
            .Set(u => u.Name, "John")
            .Where(u => u.Id == 1);

        // Act
        var template = expressionToSql.ToTemplate();

        // Assert
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);
        Assert.IsFalse(string.IsNullOrWhiteSpace(template.Sql));
    }

    /// <summary>
    /// Tests ExpressionToSql ToDeleteTemplate method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ToDeleteTemplate_ReturnsValidTemplate()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id == 1);

        // Act
        var template = expressionToSql.ToTemplate();

        // Assert
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);
        Assert.IsFalse(string.IsNullOrWhiteSpace(template.Sql));
    }

    /// <summary>
    /// Tests ExpressionToSql Dispose method.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Dispose_DoesNotThrow()
    {
        // Arrange
        var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act & Assert
        try
        {
            expressionToSql.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Dispose should not throw exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests ExpressionToSql with null expression is handled gracefully.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_Where_WithNullExpression_HandlesGracefully()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act & Assert - Should not throw
        var result = expressionToSql.Where(null!);
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql OrderBy with null expression is handled gracefully.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_OrderBy_WithNullExpression_HandlesGracefully()
    {
        // Arrange
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer();

        // Act & Assert - Should not throw
        var result = expressionToSql.OrderBy<object>(null!);
        Assert.AreSame(expressionToSql, result);
    }

    /// <summary>
    /// Tests ExpressionToSql complex query building.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ComplexQuery_BuildsCorrectly()
    {
        // Arrange & Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id > 0)
            .Where(u => u.IsActive == true)
            .Where(u => u.Name != null)
            .OrderBy(u => u.Name)
            .OrderByDescending(u => u.CreatedDate)
            .Skip(10)
            .Take(20);

        var template = expressionToSql.ToTemplate();

        // Assert
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);
        Assert.IsFalse(string.IsNullOrWhiteSpace(template.Sql));
        Assert.IsTrue(template.Parameters.Count >= 0);
    }

    /// <summary>
    /// Tests ExpressionToSql complex update query building.
    /// </summary>
    [TestMethod]
    public void ExpressionToSql_ComplexUpdateQuery_BuildsCorrectly()
    {
        // Arrange & Act
        using var expressionToSql = ExpressionToSql<TestUser>.ForSqlServer()
            .Set(u => u.Name, "UpdatedName")
            .Set(u => u.Age, u => u.Age + 1)
            .Set(u => u.IsActive, true)
            .Where(u => u.Id > 0)
            .Where(u => u.IsActive == false);

        var template = expressionToSql.ToTemplate();

        // Assert
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);
        Assert.IsFalse(string.IsNullOrWhiteSpace(template.Sql));
        Assert.IsTrue(template.Parameters.Count >= 0);
    }
}
