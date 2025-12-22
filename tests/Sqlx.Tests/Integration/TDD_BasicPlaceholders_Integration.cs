// -----------------------------------------------------------------------
// <copyright file="TDD_BasicPlaceholders_Integration.cs" company="Cricle">
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
/// 基础占位符集成测试 - 覆盖所有支持的数据库
/// 测试: {{columns}}, {{table}}, {{values}}, {{set}}, {{orderby}}, {{limit}}, {{offset}}
/// </summary>
[TestClass]
public class TDD_BasicPlaceholders_Integration
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
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_Insert_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var testUsers = IntegrationTestHelpers.GenerateTestUsers(5);

        // Act
        foreach (var user in testUsers)
        {
            await repo.InsertAsync(user.Name, user.Email, user.Age, user.Balance, user.CreatedAt, user.IsActive);
        }

        // Assert
        var allUsers = await repo.GetAllAsync();
        Assert.AreEqual(5, allUsers.Count, "应该插入5个用户");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_SelectColumns_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("测试用户", "test@example.com", 25, 5000m, DateTime.Now, true);

        // Act - 使用 {{columns}} 占位符
        var users = await repo.GetAllAsync();

        // Assert
        Assert.AreEqual(1, users.Count);
        var user = users[0];
        Assert.AreEqual("测试用户", user.Name);
        Assert.AreEqual("test@example.com", user.Email);
        Assert.AreEqual(25, user.Age);
        Assert.AreEqual(5000m, user.Balance);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_Update_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var userId = await repo.InsertAsync("原始名称", "original@example.com", 25, 5000m, DateTime.Now, true);

        // Act - 使用 {{set}} 占位符更新
        var user = await repo.GetByIdAsync(userId);
        user!.Name = "更新后名称";
        user.Age = 26;
        user.Balance = 6000m;
        await repo.UpdateAsync(user);

        // Assert
        var updated = await repo.GetByIdAsync(userId);
        Assert.AreEqual("更新后名称", updated!.Name);
        Assert.AreEqual(26, updated.Age);
        Assert.AreEqual(6000m, updated.Balance);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_OrderBy_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        await repo.InsertAsync("用户A", "a@example.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("用户B", "b@example.com", 30, 5000m, DateTime.Now, true);
        await repo.InsertAsync("用户C", "c@example.com", 35, 10000m, DateTime.Now, true);

        // Act - 使用 {{orderby balance --desc}} 占位符
        var topUsers = await repo.GetTopRichUsersAsync(2);

        // Assert
        Assert.AreEqual(2, topUsers.Count);
        Assert.AreEqual("用户C", topUsers[0].Name, "第一个应该是余额最高的");
        Assert.AreEqual("用户B", topUsers[1].Name, "第二个应该是余额第二高的");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_Pagination_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var testUsers = IntegrationTestHelpers.GenerateTestUsers(10);
        foreach (var user in testUsers)
        {
            await repo.InsertAsync(user.Name, user.Email, user.Age, user.Balance, user.CreatedAt, user.IsActive);
        }

        // Act - 使用 {{limit}} {{offset}} 占位符
        var page1 = await repo.GetPagedAsync(3, 0);
        var page2 = await repo.GetPagedAsync(3, 3);
        var page3 = await repo.GetPagedAsync(3, 6);

        // Assert
        Assert.AreEqual(3, page1.Count, "第1页应该有3条记录");
        Assert.AreEqual(3, page2.Count, "第2页应该有3条记录");
        Assert.AreEqual(3, page3.Count, "第3页应该有3条记录");

        // 验证没有重复
        var allIds = page1.Select(u => u.Id)
            .Concat(page2.Select(u => u.Id))
            .Concat(page3.Select(u => u.Id))
            .ToList();
        Assert.AreEqual(9, allIds.Distinct().Count(), "不应该有重复的记录");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_Count_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var testUsers = IntegrationTestHelpers.GenerateTestUsers(7);
        foreach (var user in testUsers)
        {
            await repo.InsertAsync(user.Name, user.Email, user.Age, user.Balance, user.CreatedAt, user.IsActive);
        }

        // Act - 使用 {{count}} 占位符
        var count = await repo.CountAsync();

        // Assert
        Assert.AreEqual(7, count);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BasicPlaceholders")]
    public async Task BasicPlaceholders_Delete_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new UserRepository(connection);
        var userId = await repo.InsertAsync("待删除用户", "delete@example.com", 25, 5000m, DateTime.Now, true);

        // Act
        await repo.DeleteAsync(userId);

        // Assert
        var user = await repo.GetByIdAsync(userId);
        Assert.IsNull(user, "用户应该被删除");
    }
}
