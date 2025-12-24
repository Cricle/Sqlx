// -----------------------------------------------------------------------
// <copyright file="TDD_DialectPlaceholders_Integration.cs" company="Cricle">
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
/// 集成测试: 方言特定占位符
/// 测试 {{bool_true}}, {{bool_false}}, {{current_timestamp}} 等方言特定占位符
/// </summary>
[TestClass]
[DoNotParallelize]
public class TDD_DialectPlaceholders_Integration
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
    [TestCategory("DialectPlaceholders")]
    public async Task DialectPlaceholders_BoolTrue_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("活跃用户", "active@example.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("非活跃用户", "inactive@example.com", 30, 2000m, DateTime.Now, false);

        // Act - 查询活跃用户
        var activeUsers = await repo.GetActiveUsersAsync();

        // Assert
        Assert.AreEqual(1, activeUsers.Count, "应该只有1个活跃用户");
        Assert.AreEqual("活跃用户", activeUsers[0].Name);
        Assert.IsTrue(activeUsers[0].IsActive);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("DialectPlaceholders")]
    public async Task DialectPlaceholders_BoolFalse_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("活跃用户", "active@example.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("非活跃用户", "inactive@example.com", 30, 2000m, DateTime.Now, false);

        // Act - 查询非活跃用户
        var inactiveUsers = await repo.GetInactiveUsersAsync();

        // Assert
        Assert.AreEqual(1, inactiveUsers.Count, "应该只有1个非活跃用户");
        Assert.AreEqual("非活跃用户", inactiveUsers[0].Name);
        Assert.IsFalse(inactiveUsers[0].IsActive);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("DialectPlaceholders")]
    public async Task DialectPlaceholders_CurrentTimestamp_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var beforeInsert = DateTime.UtcNow.AddSeconds(-1);

        // Act - 插入用户（created_at 使用当前时间）
        var userId = await repo.InsertAsync("测试用户", "test@example.com", 25, 1000m, DateTime.UtcNow, true);
        var user = await repo.GetByIdAsync(userId);
        var afterInsert = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.IsNotNull(user);
        Assert.IsTrue(user.CreatedAt >= beforeInsert, "创建时间应该在插入之后");
        Assert.IsTrue(user.CreatedAt <= afterInsert, "创建时间应该在插入之前");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("DialectPlaceholders")]
    public async Task DialectPlaceholders_AutoIncrement_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);

        // Act - 插入多个用户，验证自动递增ID
        var id1 = await repo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        var id2 = await repo.InsertAsync("用户2", "user2@example.com", 30, 2000m, DateTime.Now, true);
        var id3 = await repo.InsertAsync("用户3", "user3@example.com", 35, 3000m, DateTime.Now, true);

        // Assert
        Assert.IsTrue(id2 > id1, "第二个ID应该大于第一个");
        Assert.IsTrue(id3 > id2, "第三个ID应该大于第二个");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("DialectPlaceholders")]
    public async Task DialectPlaceholders_SoftDelete_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var productRepo = new ProductRepository(connection);
        
        // Act - 插入产品并软删除
        var productId = await productRepo.InsertAsync("测试产品", "Electronics", 999m, 10);
        await productRepo.SoftDeleteAsync(productId);
        var activeProducts = await productRepo.GetActiveProductsAsync();

        // Assert
        Assert.AreEqual(0, activeProducts.Count, "软删除后应该没有活跃产品");
    }
}
