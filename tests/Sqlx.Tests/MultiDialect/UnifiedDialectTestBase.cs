// -----------------------------------------------------------------------
// <copyright file="UnifiedDialectTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.MultiDialect;

// ==================== 通用测试实体 ====================

public class UnifiedDialectUser
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

// ==================== 统一仓储接口（写一次，全部数据库可用）====================

/// <summary>
/// 统一的仓储接口，使用方言占位符。
/// 这个接口只需要定义一次，就可以在所有数据库上使用！
/// </summary>
public partial interface IUnifiedDialectUserRepository
{
    // ==================== CRUD操作 ====================

    [SqlTemplate(@"
        INSERT INTO {{table}} (username, email, age, balance, created_at, last_login_at, is_active)
        VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive)
        {{returning_id}}")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<UnifiedDialectUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
    Task<List<UnifiedDialectUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE {{table}} SET email = @email, balance = @balance WHERE id = @id")]
    Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken ct = default);

    // ==================== WHERE子句 ====================

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE username = @username")]
    Task<UnifiedDialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND age <= @maxAge")]
    Task<List<UnifiedDialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE balance > @minBalance")]
    Task<List<UnifiedDialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<UnifiedDialectUser>> GetActiveUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_false}}")]
    Task<List<UnifiedDialectUser>> GetInactiveUsersAsync(CancellationToken ct = default);

    // ==================== NULL处理 ====================

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE last_login_at IS NULL")]
    Task<List<UnifiedDialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE last_login_at IS NOT NULL")]
    Task<List<UnifiedDialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE {{table}} SET last_login_at = @lastLoginAt WHERE id = @id")]
    Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    // ==================== 聚合函数 ====================

    [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
    Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<int> CountActiveAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM {{table}}")]
    Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM {{table}}")]
    Task<double> GetAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM {{table}}")]
    Task<int> GetMinAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM {{table}}")]
    Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    // ==================== ORDER BY ====================

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY username ASC")]
    Task<List<UnifiedDialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY balance DESC")]
    Task<List<UnifiedDialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY age ASC, balance DESC")]
    Task<List<UnifiedDialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    // ==================== LIKE模式匹配 ====================

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE username LIKE @pattern")]
    Task<List<UnifiedDialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    // ==================== BETWEEN ====================

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE created_at BETWEEN @startDate AND @endDate")]
    Task<List<UnifiedDialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    // ==================== 时间戳 ====================

    [SqlTemplate("INSERT INTO {{table}} (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, {{current_timestamp}}, NULL, {{bool_true}}) {{returning_id}}")]
    [ReturnInsertedId]
    Task<long> InsertWithCurrentTimestampAsync(string username, string email, int age, decimal balance, CancellationToken ct = default);
}

// ==================== 方言特定实现类（只需要指定方言和表名）====================

/// <summary>
/// PostgreSQL实现 - 只需要指定方言和表名，SQL模板自动继承！
/// </summary>
[RepositoryFor(typeof(IUnifiedDialectUserRepository),
    Dialect = SqlDefineTypes.PostgreSql,
    TableName = "unified_dialect_users_pg")]
public partial class PostgreSQLUnifiedUserRepository : IUnifiedDialectUserRepository
{
    private readonly DbConnection _connection;

    public PostgreSQLUnifiedUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

/// <summary>
/// MySQL实现 - 只需要指定方言和表名，SQL模板自动继承！
/// </summary>
[RepositoryFor(typeof(IUnifiedDialectUserRepository),
    Dialect = SqlDefineTypes.MySql,
    TableName = "unified_dialect_users_my")]
public partial class MySQLUnifiedUserRepository : IUnifiedDialectUserRepository
{
    private readonly DbConnection _connection;

    public MySQLUnifiedUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

/// <summary>
/// SQLite实现 - 只需要指定方言和表名，SQL模板自动继承！
/// </summary>
[RepositoryFor(typeof(IUnifiedDialectUserRepository),
    Dialect = SqlDefineTypes.SQLite,
    TableName = "unified_dialect_users_sq")]
public partial class SQLiteUnifiedUserRepository : IUnifiedDialectUserRepository
{
    private readonly DbConnection _connection;

    public SQLiteUnifiedUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

/// <summary>
/// SQL Server实现 - 只需要指定方言和表名，SQL模板自动继承！
/// </summary>
[RepositoryFor(typeof(IUnifiedDialectUserRepository),
    Dialect = SqlDefineTypes.SqlServer,
    TableName = "unified_dialect_users_ss")]
public partial class SqlServerUnifiedUserRepository : IUnifiedDialectUserRepository
{
    private readonly DbConnection _connection;

    public SqlServerUnifiedUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

// ==================== 统一测试基类 ====================

/// <summary>
/// 统一的测试基类 - 测试代码也只需要写一次！
/// </summary>
public abstract class UnifiedDialectTestBase
{
    protected DbConnection? Connection;
    protected IUnifiedDialectUserRepository? Repository;
    protected abstract string TableName { get; }

    protected abstract DbConnection CreateConnection();
    protected abstract IUnifiedDialectUserRepository CreateRepository(DbConnection connection);

    [TestInitialize]
    public async Task Initialize()
    {
        Connection = CreateConnection();
        await Connection.OpenAsync();
        Repository = CreateRepository(Connection);

        // 创建表
        await CreateTableAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (Connection != null)
        {
            await DropTableAsync();
            await Connection.DisposeAsync();
        }
    }

    protected abstract Task CreateTableAsync();
    protected abstract Task DropTableAsync();

    // ==================== 统一的测试方法（写一次，全部数据库运行）====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task InsertAndGetById_ShouldWork()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100.50m, now, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("testuser", user.Username);
        Assert.AreEqual("test@example.com", user.Email);
        Assert.AreEqual(25, user.Age);
        Assert.AreEqual(100.50m, user.Balance);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetActiveUsers_WithBoolPlaceholder_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("active1", "active1@test.com", 30, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("inactive1", "inactive1@test.com", 25, 50m, DateTime.UtcNow, null, false);
        await Repository.InsertAsync("active2", "active2@test.com", 35, 150m, DateTime.UtcNow, null, true);

        // Act
        var activeUsers = await Repository.GetActiveUsersAsync();

        // Assert
        Assert.AreEqual(2, activeUsers.Count);
        Assert.IsTrue(activeUsers.All(u => u.IsActive));
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task InsertWithCurrentTimestamp_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertWithCurrentTimestampAsync("timestamp_user", "ts@test.com", 28, 200m);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("timestamp_user", user.Username);
        Assert.IsTrue(user.CreatedAt > DateTime.UtcNow.AddMinutes(-1)); // 创建时间在最近1分钟内
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task CountActive_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, false);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 200m, DateTime.UtcNow, null, true);

        // Act
        var count = await Repository.CountActiveAsync();

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetTotalBalance_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 250m, DateTime.UtcNow, null, true);

        // Act
        var total = await Repository.GetTotalBalanceAsync();

        // Assert
        Assert.AreEqual(500m, total);
    }
}

