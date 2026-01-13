// -----------------------------------------------------------------------
// <copyright file="TDD_BinaryOperatorDialects.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests.Expression;

/// <summary>
/// Tests for binary operator dialect differences in ExpressionToSql.
/// </summary>
[TestClass]
[TestCategory("Expression")]
[TestCategory("Dialects")]
public class TDD_BinaryOperatorDialects
{
    public class TestEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Name { get; set; } = "";
    }

    [TestMethod]
    [Description("Modulo operator should use MOD() function for Oracle")]
    public void ModuloOperator_Oracle_ShouldUseMODFunction()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForOracle();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value % 2 == 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "MOD(");
        Assert.IsFalse(sql.Contains("%"), "Oracle should not use % for modulo");
    }

    [TestMethod]
    [Description("Modulo operator should use % for SQLite")]
    public void ModuloOperator_SQLite_ShouldUsePercentSign()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlite();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value % 2 == 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "%");
    }

    [TestMethod]
    [Description("Modulo operator should use % for PostgreSQL")]
    public void ModuloOperator_PostgreSQL_ShouldUsePercentSign()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForPostgreSQL();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value % 2 == 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "%");
    }

    [TestMethod]
    [Description("Modulo operator should use % for MySQL")]
    public void ModuloOperator_MySQL_ShouldUsePercentSign()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForMySql();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value % 2 == 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "%");
    }

    [TestMethod]
    [Description("Modulo operator should use % for SqlServer")]
    public void ModuloOperator_SqlServer_ShouldUsePercentSign()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlServer();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value % 2 == 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "%");
    }

    [TestMethod]
    [Description("NotEqual operator should use != for Oracle")]
    public void NotEqualOperator_Oracle_ShouldUseExclamationEquals()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForOracle();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value != 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "!=");
    }

    [TestMethod]
    [Description("NotEqual operator should use <> for SQLite")]
    public void NotEqualOperator_SQLite_ShouldUseDiamondOperator()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlite();
        Expression<Func<TestEntity, bool>> predicate = x => x.Value != 0;

        // Act
        var sql = expr.Where(predicate).ToWhereClause();

        // Assert
        StringAssert.Contains(sql, "<>");
    }

    [TestMethod]
    [Description("Boolean literal should use true/false for PostgreSQL")]
    public void BooleanLiteral_PostgreSQL_ShouldUseTrueFalse()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForPostgreSQL();

        // Act - use reflection to test GetBooleanLiteral
        var method = typeof(ExpressionToSqlBase).GetMethod("GetBooleanLiteral",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var trueResult = method?.Invoke(expr, new object[] { true })?.ToString();
        var falseResult = method?.Invoke(expr, new object[] { false })?.ToString();

        // Assert
        Assert.AreEqual("true", trueResult);
        Assert.AreEqual("false", falseResult);
    }

    [TestMethod]
    [Description("Boolean literal should use 1/0 for SQLite")]
    public void BooleanLiteral_SQLite_ShouldUseOneZero()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlite();

        // Act - use reflection to test GetBooleanLiteral
        var method = typeof(ExpressionToSqlBase).GetMethod("GetBooleanLiteral",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var trueResult = method?.Invoke(expr, new object[] { true })?.ToString();
        var falseResult = method?.Invoke(expr, new object[] { false })?.ToString();

        // Assert
        Assert.AreEqual("1", trueResult);
        Assert.AreEqual("0", falseResult);
    }
}
