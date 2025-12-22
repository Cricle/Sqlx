// -----------------------------------------------------------------------
// <copyright file="TDD_CaseExpression_Integration.cs" company="Cricle">
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
/// 集成测试: CASE 表达式
/// 测试 {{case}} 占位符和条件表达式
/// </summary>
[TestClass]
public class TDD_CaseExpression_Integration
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
    [TestCategory("CaseExpression")]
    public async Task CaseExpression_UserLevel_CategorizesCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        
        // 创建不同余额的用户
        await userRepo.InsertAsync("VIP用户", "vip@example.com", 30, 15000m, DateTime.Now, true);
        await userRepo.InsertAsync("高级用户", "premium@example.com", 25, 7000m, DateTime.Now, true);
        await userRepo.InsertAsync("普通用户", "regular@example.com", 28, 3000m, DateTime.Now, true);

        // Act - 使用 {{case}} 根据余额分类用户等级
        var usersWithLevel = await advancedRepo.GetUsersWithLevelAsync();

        // Assert
        Assert.AreEqual(3, usersWithLevel.Count);
        
        // 验证 VIP 用户 (balance > 10000)
        var vipUser = usersWithLevel.FirstOrDefault(u => u["name"]?.ToString() == "VIP用户");
        Assert.IsNotNull(vipUser);
        Assert.AreEqual("VIP", vipUser["level"]?.ToString());
        
        // 验证高级用户 (balance > 5000)
        var premiumUser = usersWithLevel.FirstOrDefault(u => u["name"]?.ToString() == "高级用户");
        Assert.IsNotNull(premiumUser);
        Assert.AreEqual("Premium", premiumUser["level"]?.ToString());
        
        // 验证普通用户 (其他)
        var regularUser = usersWithLevel.FirstOrDefault(u => u["name"]?.ToString() == "普通用户");
        Assert.IsNotNull(regularUser);
        Assert.AreEqual("Regular", regularUser["level"]?.ToString());
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("CaseExpression")]
    public async Task CaseExpression_MultipleConditions_WorksCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        
        // 创建边界值测试用户
        await userRepo.InsertAsync("边界VIP", "boundary_vip@example.com", 30, 10001m, DateTime.Now, true);
        await userRepo.InsertAsync("边界Premium", "boundary_premium@example.com", 25, 5001m, DateTime.Now, true);
        await userRepo.InsertAsync("边界Regular", "boundary_regular@example.com", 28, 4999m, DateTime.Now, true);

        // Act
        var usersWithLevel = await advancedRepo.GetUsersWithLevelAsync();

        // Assert
        var boundaryVip = usersWithLevel.FirstOrDefault(u => u["name"]?.ToString() == "边界VIP");
        Assert.IsNotNull(boundaryVip);
        Assert.AreEqual("VIP", boundaryVip["level"]?.ToString(), "余额 > 10000 应该是 VIP");
        
        var boundaryPremium = usersWithLevel.FirstOrDefault(u => u["name"]?.ToString() == "边界Premium");
        Assert.IsNotNull(boundaryPremium);
        Assert.AreEqual("Premium", boundaryPremium["level"]?.ToString(), "余额 > 5000 应该是 Premium");
        
        var boundaryRegular = usersWithLevel.FirstOrDefault(u => u["name"]?.ToString() == "边界Regular");
        Assert.IsNotNull(boundaryRegular);
        Assert.AreEqual("Regular", boundaryRegular["level"]?.ToString(), "余额 <= 5000 应该是 Regular");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("CaseExpression")]
    public async Task CaseExpression_AllUsersInSameCategory_ReturnsCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var userRepo = new UserRepository(connection);
        
        // 创建所有都是普通用户
        await userRepo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        await userRepo.InsertAsync("用户2", "user2@example.com", 30, 2000m, DateTime.Now, true);
        await userRepo.InsertAsync("用户3", "user3@example.com", 35, 3000m, DateTime.Now, true);

        // Act
        var usersWithLevel = await advancedRepo.GetUsersWithLevelAsync();

        // Assert
        Assert.AreEqual(3, usersWithLevel.Count);
        Assert.IsTrue(usersWithLevel.All(u => u["level"]?.ToString() == "Regular"), 
            "所有用户都应该是 Regular 等级");
    }
}
