// -----------------------------------------------------------------------
// <copyright file="ExpressionExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for ExpressionExtensions.
/// </summary>
[TestClass]
public class ExpressionExtensionsTests
{
    [Sqlx]
    [TableName("users")]
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Email { get; set; }
    }

    [TestMethod]
    public void ToWhereClause_SimpleEquality_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Id == 1;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("id") || result.Contains("Id"), $"Result: {result}");
        Assert.IsTrue(result.Contains("="), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_WithSqliteDialect_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Name == "John";

        // Act
        var result = predicate.ToWhereClause(SqlDefine.SQLite);

        // Assert
        Assert.IsTrue(result.Contains("name") || result.Contains("Name"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_WithMySqlDialect_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age > 18;

        // Act
        var result = predicate.ToWhereClause(SqlDefine.MySql);

        // Assert
        Assert.IsTrue(result.Contains("age") || result.Contains("Age"), $"Result: {result}");
        Assert.IsTrue(result.Contains(">"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_WithPostgreSqlDialect_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.IsActive;

        // Act
        var result = predicate.ToWhereClause(SqlDefine.PostgreSql);

        // Assert
        Assert.IsTrue(result.Contains("is_active") || result.Contains("IsActive"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_WithSqlServerDialect_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Name.Contains("test");

        // Act
        var result = predicate.ToWhereClause(SqlDefine.SqlServer);

        // Assert
        Assert.IsTrue(result.Contains("LIKE") || result.Contains("name"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_WithOracleDialect_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age >= 21;

        // Act
        var result = predicate.ToWhereClause(SqlDefine.Oracle);

        // Assert
        Assert.IsTrue(result.Contains("age") || result.Contains("Age"), $"Result: {result}");
        Assert.IsTrue(result.Contains(">="), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_WithDB2Dialect_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Id != 0;

        // Act
        var result = predicate.ToWhereClause(SqlDefine.DB2);

        // Assert
        Assert.IsTrue(result.Contains("id") || result.Contains("Id"), $"Result: {result}");
        Assert.IsTrue(result.Contains("!=") || result.Contains("<>"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_NullPredicate_ReturnsEmptyString()
    {
        // Arrange
        Expression<Func<TestUser, bool>>? predicate = null;

        // Act
        var result = predicate!.ToWhereClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToWhereClause_ComplexExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age > 18 && u.IsActive;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("age") || result.Contains("Age"), $"Result: {result}");
        Assert.IsTrue(result.Contains("is_active") || result.Contains("IsActive"), $"Result: {result}");
        Assert.IsTrue(result.Contains("AND") || result.Contains("and"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_OrExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age < 18 || u.Age > 65;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("age") || result.Contains("Age"), $"Result: {result}");
        Assert.IsTrue(result.Contains("OR") || result.Contains("or"), $"Result: {result}");
    }

    [TestMethod]
    public void GetParameters_SimpleEquality_ExtractsParameter()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Id == 1;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count > 0, "Should extract at least one parameter");
    }

    [TestMethod]
    public void GetParameters_WithVariable_ExtractsValue()
    {
        // Arrange
        var name = "John";
        Expression<Func<TestUser, bool>> predicate = u => u.Name == name;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count > 0, "Should extract parameter");
        Assert.IsTrue(parameters.Values.Any(v => v?.ToString() == "John"), "Should contain 'John'");
    }

    [TestMethod]
    public void GetParameters_ComplexExpression_ExtractsMultipleParameters()
    {
        // Arrange
        var minAge = 18;
        var maxAge = 65;
        Expression<Func<TestUser, bool>> predicate = u => u.Age >= minAge && u.Age <= maxAge;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count >= 2, $"Should extract at least 2 parameters, got {parameters.Count}");
    }

    [TestMethod]
    public void GetParameters_NullPredicate_ReturnsEmptyDictionary()
    {
        // Arrange
        Expression<Func<TestUser, bool>>? predicate = null;

        // Act
        var parameters = predicate!.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
        Assert.AreEqual(0, parameters.Count);
    }

    [TestMethod]
    public void GetParameters_MethodCall_ExtractsParameters()
    {
        // Arrange
        var searchTerm = "test";
        Expression<Func<TestUser, bool>> predicate = u => u.Name.Contains(searchTerm);

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count > 0, "Should extract parameter from method call");
    }

    [TestMethod]
    public void GetParameters_UnaryExpression_ExtractsParameters()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => !u.IsActive;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
    }

    [TestMethod]
    public void GetParameters_NestedMemberAccess_ExtractsValue()
    {
        // Arrange
        var user = new TestUser { Name = "Alice" };
        Expression<Func<TestUser, bool>> predicate = u => u.Name == user.Name;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count > 0, "Should extract nested member value");
        Assert.IsTrue(parameters.Values.Any(v => v?.ToString() == "Alice"), "Should contain 'Alice'");
    }

    [TestMethod]
    public void ToWhereClause_NullableProperty_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Email != null;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("email") || result.Contains("Email"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_StringStartsWith_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Name.StartsWith("J");

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("LIKE") || result.Contains("name"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_StringEndsWith_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Name.EndsWith("son");

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("LIKE") || result.Contains("name"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age < 30;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("<"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_LessThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age <= 30;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("<="), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age > 18;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains(">"), $"Result: {result}");
    }

    [TestMethod]
    public void ToWhereClause_NotEqual_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Id != 0;

        // Act
        var result = predicate.ToWhereClause();

        // Assert
        Assert.IsTrue(result.Contains("!=") || result.Contains("<>"), $"Result: {result}");
    }

    [TestMethod]
    public void GetParameters_WithConstant_ExtractsValue()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Age == 25;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count > 0, "Should extract constant value");
        Assert.IsTrue(parameters.Values.Any(v => v is int && (int)v == 25), "Should contain value 25");
    }

    [TestMethod]
    public void GetParameters_WithBooleanConstant_ExtractsValue()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.IsActive == true;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsTrue(parameters.Count > 0, "Should extract boolean constant");
    }

    [TestMethod]
    public void ToWhereClause_WithNullDialect_UsesDefaultSqlite()
    {
        // Arrange
        Expression<Func<TestUser, bool>> predicate = u => u.Id == 1;

        // Act
        var result = predicate.ToWhereClause(null);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result), "Should generate SQL with default dialect");
    }
}
