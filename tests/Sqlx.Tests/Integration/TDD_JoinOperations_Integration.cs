// -----------------------------------------------------------------------
// <copyright file="TDD_JoinOperations_Integration.cs" company="Cricle">
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
/// 集成测试: JOIN 操作
/// 测试 INNER JOIN, LEFT JOIN 等多表关联查询
/// </summary>
[TestClass]
public class TDD_JoinOperations_Integration
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
    [TestCategory("JoinOperations")]
    public async Task JoinOperations_InnerJoin_ReturnsMatchingRecords()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 插入测试数据
        await productRepo.InsertAsync("笔记本电脑", "Electronics", 5000m, 10);
        await productRepo.InsertAsync("鼠标", "Electronics", 100m, 50);
        await productRepo.InsertAsync("Python编程", "Books", 80m, 30);

        // Act - 使用 {{join}} 进行 INNER JOIN
        var productDetails = await advancedRepo.GetProductDetailsAsync();

        // Assert
        Assert.IsTrue(productDetails.Count > 0, "应该返回关联的产品详情");
        
        // 验证返回的数据包含产品和分类信息
        foreach (var detail in productDetails)
        {
            Assert.IsTrue(detail.ProductId > 0);
            Assert.IsFalse(string.IsNullOrEmpty(detail.ProductName));
            Assert.IsTrue(detail.Price > 0);
            Assert.IsFalse(string.IsNullOrEmpty(detail.CategoryName));
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("JoinOperations")]
    public async Task JoinOperations_LeftJoin_IncludesNullRecords()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        var orderRepo = new OrderRepository(connection);
        
        // 插入用户（有些有订单，有些没有）
        var userId1 = await userRepo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        var userId2 = await userRepo.InsertAsync("用户2", "user2@example.com", 30, 2000m, DateTime.Now, true);
        var userId3 = await userRepo.InsertAsync("用户3", "user3@example.com", 35, 3000m, DateTime.Now, true);
        
        // 只为部分用户创建订单
        await orderRepo.InsertAsync(userId1, 500m, "completed", "system");
        await orderRepo.InsertAsync(userId1, 300m, "pending", "system");
        await orderRepo.InsertAsync(userId2, 1000m, "completed", "system");
        // userId3 没有订单

        // Act - 使用 LEFT JOIN 获取用户统计（包括没有订单的用户）
        var userStats = await advancedRepo.GetUserStatsAsync();

        // Assert
        Assert.IsTrue(userStats.Count >= 2, "应该至少包含有订单的用户");
        
        // 验证有订单的用户
        var user1Stats = userStats.FirstOrDefault(s => s.UserId == userId1);
        Assert.IsNotNull(user1Stats);
        Assert.AreEqual(2, user1Stats.OrderCount, "用户1应该有2个订单");
        Assert.AreEqual(800m, user1Stats.TotalSpent, "用户1总消费应该是800");
        
        var user2Stats = userStats.FirstOrDefault(s => s.UserId == userId2);
        Assert.IsNotNull(user2Stats);
        Assert.AreEqual(1, user2Stats.OrderCount, "用户2应该有1个订单");
        Assert.AreEqual(1000m, user2Stats.TotalSpent, "用户2总消费应该是1000");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("JoinOperations")]
    public async Task JoinOperations_GroupByWithJoin_AggregatesCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        var orderRepo = new OrderRepository(connection);
        
        var userId = await userRepo.InsertAsync("测试用户", "test@example.com", 30, 5000m, DateTime.Now, true);
        
        // 创建多个订单
        await orderRepo.InsertAsync(userId, 100m, "completed", "system");
        await orderRepo.InsertAsync(userId, 200m, "completed", "system");
        await orderRepo.InsertAsync(userId, 300m, "pending", "system");

        // Act - 使用 {{groupby}} 和 JOIN 进行聚合
        var userStats = await advancedRepo.GetUserStatsAsync();

        // Assert
        var stats = userStats.FirstOrDefault(s => s.UserId == userId);
        Assert.IsNotNull(stats);
        Assert.AreEqual("测试用户", stats.UserName);
        Assert.AreEqual(3, stats.OrderCount, "应该有3个订单");
        Assert.AreEqual(600m, stats.TotalSpent, "总消费应该是600");
    }
}
