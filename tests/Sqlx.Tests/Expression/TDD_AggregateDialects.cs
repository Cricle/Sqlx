// -----------------------------------------------------------------------
// <copyright file="TDD_AggregateDialects.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Sqlx.Tests.Expression;

/// <summary>
/// Tests for aggregate function dialect differences in ExpressionToSql.
/// </summary>
[TestClass]
[TestCategory("Expression")]
[TestCategory("Dialects")]
public class TDD_AggregateDialects
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Salary { get; set; }
    }

    [TestMethod]
    [Description("StringAgg should use GROUP_CONCAT with SEPARATOR for MySQL")]
    public void StringAgg_MySQL_ShouldUseGroupConcatWithSeparator()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForMySql();
        var method = typeof(ExpressionToSqlBase).GetMethod("GetStringAggSyntax",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method?.Invoke(expr, new object[] { "name", "','" })?.ToString();

        // Assert
        Assert.AreEqual("GROUP_CONCAT(name SEPARATOR ',')", result);
    }

    [TestMethod]
    [Description("StringAgg should use GROUP_CONCAT for SQLite")]
    public void StringAgg_SQLite_ShouldUseGroupConcat()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForSqlite();
        var method = typeof(ExpressionToSqlBase).GetMethod("GetStringAggSyntax",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method?.Invoke(expr, new object[] { "name", "','" })?.ToString();

        // Assert
        Assert.AreEqual("GROUP_CONCAT(name, ',')", result);
    }

    [TestMethod]
    [Description("StringAgg should use LISTAGG for Oracle")]
    public void StringAgg_Oracle_ShouldUseListagg()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForOracle();
        var method = typeof(ExpressionToSqlBase).GetMethod("GetStringAggSyntax",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method?.Invoke(expr, new object[] { "name", "','" })?.ToString();

        // Assert
        Assert.AreEqual("LISTAGG(name, ',') WITHIN GROUP (ORDER BY name)", result);
    }

    [TestMethod]
    [Description("StringAgg should use STRING_AGG for PostgreSQL")]
    public void StringAgg_PostgreSQL_ShouldUseStringAgg()
    {
        // Arrange
        var expr = ExpressionToSql<TestEntity>.ForPostgreSQL();
        var method = typeof(ExpressionToSqlBase).GetMethod("GetStringAggSyntax",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method?.Invoke(expr, new object[] { "name", "','" })?.ToString();

        // Assert
        Assert.AreEqual("STRING_AGG(name, ',')", result);
    }

    [TestMethod]
    [Description("Basic aggregate functions should work for all dialects")]
    public void BasicAggregates_AllDialects_ShouldWork()
    {
        // Arrange & Act
        var sqliteSql = ExpressionToSql<TestEntity>.ForSqlite()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToSql();

        var mysqlSql = ExpressionToSql<TestEntity>.ForMySql()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToSql();

        var oracleSql = ExpressionToSql<TestEntity>.ForOracle()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToSql();

        // Assert - all should contain COUNT(*)
        StringAssert.Contains(sqliteSql, "COUNT(*)");
        StringAssert.Contains(mysqlSql, "COUNT(*)");
        StringAssert.Contains(oracleSql, "COUNT(*)");
    }

    [TestMethod]
    [Description("SUM aggregate should work for all dialects")]
    public void SumAggregate_AllDialects_ShouldWork()
    {
        // Arrange & Act
        var sqliteSql = ExpressionToSql<TestEntity>.ForSqlite()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Salary) })
            .ToSql();

        var oracleSql = ExpressionToSql<TestEntity>.ForOracle()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Salary) })
            .ToSql();

        // Assert
        StringAssert.Contains(sqliteSql, "SUM(");
        StringAssert.Contains(oracleSql, "SUM(");
    }

    [TestMethod]
    [Description("AVG aggregate should work for all dialects")]
    public void AvgAggregate_AllDialects_ShouldWork()
    {
        // Arrange & Act
        var sqliteSql = ExpressionToSql<TestEntity>.ForSqlite()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Avg = g.Average(x => x.Salary) })
            .ToSql();

        var postgresqlSql = ExpressionToSql<TestEntity>.ForPostgreSQL()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Avg = g.Average(x => x.Salary) })
            .ToSql();

        // Assert
        StringAssert.Contains(sqliteSql, "AVG(");
        StringAssert.Contains(postgresqlSql, "AVG(");
    }

    [TestMethod]
    [Description("MAX/MIN aggregates should work for all dialects")]
    public void MaxMinAggregate_AllDialects_ShouldWork()
    {
        // Arrange & Act
        var sqliteSql = ExpressionToSql<TestEntity>.ForSqlite()
            .GroupBy(x => x.Name)
            .Select(g => new { Key = g.Key, Max = g.Max(x => x.Salary), Min = g.Min(x => x.Salary) })
            .ToSql();

        // Assert
        StringAssert.Contains(sqliteSql, "MAX(");
        StringAssert.Contains(sqliteSql, "MIN(");
    }
}
