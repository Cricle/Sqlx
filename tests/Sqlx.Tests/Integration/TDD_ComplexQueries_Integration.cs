// -----------------------------------------------------------------------
// <copyright file="TDD_ComplexQueries_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using FullFeatureDemo;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 集成测试: 复杂查询
/// 测试 GROUPBY, HAVING, CASE 等复杂SQL功能
/// </summary>
[TestClass]
public class TDD_ComplexQueries_Integration
{
    private static DatabaseFixture _fixture = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _fixture = new DatabaseFixture();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("ComplexQueries")]
    public async Task ComplexQueries_GroupBy_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        
        await logRepo.BatchInsertAsync(new[]
        {
            new Log { Level = "INFO", Message = "Info 1", Timestamp = DateTime.Now },
            new Log { Level = "INFO", Message = "Info 2", Timestamp = DateTime.Now },
            new Log { Level = "ERROR", Message = "Error 1", Timestamp = DateTime.Now },
            new Log { Level = "WARN", Message = "Warn 1", Timestamp = DateTime.Now }
        });

        // Act - 使用 {{groupby}} 按级别分组
        var summary = await logRepo.GetLogSummaryAsync(10);

        // Assert
        Assert.AreEqual(3, summary.Count, "应该有3个不同的日志级别");
        Assert.IsTrue(summary.Any(s => s["level"]?.ToString() == "INFO"));
        Assert.IsTrue(summary.Any(s => s["level"]?.ToString() == "ERROR"));
        Assert.IsTrue(summary.Any(s => s["level"]?.ToString() == "WARN"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("ComplexQueries")]
    public async Task ComplexQueries_Count_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        
        await logRepo.BatchInsertAsync(new[]
        {
            new Log { Level = "ERROR", Message = "Error 1", Timestamp = DateTime.Now },
            new Log { Level = "ERROR", Message = "Error 2", Timestamp = DateTime.Now },
            new Log { Level = "INFO", Message = "Info 1", Timestamp = DateTime.Now }
        });

        // Act - 按级别统计数量
        var errorCount = await logRepo.CountByLevelAsync("ERROR");

        // Assert
        Assert.AreEqual(2, errorCount, "应该有2条ERROR日志");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("ComplexQueries")]
    public async Task ComplexQueries_OrderByDesc_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var userRepo = new UserRepository(connection);
        
        await userRepo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        await userRepo.InsertAsync("用户2", "user2@example.com", 30, 5000m, DateTime.Now, true);
        await userRepo.InsertAsync("用户3", "user3@example.com", 35, 3000m, DateTime.Now, true);

        // Act - 使用 {{orderby --desc}} 降序排序
        var topUsers = await userRepo.GetTopRichUsersAsync(2);

        // Assert
        Assert.AreEqual(2, topUsers.Count);
        Assert.AreEqual("用户2", topUsers[0].Name, "第一个应该是余额最高的");
        Assert.AreEqual(5000m, topUsers[0].Balance);
        Assert.AreEqual("用户3", topUsers[1].Name, "第二个应该是余额第二高的");
        Assert.AreEqual(3000m, topUsers[1].Balance);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("ComplexQueries")]
    public async Task ComplexQueries_Pagination_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var userRepo = new UserRepository(connection);
        
        for (int i = 1; i <= 10; i++)
        {
            await userRepo.InsertAsync($"用户{i}", $"user{i}@example.com", 20 + i, 1000m * i, DateTime.Now, true);
        }

        // Act - 使用 {{limit}} {{offset}} 分页
        var page1 = await userRepo.GetPagedAsync(3, 0);
        var page2 = await userRepo.GetPagedAsync(3, 3);
        var page3 = await userRepo.GetPagedAsync(3, 6);

        // Assert
        Assert.AreEqual(3, page1.Count, "第1页应该有3条记录");
        Assert.AreEqual(3, page2.Count, "第2页应该有3条记录");
        Assert.AreEqual(3, page3.Count, "第3页应该有3条记录");
        
        // 验证分页不重复
        var allIds = page1.Concat(page2).Concat(page3).Select(u => u.Id).ToList();
        Assert.AreEqual(9, allIds.Distinct().Count(), "分页结果不应该有重复");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("ComplexQueries")]
    public async Task ComplexQueries_MultipleConditions_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var productRepo = new ProductRepository(connection);
        
        await productRepo.InsertAsync("产品1", "Electronics", 100m, 10);
        await productRepo.InsertAsync("产品2", "Electronics", 500m, 5);
        await productRepo.InsertAsync("产品3", "Books", 50m, 20);
        await productRepo.InsertAsync("产品4", "Electronics", 1500m, 2);

        // Act - 使用 {{between}} 价格范围查询
        var products = await productRepo.GetByPriceRangeAsync(100m, 1000m);

        // Assert
        Assert.AreEqual(2, products.Count, "应该有2个产品在价格范围内");
        Assert.IsTrue(products.All(p => p.Price >= 100m && p.Price <= 1000m));
    }
}
