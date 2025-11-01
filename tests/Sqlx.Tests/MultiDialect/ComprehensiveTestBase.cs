// -----------------------------------------------------------------------
// <copyright file="ComprehensiveTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

// ==================== 通用测试实体 ====================

public class DialectUser
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

// ==================== 通用仓储接口基类 ====================

public partial interface IDialectUserRepositoryBase
{
    // ==================== CRUD操作 ====================

    Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);
    Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<DialectUser>> GetAllAsync(CancellationToken ct = default);
    Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);
    Task<int> DeleteAsync(long id, CancellationToken ct = default);

    // ==================== WHERE子句 ====================

    Task<DialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<List<DialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);
    Task<List<DialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);
    Task<List<DialectUser>> GetByActiveStatusAsync(bool isActive, CancellationToken ct = default);

    // ==================== NULL处理 ====================

    Task<List<DialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);
    Task<List<DialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);
    Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    // ==================== 聚合函数 ====================

    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
    Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);
    Task<double> GetAverageAgeAsync(CancellationToken ct = default);
    Task<int> GetMinAgeAsync(CancellationToken ct = default);
    Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    // ==================== ORDER BY ====================

    Task<List<DialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);
    Task<List<DialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);
    Task<List<DialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    // ==================== LIMIT和OFFSET ====================

    Task<List<DialectUser>> GetTopUsersAsync(int limit, CancellationToken ct = default);
    Task<List<DialectUser>> GetUsersPaginatedAsync(int limit, int offset, CancellationToken ct = default);

    // ==================== LIKE模式匹配 ====================

    Task<List<DialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    // ==================== BETWEEN ====================

    Task<List<DialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    // ==================== IN操作符 ====================

    Task<List<DialectUser>> GetUsersBySpecificAgesAsync(CancellationToken ct = default);

    // ==================== GROUP BY ====================

    Task<List<Dictionary<string, object>>> GetUserCountByAgeAsync(CancellationToken ct = default);

    // ==================== DISTINCT ====================

    Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    // ==================== 子查询 ====================

    Task<List<DialectUser>> GetAboveAverageAgeUsersAsync(CancellationToken ct = default);

    // ==================== 字符串函数 ====================

    Task<DialectUser?> GetByCaseInsensitiveUsernameAsync(string username, CancellationToken ct = default);

    // ==================== 批量操作 ====================

    Task<int> DeleteInactiveUsersAsync(CancellationToken ct = default);
    Task<int> ApplyBalanceBonusAsync(CancellationToken ct = default);
}

// ==================== 测试基类 ====================

/// <summary>
/// 数据库方言综合测试基类 - 一次编写，所有方言都能运行
/// </summary>
public abstract class ComprehensiveTestBase : IDisposable
{
    protected DbConnection? _connection;
    protected abstract string DialectName { get; }
    protected abstract string TableName { get; }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    protected abstract DbConnection CreateConnection();

    /// <summary>
    /// 创建测试表
    /// </summary>
    protected abstract void CreateTable();

    /// <summary>
    /// 创建仓储实例
    /// </summary>
    protected abstract IDialectUserRepositoryBase CreateRepository();

    /// <summary>
    /// 获取预期的自增ID起始值（某些数据库从1开始，某些从其他值开始）
    /// </summary>
    protected virtual long ExpectedFirstId => 1;

    [TestInitialize]
    public virtual void Initialize()
    {
        // 检查是否应该跳过测试
        if (DatabaseConnectionHelper.ShouldSkipTest(DialectName))
        {
            Assert.Inconclusive($"{DialectName} tests are only run in CI environment.");
            return;
        }

        _connection = CreateConnection();
        CreateTable();
    }

    public virtual void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== CRUD测试 ====================

    [TestMethod]
    public async Task Insert_ShouldReturnAutoIncrementId()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        var id = await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);

        Assert.IsTrue(id >= ExpectedFirstId);
    }

    [TestMethod]
    public async Task InsertMultiple_ShouldAutoIncrement()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        var id1 = await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        var id2 = await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, true);
        var id3 = await repo.InsertAsync("user3", "user3@test.com", 35, 300m, now, null, true);

        Assert.IsTrue(id1 > 0);
        Assert.IsTrue(id2 > id1);
        Assert.IsTrue(id3 > id2);
    }

    [TestMethod]
    public async Task GetById_ShouldReturnCorrectUser()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        var id = await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        var user = await repo.GetByIdAsync(id);

        Assert.IsNotNull(user);
        Assert.AreEqual("user1", user.Username);
        Assert.AreEqual(25, user.Age);
        Assert.AreEqual(100m, user.Balance);
    }

    [TestMethod]
    public async Task Update_ShouldModifyUser()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        var id = await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        var updated = await repo.UpdateAsync(id, "newemail@test.com", 500m);

        Assert.AreEqual(1, updated);

        var user = await repo.GetByIdAsync(id);
        Assert.AreEqual("newemail@test.com", user.Email);
        Assert.AreEqual(500m, user.Balance);
    }

    [TestMethod]
    public async Task Delete_ShouldRemoveUser()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        var id = await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        var deleted = await repo.DeleteAsync(id);

        Assert.AreEqual(1, deleted);

        var user = await repo.GetByIdAsync(id);
        Assert.IsNull(user);
    }

    // ==================== WHERE子句测试 ====================

    [TestMethod]
    public async Task GetByUsername_ShouldFind()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("alice", "alice@test.com", 25, 100m, now, null, true);
        var user = await repo.GetByUsernameAsync("alice");

        Assert.IsNotNull(user);
        Assert.AreEqual("alice", user.Username);
    }

    [TestMethod]
    public async Task GetByAgeRange_ShouldFilterCorrectly()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 20, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 25, 200m, now, null, true);
        await repo.InsertAsync("user3", "user3@test.com", 30, 300m, now, null, true);
        await repo.InsertAsync("user4", "user4@test.com", 35, 400m, now, null, true);

        var users = await repo.GetByAgeRangeAsync(22, 32);

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(u => u.Age >= 22 && u.Age <= 32));
    }

    // ==================== NULL处理测试 ====================

    [TestMethod]
    public async Task NullHandling_ShouldWork()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, now, true);

        var neverLoggedIn = await repo.GetNeverLoggedInUsersAsync();
        var loggedIn = await repo.GetLoggedInUsersAsync();

        Assert.AreEqual(1, neverLoggedIn.Count);
        Assert.AreEqual(1, loggedIn.Count);
    }

    // ==================== 聚合函数测试 ====================

    [TestMethod]
    public async Task Count_ShouldReturnCorrectCount()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, false);
        await repo.InsertAsync("user3", "user3@test.com", 35, 300m, now, null, true);

        var totalCount = await repo.CountAsync();
        var activeCount = await repo.CountActiveAsync();

        Assert.AreEqual(3, totalCount);
        Assert.AreEqual(2, activeCount);
    }

    [TestMethod]
    public async Task AggregateFunctions_ShouldCalculateCorrectly()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 20, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, true);
        await repo.InsertAsync("user3", "user3@test.com", 40, 300m, now, null, true);

        var totalBalance = await repo.GetTotalBalanceAsync();
        var avgAge = await repo.GetAverageAgeAsync();
        var minAge = await repo.GetMinAgeAsync();
        var maxBalance = await repo.GetMaxBalanceAsync();

        Assert.AreEqual(600m, totalBalance);
        Assert.AreEqual(30.0, avgAge);
        Assert.AreEqual(20, minAge);
        Assert.AreEqual(300m, maxBalance);
    }

    // ==================== ORDER BY测试 ====================

    [TestMethod]
    public async Task OrderBy_ShouldSortCorrectly()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("charlie", "c@test.com", 30, 300m, now, null, true);
        await repo.InsertAsync("alice", "a@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("bob", "b@test.com", 35, 200m, now, null, true);

        var byUsername = await repo.GetAllOrderByUsernameAsync();
        var byBalance = await repo.GetAllOrderByBalanceDescAsync();

        Assert.AreEqual("alice", byUsername[0].Username);
        Assert.AreEqual("bob", byUsername[1].Username);
        Assert.AreEqual("charlie", byUsername[2].Username);

        Assert.AreEqual(300m, byBalance[0].Balance);
        Assert.AreEqual(200m, byBalance[1].Balance);
        Assert.AreEqual(100m, byBalance[2].Balance);
    }

    // ==================== LIMIT和OFFSET测试 ====================

    [TestMethod]
    public async Task Limit_ShouldReturnTopN()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        for (int i = 1; i <= 10; i++)
        {
            await repo.InsertAsync($"user{i}", $"user{i}@test.com", 20 + i, i * 100m, now, null, true);
        }

        var top3 = await repo.GetTopUsersAsync(3);

        Assert.AreEqual(3, top3.Count);
    }

    [TestMethod]
    public async Task LimitOffset_ShouldPaginate()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        for (int i = 1; i <= 10; i++)
        {
            await repo.InsertAsync($"user{i}", $"user{i}@test.com", 20 + i, i * 100m, now, null, true);
        }

        var page1 = await repo.GetUsersPaginatedAsync(3, 0);
        var page2 = await repo.GetUsersPaginatedAsync(3, 3);

        Assert.AreEqual(3, page1.Count);
        Assert.AreEqual(3, page2.Count);
        Assert.AreNotEqual(page1[0].Id, page2[0].Id);
    }

    // ==================== LIKE模式匹配测试 ====================

    [TestMethod]
    public async Task LikePattern_ShouldMatchCorrectly()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("alice", "alice@example.com", 25, 100m, now, null, true);
        await repo.InsertAsync("alicia", "alicia@example.com", 30, 200m, now, null, true);
        await repo.InsertAsync("bob", "bob@test.com", 35, 300m, now, null, true);

        var users = await repo.SearchByUsernameAsync("ali%");

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(u => u.Username.StartsWith("ali")));
    }

    // ==================== GROUP BY测试 ====================

    [TestMethod]
    public async Task GroupBy_ShouldGroupCorrectly()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 25, 200m, now, null, true);
        await repo.InsertAsync("user3", "user3@test.com", 30, 300m, now, null, true);

        var groups = await repo.GetUserCountByAgeAsync();

        Assert.AreEqual(2, groups.Count);
        var age25Group = groups.FirstOrDefault(g => Convert.ToInt32(g["age"]) == 25);
        Assert.IsNotNull(age25Group);
        Assert.AreEqual(2L, age25Group["count"]);
    }

    // ==================== DISTINCT测试 ====================

    [TestMethod]
    public async Task Distinct_ShouldRemoveDuplicates()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 25, 200m, now, null, true);
        await repo.InsertAsync("user3", "user3@test.com", 30, 300m, now, null, true);
        await repo.InsertAsync("user4", "user4@test.com", 30, 400m, now, null, true);

        var distinctAges = await repo.GetDistinctAgesAsync();

        Assert.AreEqual(2, distinctAges.Count);
    }

    // ==================== 子查询测试 ====================

    [TestMethod]
    public async Task Subquery_ShouldFilterCorrectly()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 20, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, true);
        await repo.InsertAsync("user3", "user3@test.com", 40, 300m, now, null, true);

        var aboveAverage = await repo.GetAboveAverageAgeUsersAsync();

        Assert.IsTrue(aboveAverage.Count > 0);
        Assert.IsTrue(aboveAverage.All(u => u.Age > 30)); // 平均值是30
    }

    // ==================== 字符串函数测试 ====================

    [TestMethod]
    public async Task CaseInsensitive_ShouldMatch()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("Alice", "alice@test.com", 25, 100m, now, null, true);

        var user1 = await repo.GetByCaseInsensitiveUsernameAsync("alice");
        var user2 = await repo.GetByCaseInsensitiveUsernameAsync("ALICE");
        var user3 = await repo.GetByCaseInsensitiveUsernameAsync("AlIcE");

        Assert.IsNotNull(user1);
        Assert.IsNotNull(user2);
        Assert.IsNotNull(user3);
    }

    // ==================== 批量操作测试 ====================

    [TestMethod]
    public async Task BatchDelete_ShouldRemoveMultiple()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, false);
        await repo.InsertAsync("user3", "user3@test.com", 35, 300m, now, null, false);

        var deleted = await repo.DeleteInactiveUsersAsync();

        Assert.AreEqual(2, deleted);

        var remaining = await repo.CountAsync();
        Assert.AreEqual(1, remaining);
    }

    [TestMethod]
    public async Task BatchUpdate_ShouldModifyMultiple()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, true);
        await repo.InsertAsync("user3", "user3@test.com", 35, 300m, now, null, false);

        var updated = await repo.ApplyBalanceBonusAsync();

        Assert.AreEqual(2, updated);

        var user1 = await repo.GetByUsernameAsync("user1");
        Assert.AreEqual(105m, user1.Balance); // 100 * 1.05
    }
}

