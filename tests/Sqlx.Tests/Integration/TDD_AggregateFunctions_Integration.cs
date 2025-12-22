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
public class TDD_AggregateFunctions_Integration : IntegrationTestBase
{
    public TDD_AggregateFunctions_Integration()
    {
        _needsSeedData = true;  // 需要预置数据
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Sum_SQLite()
    {
        // Arrange - 预置数据已经包含15个用户，总余额 = 17500
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);

        // Act - 使用 {{sum balance}} 占位符
        var totalBalance = await repo.GetTotalBalanceAsync();

        // Assert
        Assert.AreEqual(17500m, totalBalance, "总余额应该是17500（15个预置用户）");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("AggregateFunctions")]
    public async Task AggregateFunctions_Avg_SQLite()
    {
        // Arrange - 预置数据已经包含15个用户，年龄分布：25(5个), 30(5个), 35(5个)
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);

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
        // Arrange - 预置数据已经包含15个用户，最高余额是5000
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);

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
        // Arrange - 预置数据已经包含15个用户
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);

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
        // Arrange - 预置数据已经包含15个用户，年龄分布：25(5个), 30(5个), 35(5个)
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);

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
