// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_EdgeCases.cs" company="Cricle">
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
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 边界测试实体 ====================

public class EdgeCaseUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int? NullableAge { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AuditEntity
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

// ==================== 场景13: 可空类型处理 ====================

public partial interface INullableTypeRepo
{
    [SqlTemplate("INSERT INTO edge_users (name, nullable_age, created_at, is_active) VALUES (@name, @age, @createdAt, @isActive)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int? age, DateTime? createdAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE nullable_age IS NOT NULL")]
    Task<List<EdgeCaseUser>> GetUsersWithAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE nullable_age IS NULL")]
    Task<List<EdgeCaseUser>> GetUsersWithoutAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE created_at > @date")]
    Task<List<EdgeCaseUser>> GetUsersAfterDateAsync(DateTime date, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(INullableTypeRepo))]
public partial class NullableTypeRepo(DbConnection connection) : INullableTypeRepo
{
}

// ==================== 场景14: 布尔值处理 ====================

public partial interface IBooleanRepo
{
    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE is_active = @isActive")]
    Task<List<EdgeCaseUser>> GetByStatusAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("UPDATE edge_users SET is_active = @isActive WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM edge_users WHERE is_active = 1")]
    Task<int> CountActiveUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM edge_users WHERE is_active = 0")]
    Task<int> CountInactiveUsersAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBooleanRepo))]
public partial class BooleanRepo(DbConnection connection) : IBooleanRepo
{
}

// ==================== 场景15: DateTime处理 ====================

public partial interface IDateTimeRepo
{
    [SqlTemplate("INSERT INTO edge_users (name, nullable_age, created_at, is_active) VALUES (@name, @age, @timestamp, 1)")]
    [ReturnInsertedId]
    Task<long> InsertWithTimestampAsync(string name, int? age, DateTime timestamp, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE created_at BETWEEN @start AND @end")]
    Task<List<EdgeCaseUser>> GetUsersInRangeAsync(DateTime start, DateTime end, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users ORDER BY created_at DESC LIMIT 1")]
    Task<EdgeCaseUser?> GetLatestUserAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT MIN(created_at), MAX(created_at) FROM edge_users")]
    Task<Dictionary<string, object>> GetDateRangeAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDateTimeRepo))]
public partial class DateTimeRepo(DbConnection connection) : IDateTimeRepo
{
}

// ==================== 场景16: 字符串模式匹配 ====================

public partial interface IPatternMatchRepo
{
    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE name LIKE @pattern")]
    Task<List<EdgeCaseUser>> SearchByPatternAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE name LIKE @pattern ESCAPE '\\'")]
    Task<List<EdgeCaseUser>> SearchWithEscapeAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM edge_users WHERE name LIKE @prefix || '%'")]
    Task<int> CountByPrefixAsync(string prefix, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPatternMatchRepo))]
public partial class PatternMatchRepo(DbConnection connection) : IPatternMatchRepo
{
}

// ==================== 场景17: 排序和分页 ====================

public partial interface ISortingPagingRepo
{
    [SqlTemplate("SELECT {{columns}} FROM edge_users ORDER BY name ASC LIMIT @limit OFFSET @offset")]
    Task<List<EdgeCaseUser>> GetPagedAsync(int limit, int offset, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users ORDER BY created_at DESC, name ASC LIMIT @limit")]
    Task<List<EdgeCaseUser>> GetTopRecentAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE nullable_age IS NOT NULL ORDER BY nullable_age ASC")]
    Task<List<EdgeCaseUser>> GetOrderedByAgeAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISortingPagingRepo))]
public partial class SortingPagingRepo(DbConnection connection) : ISortingPagingRepo
{
}

// ==================== 场景18: CASE表达式 ====================

public partial interface ICaseExpressionRepo
{
    [SqlTemplate(@"
        SELECT id, name,
        CASE
            WHEN nullable_age < 18 THEN 'Minor'
            WHEN nullable_age >= 18 AND nullable_age < 65 THEN 'Adult'
            ELSE 'Senior'
        END as age_group
        FROM edge_users
        WHERE nullable_age IS NOT NULL")]
    Task<List<Dictionary<string, object>>> GetAgeGroupsAsync(CancellationToken ct = default);

    [SqlTemplate(@"
        SELECT
        CASE
            WHEN is_active = 1 THEN 'Active'
            ELSE 'Inactive'
        END as status,
        COUNT(*) as count
        FROM edge_users
        GROUP BY is_active")]
    Task<List<Dictionary<string, object>>> GetStatusDistributionAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICaseExpressionRepo))]
public partial class CaseExpressionRepo(DbConnection connection) : ICaseExpressionRepo
{
}

// ==================== 场景19: DISTINCT查询 ====================

public partial interface IDistinctRepo
{
    [SqlTemplate("SELECT DISTINCT nullable_age as Age FROM edge_users WHERE nullable_age IS NOT NULL ORDER BY nullable_age")]
    Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(DISTINCT nullable_age) FROM edge_users WHERE nullable_age IS NOT NULL")]
    Task<int> CountDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT DISTINCT name as Name FROM edge_users ORDER BY name")]
    Task<List<Dictionary<string, object>>> GetDistinctNamesAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDistinctRepo))]
public partial class DistinctRepo(DbConnection connection) : IDistinctRepo
{
}

// ==================== 场景20: 子查询 ====================

public partial interface ISubqueryRepo
{
    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE nullable_age > (SELECT AVG(nullable_age) FROM edge_users WHERE nullable_age IS NOT NULL)")]
    Task<List<EdgeCaseUser>> GetAboveAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM edge_users WHERE id IN (SELECT id FROM edge_users ORDER BY created_at DESC LIMIT @limit)")]
    Task<List<EdgeCaseUser>> GetRecentUsersAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM edge_users WHERE nullable_age > (SELECT AVG(nullable_age) FROM edge_users)")]
    Task<int> CountAboveAverageAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISubqueryRepo))]
public partial class SubqueryRepo(DbConnection connection) : ISubqueryRepo
{
}

// ==================== 场景21: 审计日志 ====================

public partial interface IAuditLogRepo
{
    [SqlTemplate("INSERT INTO audit_logs (entity_type, action, timestamp, details) VALUES (@entityType, @action, @timestamp, @details)")]
    [ReturnInsertedId]
    Task<long> LogAsync(string entityType, string action, DateTime timestamp, string? details, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM audit_logs WHERE entity_type = @entityType ORDER BY timestamp DESC LIMIT @limit")]
    Task<List<AuditEntity>> GetRecentLogsAsync(string entityType, int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM audit_logs WHERE timestamp >= @since")]
    Task<List<AuditEntity>> GetLogsSinceAsync(DateTime since, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM audit_logs WHERE timestamp < @before")]
    Task<int> CleanupOldLogsAsync(DateTime before, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAuditLogRepo))]
public partial class AuditLogRepo(DbConnection connection) : IAuditLogRepo
{
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数和有参构造函数的边界测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_EdgeCases : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_EdgeCases()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE edge_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                nullable_age INTEGER,
                created_at TEXT,
                is_active INTEGER DEFAULT 1
            );

            CREATE TABLE audit_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                entity_type TEXT NOT NULL,
                action TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                details TEXT
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== 可空类型测试 ====================

    [TestMethod]
    public async Task NullableType_InsertWithNull_ShouldSucceed()
    {
        // Arrange
        var repo = new NullableTypeRepo(_connection);

        // Act - 插入NULL age
        var id1 = await repo.InsertAsync("User1", null, null, true);

        // Act - 插入非NULL age
        var id2 = await repo.InsertAsync("User2", 25, DateTime.UtcNow, true);

        // Assert
        Assert.IsTrue(id1 > 0);
        Assert.IsTrue(id2 > 0);

        var withAge = await repo.GetUsersWithAgeAsync();
        var withoutAge = await repo.GetUsersWithoutAgeAsync();

        Assert.AreEqual(1, withAge.Count);
        Assert.AreEqual(1, withoutAge.Count);
    }

    [TestMethod]
    public async Task NullableType_QueryWithNullableDateTime_ShouldFilter()
    {
        // Arrange
        var repo = new NullableTypeRepo(_connection);
        var now = DateTime.UtcNow;
        var past = now.AddDays(-1);

        await repo.InsertAsync("Past", 20, past, true);
        await repo.InsertAsync("Now", 25, now, true);
        await repo.InsertAsync("NoDate", 30, null, true);

        // Act
        var recentUsers = await repo.GetUsersAfterDateAsync(past.AddHours(1));

        // Assert
        Assert.AreEqual(1, recentUsers.Count);
        Assert.AreEqual("Now", recentUsers[0].Name);
    }

    // ==================== 布尔值测试 ====================

    [TestMethod]
    public async Task Boolean_InsertAndQuery_ShouldHandle()
    {
        // Arrange
        var repo = new BooleanRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('Active1', 25, 1),
            ('Active2', 30, 1),
            ('Inactive1', 35, 0)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var active = await repo.GetByStatusAsync(true);
        var inactive = await repo.GetByStatusAsync(false);

        // Assert
        Assert.AreEqual(2, active.Count);
        Assert.AreEqual(1, inactive.Count);
    }

    [TestMethod]
    public async Task Boolean_UpdateStatus_ShouldToggle()
    {
        // Arrange
        var repo = new BooleanRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO edge_users (name, nullable_age, is_active) VALUES ('User', 25, 1)";
        cmd.ExecuteNonQuery();

        // Act - 停用
        var updated = await repo.UpdateStatusAsync(1, false);
        var activeCount = await repo.CountActiveUsersAsync();
        var inactiveCount = await repo.CountInactiveUsersAsync();

        // Assert
        Assert.AreEqual(1, updated);
        Assert.AreEqual(0, activeCount);
        Assert.AreEqual(1, inactiveCount);
    }

    // ==================== DateTime测试 ====================

    [TestMethod]
    public async Task DateTime_InsertAndQuery_ShouldPreserve()
    {
        // Arrange
        var repo = new DateTimeRepo(_connection);
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var userId = await repo.InsertWithTimestampAsync("User", 25, timestamp);

        // Assert
        Assert.IsTrue(userId > 0);

        var latest = await repo.GetLatestUserAsync();
        Assert.IsNotNull(latest);
        Assert.AreEqual("User", latest.Name);
    }

    [TestMethod]
    public async Task DateTime_RangeQuery_ShouldFilter()
    {
        // Arrange
        var repo = new DateTimeRepo(_connection);
        var start = new DateTime(2024, 1, 1);
        var middle = new DateTime(2024, 1, 15);
        var end = new DateTime(2024, 2, 1);

        await repo.InsertWithTimestampAsync("User1", 20, start);
        await repo.InsertWithTimestampAsync("User2", 25, middle);
        await repo.InsertWithTimestampAsync("User3", 30, end);

        // Act
        var inRange = await repo.GetUsersInRangeAsync(
            new DateTime(2024, 1, 10),
            new DateTime(2024, 1, 20));

        // Assert
        Assert.AreEqual(1, inRange.Count);
        Assert.AreEqual("User2", inRange[0].Name);
    }

    // ==================== 字符串模式匹配测试 ====================

    [TestMethod]
    public async Task PatternMatch_LikeOperator_ShouldFind()
    {
        // Arrange
        var repo = new PatternMatchRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('Alice', 25, 1),
            ('Bob', 30, 1),
            ('Charlie', 35, 1),
            ('David', 40, 1)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var withA = await repo.SearchByPatternAsync("A%");
        var withlie = await repo.SearchByPatternAsync("%lie");

        // Assert
        Assert.AreEqual(1, withA.Count);
        Assert.AreEqual("Alice", withA[0].Name);
        Assert.IsTrue(withlie.Any(u => u.Name == "Alice" || u.Name == "Charlie"));
    }

    [TestMethod]
    public async Task PatternMatch_CountByPrefix_ShouldCalculate()
    {
        // Arrange
        var repo = new PatternMatchRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('Test1', 25, 1),
            ('Test2', 30, 1),
            ('Other', 35, 1)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var count = await repo.CountByPrefixAsync("Test");

        // Assert
        Assert.AreEqual(2, count);
    }

    // ==================== 排序和分页测试 ====================

    [TestMethod]
    public async Task SortingPaging_GetPaged_ShouldReturnSubset()
    {
        // Arrange
        var repo = new SortingPagingRepo(_connection);

        // 插入10条记录
        using var cmd = _connection.CreateCommand();
        for (int i = 1; i <= 10; i++)
        {
            cmd.CommandText = $"INSERT INTO edge_users (name, nullable_age, is_active) VALUES ('User{i:D2}', {20 + i}, 1)";
            cmd.ExecuteNonQuery();
        }

        // Act
        var page1 = await repo.GetPagedAsync(3, 0);
        var page2 = await repo.GetPagedAsync(3, 3);

        // Assert
        Assert.AreEqual(3, page1.Count);
        Assert.AreEqual(3, page2.Count);
        Assert.AreNotEqual(page1[0].Id, page2[0].Id);
    }

    [TestMethod]
    public async Task SortingPaging_GetTopRecent_ShouldOrder()
    {
        // Arrange
        var repo = new SortingPagingRepo(_connection);
        var now = DateTime.UtcNow;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $@"
            INSERT INTO edge_users (name, nullable_age, created_at, is_active) VALUES
            ('Old', 25, '{now.AddDays(-2):yyyy-MM-dd HH:mm:ss}', 1),
            ('Recent', 30, '{now:yyyy-MM-dd HH:mm:ss}', 1),
            ('Middle', 35, '{now.AddDays(-1):yyyy-MM-dd HH:mm:ss}', 1)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var top2 = await repo.GetTopRecentAsync(2);

        // Assert
        Assert.AreEqual(2, top2.Count);
        Assert.AreEqual("Recent", top2[0].Name);
    }

    // ==================== CASE表达式测试 ====================

    [TestMethod]
    public async Task CaseExpression_AgeGroups_ShouldClassify()
    {
        // Arrange
        var repo = new CaseExpressionRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('Minor', 15, 1),
            ('Adult', 30, 1),
            ('Senior', 70, 1)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var groups = await repo.GetAgeGroupsAsync();

        // Assert
        Assert.AreEqual(3, groups.Count);
        Assert.IsTrue(groups.Any(g => g["age_group"].ToString() == "Minor"));
        Assert.IsTrue(groups.Any(g => g["age_group"].ToString() == "Adult"));
        Assert.IsTrue(groups.Any(g => g["age_group"].ToString() == "Senior"));
    }

    [TestMethod]
    public async Task CaseExpression_StatusDistribution_ShouldAggregate()
    {
        // Arrange
        var repo = new CaseExpressionRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('Active1', 25, 1),
            ('Active2', 30, 1),
            ('Inactive', 35, 0)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var distribution = await repo.GetStatusDistributionAsync();

        // Assert
        Assert.AreEqual(2, distribution.Count);
    }

    // ==================== DISTINCT测试 ====================

    [TestMethod]
    public async Task Distinct_GetUniqueAges_ShouldRemoveDuplicates()
    {
        // Arrange
        var repo = new DistinctRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('User1', 25, 1),
            ('User2', 25, 1),
            ('User3', 30, 1),
            ('User4', 30, 1),
            ('User5', 35, 1)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var distinctAges = await repo.GetDistinctAgesAsync();
        var count = await repo.CountDistinctAgesAsync();

        // Assert
        Assert.AreEqual(3, distinctAges.Count);
        Assert.AreEqual(3, count);

        var ages = distinctAges.Select(d => Convert.ToInt32(d["Age"])).ToList();
        CollectionAssert.AreEqual(new List<int> { 25, 30, 35 }, ages);
    }

    // ==================== 子查询测试 ====================

    [TestMethod]
    public async Task Subquery_AboveAverage_ShouldFilter()
    {
        // Arrange
        var repo = new SubqueryRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO edge_users (name, nullable_age, is_active) VALUES
            ('Young', 20, 1),
            ('Middle', 30, 1),
            ('Old', 40, 1)
        ";
        cmd.ExecuteNonQuery();

        // Act
        var aboveAverage = await repo.GetAboveAverageAgeAsync();

        // Assert
        Assert.IsTrue(aboveAverage.Count > 0);
        Assert.IsTrue(aboveAverage.All(u => u.NullableAge > 30)); // 平均值是30
    }

    [TestMethod]
    public async Task Subquery_RecentUsers_ShouldLimit()
    {
        // Arrange
        var repo = new SubqueryRepo(_connection);
        var now = DateTime.UtcNow;

        using var cmd = _connection.CreateCommand();
        for (int i = 1; i <= 5; i++)
        {
            cmd.CommandText = $@"
                INSERT INTO edge_users (name, nullable_age, created_at, is_active)
                VALUES ('User{i}', {20 + i}, '{now.AddDays(-i):yyyy-MM-dd HH:mm:ss}', 1)
            ";
            cmd.ExecuteNonQuery();
        }

        // Act
        var recent = await repo.GetRecentUsersAsync(3);

        // Assert
        Assert.AreEqual(3, recent.Count);
    }

    // ==================== 审计日志测试 ====================

    [TestMethod]
    public async Task AuditLog_Insert_ShouldRecord()
    {
        // Arrange
        var repo = new AuditLogRepo(_connection);
        var timestamp = DateTime.UtcNow;

        // Act
        var logId = await repo.LogAsync("User", "Create", timestamp, "User created successfully");

        // Assert
        Assert.IsTrue(logId > 0);

        var logs = await repo.GetRecentLogsAsync("User", 10);
        Assert.AreEqual(1, logs.Count);
        Assert.AreEqual("Create", logs[0].Action);
    }

    [TestMethod]
    public async Task AuditLog_GetSince_ShouldFilter()
    {
        // Arrange
        var repo = new AuditLogRepo(_connection);
        var now = DateTime.UtcNow;
        var past = now.AddDays(-1);

        await repo.LogAsync("User", "Old", past, null);
        await repo.LogAsync("User", "New", now, null);

        // Act
        var recentLogs = await repo.GetLogsSinceAsync(past.AddHours(1));

        // Assert
        Assert.AreEqual(1, recentLogs.Count);
        Assert.AreEqual("New", recentLogs[0].Action);
    }

    [TestMethod]
    public async Task AuditLog_Cleanup_ShouldDelete()
    {
        // Arrange
        var repo = new AuditLogRepo(_connection);
        var now = DateTime.UtcNow;
        var oldDate = now.AddDays(-30);

        await repo.LogAsync("User", "Old1", oldDate, null);
        await repo.LogAsync("User", "Old2", oldDate.AddDays(-1), null);
        await repo.LogAsync("User", "Recent", now, null);

        // Act
        var deleted = await repo.CleanupOldLogsAsync(now.AddDays(-7));

        // Assert
        Assert.AreEqual(2, deleted);

        var remaining = await repo.GetRecentLogsAsync("User", 100);
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual("Recent", remaining[0].Action);
    }

    // ==================== 组合场景测试 ====================

    [TestMethod]
    public async Task Combined_ComplexWorkflow_ShouldSucceed()
    {
        // Arrange
        var userRepo = new NullableTypeRepo(_connection);
        var auditRepo = new AuditLogRepo(_connection);
        var now = DateTime.UtcNow;

        // Act - 创建用户并记录审计
        var userId = await userRepo.InsertAsync("TestUser", 25, now, true);
        await auditRepo.LogAsync("User", "Create", now, $"Created user {userId}");

        // 查询用户
        var usersWithAge = await userRepo.GetUsersWithAgeAsync();

        // 记录查询审计
        await auditRepo.LogAsync("User", "Query", now, $"Queried {usersWithAge.Count} users");

        // Assert
        var logs = await auditRepo.GetRecentLogsAsync("User", 10);
        Assert.AreEqual(2, logs.Count);
        Assert.IsTrue(logs.Any(l => l.Action == "Create"));
        Assert.IsTrue(logs.Any(l => l.Action == "Query"));
    }
}

