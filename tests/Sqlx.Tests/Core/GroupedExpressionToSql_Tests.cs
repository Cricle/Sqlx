// -----------------------------------------------------------------------
// <copyright file="GroupedExpressionToSql_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 测试 GroupedExpressionToSql 分组查询功能
/// </summary>
[TestClass]
public class GroupedExpressionToSql_Tests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class GroupResult
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
    }

    [TestMethod]
    [Description("GroupBy 应该创建分组查询")]
    public void GroupBy_ShouldCreateGroupedQuery()
    {
        // Arrange & Act
        var grouped = ExpressionToSql<TestEntity>.ForSqlServer()
            .GroupBy(e => e.Category);

        // Assert
        Assert.IsNotNull(grouped);
    }

    [TestMethod]
    [Description("GroupBy 后应该能够 Select")]
    public void GroupBy_ThenSelect_ShouldWork()
    {
        // Arrange & Act
        var query = ExpressionToSql<TestEntity>.ForSqlServer()
            .GroupBy(e => e.Category)
            .Select<GroupResult>(g => new GroupResult
            {
                Category = g.Key,
                TotalAmount = g.Sum(x => x.Amount),
                TotalCount = g.Count()
            });

        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("GROUP BY"), "应该包含 GROUP BY");
    }

    [TestMethod]
    [Description("GroupBy 后应该能够添加 Having 条件")]
    public void GroupBy_WithHaving_ShouldWork()
    {
        // Arrange & Act
        var grouped = ExpressionToSql<TestEntity>.ForSqlServer()
            .GroupBy(e => e.Category)
            .Having(g => g.Sum(x => x.Amount) > 1000);

        // Assert
        Assert.IsNotNull(grouped);
    }

    [TestMethod]
    [Description("GroupBy 多个字段应该工作")]
    public void GroupBy_MultipleFields_ShouldWork()
    {
        // Arrange & Act
        var query = ExpressionToSql<TestEntity>.ForSqlServer()
            .AddGroupBy("Category")
            .AddGroupBy("Name");

        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("GROUP BY"), "应该包含 GROUP BY");
    }

    [TestMethod]
    [Description("GroupBy 使用不同方言应该工作")]
    public void GroupBy_WithDifferentDialects_ShouldWork()
    {
        var dialects = new[]
        {
            ("SQL Server", ExpressionToSql<TestEntity>.ForSqlServer()),
            ("MySQL", ExpressionToSql<TestEntity>.ForMySql()),
            ("PostgreSQL", ExpressionToSql<TestEntity>.ForPostgreSQL())
        };

        foreach (var (name, expr) in dialects)
        {
            // Act
            var grouped = expr.GroupBy(e => e.Category);
            
            // Assert
            Assert.IsNotNull(grouped, $"{name} 应该支持 GroupBy");
        }
    }
}
