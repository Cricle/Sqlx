// -----------------------------------------------------------------------
// <copyright file="ExpressionToSql_Comprehensive_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 全面测试 ExpressionToSql 功能，包括所有数据库方言的参数前缀
/// </summary>
[TestClass]
public class ExpressionToSql_Comprehensive_Tests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? NullableText { get; set; }
    }

    #region 参数前缀测试

    [TestMethod]
    [Description("SQL Server ExpressionToSql 应该使用正确的方言配置")]
    public void ExpressionToSql_SqlServer_ShouldUseCorrectDialect()
    {
        // Arrange & Act
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id == 1)
            .Where(e => e.Name == "test");

        var sql = expr.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("["), "SQL Server 应该使用 [] 包裹列名");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
    }

    [TestMethod]
    [Description("MySQL ExpressionToSql 应该使用正确的方言配置")]
    public void ExpressionToSql_MySQL_ShouldUseCorrectDialect()
    {
        // Arrange & Act
        var expr = ExpressionToSql<TestEntity>.ForMySql()
            .Where(e => e.Id == 1)
            .Where(e => e.Name == "test");

        var sql = expr.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("`"), "MySQL 应该使用 `` 包裹列名");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
    }

    [TestMethod]
    [Description("PostgreSQL ExpressionToSql 应该使用正确的方言配置")]
    public void ExpressionToSql_PostgreSQL_ShouldUseCorrectDialect()
    {
        // Arrange & Act
        var expr = ExpressionToSql<TestEntity>.ForPostgreSQL()
            .Where(e => e.Id == 1)
            .Where(e => e.Name == "test");

        var sql = expr.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("\""), "PostgreSQL 应该使用 \"\" 包裹列名");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
    }

    [TestMethod]
    [Description("Oracle ExpressionToSql 应该使用正确的方言配置")]
    public void ExpressionToSql_Oracle_ShouldUseCorrectDialect()
    {
        // Arrange & Act
        var expr = ExpressionToSql<TestEntity>.ForOracle()
            .Where(e => e.Id == 1)
            .Where(e => e.Name == "test");

        var sql = expr.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("\""), "Oracle 应该使用 \"\" 包裹列名");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
    }

    [TestMethod]
    [Description("SQLite ExpressionToSql 应该使用正确的方言配置")]
    public void ExpressionToSql_SQLite_ShouldUseCorrectDialect()
    {
        // Arrange & Act
        var expr = ExpressionToSql<TestEntity>.ForSqlite()
            .Where(e => e.Id == 1)
            .Where(e => e.Name == "test");

        var sql = expr.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("["), "SQLite 应该使用 [] 包裹列名");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
    }

    #endregion

    #region WHERE 子句测试

    [TestMethod]
    [Description("ExpressionToSql WHERE 相等比较")]
    public void ExpressionToSql_Where_Equality()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id == 100);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        Assert.IsTrue(sql.Contains("="), "应该包含等号");
        Assert.IsTrue(sql.Contains("100"), "应该包含值 100");
    }

    [TestMethod]
    [Description("ExpressionToSql WHERE 大于比较")]
    public void ExpressionToSql_Where_GreaterThan()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Age > 18);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        Assert.IsTrue(sql.Contains(">"), "应该包含大于号");
    }

    [TestMethod]
    [Description("ExpressionToSql WHERE 多个条件")]
    public void ExpressionToSql_Where_MultipleConditions()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Age > 18)
            .Where(e => e.IsActive == true)
            .Where(e => e.Balance >= 100m);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        Assert.IsTrue(sql.Contains("AND"), "应该包含 AND 连接符");
    }

    [TestMethod]
    [Description("ExpressionToSql WHERE 字符串比较")]
    public void ExpressionToSql_Where_StringComparison()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Name == "John");

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        Assert.IsTrue(sql.Contains("John"), "应该包含 'John'");
    }

    #endregion

    #region SET 子句测试

    [TestMethod]
    [Description("ExpressionToSql SET 单个字段")]
    public void ExpressionToSql_Set_SingleField()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Set(e => e.Name, "UpdatedName");

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("SET"), "应该包含 SET 子句");
        Assert.IsTrue(sql.Contains("UpdatedName"), "应该包含 'UpdatedName'");
    }

    [TestMethod]
    [Description("ExpressionToSql SET 多个字段")]
    public void ExpressionToSql_Set_MultipleFields()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Set(e => e.Name, "UpdatedName")
            .Set(e => e.Age, 30)
            .Set(e => e.Balance, 1000m);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("SET"), "应该包含 SET 子句");
        Assert.IsTrue(sql.Contains(","), "多个 SET 应该用逗号分隔");
    }

    #endregion

    #region ORDER BY 测试

    [TestMethod]
    [Description("ExpressionToSql ORDER BY 升序")]
    public void ExpressionToSql_OrderBy_Ascending()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .OrderBy(e => e.Name);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"), "应该包含 ORDER BY 子句");
    }

    [TestMethod]
    [Description("ExpressionToSql ORDER BY 降序")]
    public void ExpressionToSql_OrderByDescending()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .OrderByDescending(e => e.CreatedAt);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"), "应该包含 ORDER BY 子句");
        Assert.IsTrue(sql.Contains("DESC"), "应该包含 DESC");
    }

    #endregion

    #region 复杂查询测试

    [TestMethod]
    [Description("ExpressionToSql 复杂查询 - WHERE + ORDER BY")]
    public void ExpressionToSql_Complex_WhereAndOrderBy()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Age > 18)
            .Where(e => e.IsActive == true)
            .OrderBy(e => e.Name);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        Assert.IsTrue(sql.Contains("ORDER BY"), "应该包含 ORDER BY 子句");
        Assert.IsTrue(sql.Contains("AND"), "应该包含 AND 连接符");
    }

    [TestMethod]
    [Description("ExpressionToSql 复杂查询 - SET + WHERE")]
    public void ExpressionToSql_Complex_SetAndWhere()
    {
        var expr = ExpressionToSql<TestEntity>.ForSqlServer()
            .Set(e => e.Name, "UpdatedName")
            .Set(e => e.Age, 30)
            .Where(e => e.Id == 1);

        var sql = expr.ToSql();

        Assert.IsTrue(sql.Contains("SET"), "应该包含 SET 子句");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
    }

    #endregion

    #region 所有方言一致性测试

    [TestMethod]
    [Description("所有方言的 ExpressionToSql 应该生成有效的 SQL")]
    public void ExpressionToSql_AllDialects_ShouldGenerateValidSql()
    {
        var dialects = new[]
        {
            ("SQL Server", ExpressionToSql<TestEntity>.ForSqlServer()),
            ("MySQL", ExpressionToSql<TestEntity>.ForMySql()),
            ("PostgreSQL", ExpressionToSql<TestEntity>.ForPostgreSQL()),
            ("Oracle", ExpressionToSql<TestEntity>.ForOracle()),
            ("SQLite", ExpressionToSql<TestEntity>.ForSqlite())
        };

        foreach (var (name, expr) in dialects)
        {
            expr.Where(e => e.Id == 1)
                .Where(e => e.Name == "test")
                .OrderBy(e => e.CreatedAt);

            var sql = expr.ToSql();

            Assert.IsFalse(string.IsNullOrWhiteSpace(sql), $"{name} 应该生成非空 SQL");
            Assert.IsTrue(sql.Contains("WHERE"), $"{name} 应该包含 WHERE 子句");
            Assert.IsTrue(sql.Contains("ORDER BY"), $"{name} 应该包含 ORDER BY 子句");
        }
    }

    #endregion
}
