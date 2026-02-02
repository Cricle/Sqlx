// <copyright file="SetExpressionExtensionsTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SetExpressionExtensions - converting Expression&lt;Func&lt;T, T&gt;&gt; to SET clauses.
/// </summary>
[TestClass]
public class SetExpressionExtensionsTests
{
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
    }

    [TestMethod]
    public void ToSetClause_SingleField_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Name = "John" };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[name] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_MultipleFields_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = "John",
            Email = "john@example.com"
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[name] = @p0, [email] = @p1", result);
    }

    [TestMethod]
    public void ToSetClause_IncrementExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Version = u.Version + 1 };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[version] = ([version] + @p0)", result);
    }

    [TestMethod]
    public void ToSetClause_MixedUpdate_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = "John",
            Version = u.Version + 1
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[name] = @p0, [version] = ([version] + @p1)", result);
    }

    [TestMethod]
    public void ToSetClause_BooleanField_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { IsActive = true };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[is_active] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_ComplexExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Age = u.Age + 1,
            Version = u.Version + 1
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[age] = ([age] + @p0), [version] = ([version] + @p1)", result);
    }

    [TestMethod]
    public void ToSetClause_PostgreSQL_UsesDoubleQuotes()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Name = "John" };

        // Act
        var result = expr.ToSetClause(SqlDefine.PostgreSql);

        // Assert
        Assert.AreEqual("\"name\" = $p0", result);
    }

    [TestMethod]
    public void ToSetClause_MySQL_UsesBackticks()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Name = "John" };

        // Act
        var result = expr.ToSetClause(SqlDefine.MySql);

        // Assert
        Assert.AreEqual("`name` = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_NullExpression_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>>? expr = null;

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToSetClause_InvalidExpression_ThrowsException()
    {
        // Arrange - not a member init expression
        Expression<Func<TestUser, TestUser>> expr = u => u;

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() => expr.ToSetClause());
        Assert.IsTrue(ex.Message.Contains("member initialization"));
    }

    [TestMethod]
    public void GetSetParameters_SingleField_ExtractsParameter()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Name = "John" };

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual("John", parameters["p0"]);
    }

    [TestMethod]
    public void GetSetParameters_MultipleFields_ExtractsAllParameters()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = "John",
            Age = 30
        };

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(2, parameters.Count);
        Assert.AreEqual("John", parameters["p0"]);
        Assert.AreEqual(30, parameters["p1"]);
    }

    [TestMethod]
    public void GetSetParameters_IncrementExpression_ExtractsConstant()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Version = u.Version + 1 };

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual(1, parameters["p0"]);
    }
}
