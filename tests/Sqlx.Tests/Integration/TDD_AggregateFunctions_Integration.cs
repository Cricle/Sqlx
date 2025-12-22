// -----------------------------------------------------------------------
// <copyright file="TDD_AggregateFunctions_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.TestModels;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 聚合函数占位符集成测试
/// 测试: {{count}}, {{sum}}, {{avg}}, {{max}}, {{min}}
/// </summary>
[TestClass]
public class TDD_AggregateFunctions_Integration
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Sum_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);  // 插入测试数据
        var repo = new UserRepository(connection);
        await repo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("用户2", "user2@example.com", 30, 2000m, DateTime.Now, true);
        await repo.InsertAsync("用户3", "user3@example.com", 35, 3000m, DateTime.Now, true);

        // Act - 使用 {{sum balance}} 占位符
        var totalBalance = await repo.GetTotalBalanceAsync();

        // Assert
        Assert.AreEqual(6000m, totalBalance, "总余额应该是6000");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Avg_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("用户1", "user1@example.com", 20, 1000m, DateTime.Now, true);
        await repo.InsertAsync("用户2", "user2@example.com", 30, 2000m, DateTime.Now, true);
        await repo.InsertAsync("用户3", "user3@example.com", 40, 3000m, DateTime.Now, true);

        // Act - 使用 {{avg age}} 占位符
        var avgAge = await repo.GetAverageAgeAsync();

        // Assert
        Assert.AreEqual(30.0, avgAge, 0.1, "平均年龄应该是30");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Max_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("用户2", "user2@example.com", 30, 5000m, DateTime.Now, true);
        await repo.InsertAsync("用户3", "user3@example.com", 35, 3000m, DateTime.Now, true);

        // Act - 使用 {{max balance}} 占位符
        var maxBalance = await repo.GetMaxBalanceAsync();

        // Assert
        Assert.AreEqual(5000m, maxBalance, "最高余额应该是5000");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Count_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var testUsers = IntegrationTestHelpers.GenerateTestUsers(15);
        foreach (var user in testUsers)
        {
            await repo.InsertAsync(user.Name, user.Email, user.Age, user.Balance, user.CreatedAt, user.IsActive);
        }

        // Act - 使用 {{count}} 占位符
        var count = await repo.CountAsync();

        // Assert
        Assert.AreEqual(15, count, "应该有15个用户");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Distinct_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("用户2", "user2@example.com", 25, 2000m, DateTime.Now, true);
        await repo.InsertAsync("用户3", "user3@example.com", 30, 3000m, DateTime.Now, true);
        await repo.InsertAsync("用户4", "user4@example.com", 30, 4000m, DateTime.Now, true);
        await repo.InsertAsync("用户5", "user5@example.com", 35, 5000m, DateTime.Now, true);

        // Act - 使用 {{distinct age}} 占位符
        var distinctAges = await repo.GetDistinctAgesAsync();

        // Debug output
        Console.WriteLine($"Distinct ages count: {distinctAges.Count}");
        Console.WriteLine($"Distinct ages: {string.Join(", ", distinctAges)}");

        // Assert
        Assert.AreEqual(3, distinctAges.Count, $"应该有3个不同的年龄，但实际得到{distinctAges.Count}个: {string.Join(", ", distinctAges)}");
        Assert.IsTrue(distinctAges.Contains(25));
        Assert.IsTrue(distinctAges.Contains(30));
        Assert.IsTrue(distinctAges.Contains(35));
    }
}
