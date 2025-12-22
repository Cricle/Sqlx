// -----------------------------------------------------------------------
// <copyright file="TDD_SubqueriesAndSets_Integration.cs" company="Cricle">
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
/// 集成测试: 子查询和集合操作
/// 测试 EXISTS, UNION 等高级 SQL 功能
/// </summary>
[TestClass]
public class TDD_SubqueriesAndSets_Integration
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
    [TestCategory("SubqueriesAndSets")]
    public async Task Subqueries_Exists_FiltersCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        var orderRepo = new OrderRepository(connection);
        
        // 创建用户
        var userId1 = await userRepo.InsertAsync("高价值客户", "vip@example.com", 30, 10000m, DateTime.Now, true);
        var userId2 = await userRepo.InsertAsync("普通客户", "normal@example.com", 25, 1000m, DateTime.Now, true);
        var userId3 = await userRepo.InsertAsync("新客户", "new@example.com", 28, 500m, DateTime.Now, true);
        
        // 创建订单（只有部分用户有高价值订单）
        await orderRepo.InsertAsync(userId1, 5000m, "completed", "system");
        await orderRepo.InsertAsync(userId1, 3000m, "completed", "system");
        await orderRepo.InsertAsync(userId2, 100m, "completed", "system");
        // userId3 没有订单

        // Act - 使用 {{exists}} 子查询查找有高价值订单的客户
        var highValueCustomers = await advancedRepo.GetHighValueCustomersAsync(1000m);

        // Assert
        Assert.IsTrue(highValueCustomers.Count > 0, "应该找到高价值客户");
        Assert.IsTrue(highValueCustomers.Any(u => u.Id == userId1), "应该包含有高价值订单的用户");
        Assert.IsFalse(highValueCustomers.Any(u => u.Id == userId2), "不应该包含只有低价值订单的用户");
        Assert.IsFalse(highValueCustomers.Any(u => u.Id == userId3), "不应该包含没有订单的用户");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("SubqueriesAndSets")]
    public async Task Sets_Union_CombinesResults()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 创建高余额用户
        await userRepo.InsertAsync("富豪用户", "rich@example.com", 30, 15000m, DateTime.Now, true);
        await userRepo.InsertAsync("普通用户", "normal@example.com", 25, 1000m, DateTime.Now, true);
        
        // 创建高价产品
        await productRepo.InsertAsync("豪华笔记本", "Electronics", 12000m, 5);
        await productRepo.InsertAsync("普通鼠标", "Electronics", 50m, 100);

        // Act - 使用 {{union}} 合并高价值实体
        var highValueEntities = await advancedRepo.GetHighValueEntitiesAsync(10000m, 10000m);

        // Assert
        Assert.IsTrue(highValueEntities.Count > 0, "应该返回高价值实体");
        
        // 验证包含用户和产品
        var userEntities = highValueEntities.Where(e => e["type"]?.ToString() == "user").ToList();
        var productEntities = highValueEntities.Where(e => e["type"]?.ToString() == "product").ToList();
        
        Assert.IsTrue(userEntities.Count > 0, "应该包含高余额用户");
        Assert.IsTrue(productEntities.Count > 0, "应该包含高价产品");
        
        // 验证名称
        Assert.IsTrue(userEntities.Any(e => e["name"]?.ToString() == "富豪用户"));
        Assert.IsTrue(productEntities.Any(e => e["name"]?.ToString() == "豪华笔记本"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("SubqueriesAndSets")]
    public async Task Sets_Union_RemovesDuplicates()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 创建同名的用户和产品
        await userRepo.InsertAsync("重复名称", "user@example.com", 30, 15000m, DateTime.Now, true);
        await productRepo.InsertAsync("重复名称", "Electronics", 12000m, 5);

        // Act - UNION 应该去重
        var entities = await advancedRepo.GetHighValueEntitiesAsync(10000m, 10000m);

        // Assert
        var duplicateNames = entities.Where(e => e["name"]?.ToString() == "重复名称").ToList();
        Assert.AreEqual(2, duplicateNames.Count, "UNION 应该保留不同类型的记录（user 和 product）");
    }
}
