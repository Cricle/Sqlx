// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlCreateTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for ExpressionToSql factory methods.
/// </summary>
[TestClass]
public class ExpressionToSqlCreateTests : TestBase
{
    [TestMethod]
    public void ForSqlServer_CreatesInstance()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void ForMySql_CreatesInstance()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.ForMySql();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void ForPostgreSQL_CreatesInstance()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.ForPostgreSQL();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void ForOracle_CreatesInstance()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.ForOracle();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void ForDB2_CreatesInstance()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.ForDB2();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void ForSqlite_CreatesInstance()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.ForSqlite();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void Create_CreatesInstanceWithDefaultDialect()
    {
        // Act
        var expr = ExpressionToSql<TestEntity>.Create();

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(ExpressionToSql<TestEntity>));
    }

    [TestMethod]
    public void BasicWhere_GeneratesSQL()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var sql = expr.Where(x => x.Id == 1).ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void BasicOrderBy_GeneratesSQL()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var sql = expr.OrderBy(x => x.Name).ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    public void BasicSelect_GeneratesSQL()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var sql = expr.Select(x => x.Name).ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    public void Take_GeneratesSQL()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var sql = expr.Take(10).ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void Skip_GeneratesSQL()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var sql = expr.Skip(5).ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void ChainedMethods_ReturnSameInstance()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var result = expr.Where(x => x.Id > 0).OrderBy(x => x.Name);

        // Assert
        Assert.AreSame(expr, result);
    }

    [TestMethod]
    public void MultipleWhere_GeneratesSQL()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act
        var sql = expr
            .Where(x => x.Id > 0)
            .Where(x => x.Name != null)
            .ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();

        // Act & Assert - Should not throw
        expr.Dispose();
        Assert.IsTrue(true);
    }

    /// <summary>
    /// Test entity class.
    /// </summary>
    private class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }
}

