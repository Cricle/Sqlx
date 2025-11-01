// -----------------------------------------------------------------------
// <copyright file="UnifiedDialectTests.cs" company="Cricle">
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
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

// ==================== 统一的实体定义 ====================

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

// ==================== 统一的仓储接口（写一次！） ====================

/// <summary>
/// 统一的仓储接口 - 一次定义，所有方言共用
/// SQL模板使用通用语法，源生成器会根据方言自动转换
/// </summary>
public partial interface IUnifiedDialectUserRepository
{
    // 注意：这些SQL模板是通用的，会根据[SqlDefine]自动适配各个方言
    // 例如：is_active的值，SQLite用0/1，PostgreSQL用true/false

    [SqlTemplate("INSERT INTO dialect_users (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE id = @id")]
    Task<UnifiedDialectUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users")]
    Task<List<UnifiedDialectUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users SET email = @email, balance = @balance WHERE id = @id")]
    Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE username = @username")]
    Task<UnifiedDialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE age >= @minAge AND age <= @maxAge")]
    Task<List<UnifiedDialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE balance > @minBalance")]
    Task<List<UnifiedDialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE is_active = @isActive")]
    Task<List<UnifiedDialectUser>> GetByActiveStatusAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE last_login_at IS NULL")]
    Task<List<UnifiedDialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE last_login_at IS NOT NULL")]
    Task<List<UnifiedDialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users SET last_login_at = @lastLoginAt WHERE id = @id")]
    Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users")]
    Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users WHERE is_active = @isActive")]
    Task<int> CountActiveAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM dialect_users")]
    Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM dialect_users")]
    Task<double> GetAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM dialect_users")]
    Task<int> GetMinAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM dialect_users")]
    Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users ORDER BY username ASC")]
    Task<List<UnifiedDialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users ORDER BY balance DESC")]
    Task<List<UnifiedDialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users ORDER BY age ASC, balance DESC")]
    Task<List<UnifiedDialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users ORDER BY id LIMIT @limit")]
    Task<List<UnifiedDialectUser>> GetTopUsersAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users ORDER BY id LIMIT @limit OFFSET @offset")]
    Task<List<UnifiedDialectUser>> GetUsersPaginatedAsync(int limit, int offset, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE username LIKE @pattern")]
    Task<List<UnifiedDialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE created_at BETWEEN @startDate AND @endDate")]
    Task<List<UnifiedDialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    [SqlTemplate("SELECT age, COUNT(*) as count FROM dialect_users GROUP BY age")]
    Task<List<Dictionary<string, object>>> GetUserCountByAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT DISTINCT age FROM dialect_users ORDER BY age")]
    Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE age > (SELECT AVG(age) FROM dialect_users)")]
    Task<List<UnifiedDialectUser>> GetAboveAverageAgeUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE LOWER(username) = LOWER(@username)")]
    Task<UnifiedDialectUser?> GetByCaseInsensitiveUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users WHERE is_active = @isActive")]
    Task<int> DeleteInactiveUsersAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users SET balance = balance * 1.05 WHERE is_active = @isActive")]
    Task<int> ApplyBalanceBonusAsync(bool isActive, CancellationToken ct = default);
}

// ==================== 各方言的实现类（只需指定方言类型！） ====================

// SQLite实现
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUnifiedDialectUserRepository))]
public partial class UnifiedSQLiteUserRepository(DbConnection connection) : IUnifiedDialectUserRepository
{
}

// PostgreSQL实现
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUnifiedDialectUserRepository))]
public partial class UnifiedPostgreSQLUserRepository(DbConnection connection) : IUnifiedDialectUserRepository
{
}

// MySQL实现
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IUnifiedDialectUserRepository))]
public partial class UnifiedMySQLUserRepository(DbConnection connection) : IUnifiedDialectUserRepository
{
}

// SQL Server实现
[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IUnifiedDialectUserRepository))]
public partial class UnifiedSqlServerUserRepository(DbConnection connection) : IUnifiedDialectUserRepository
{
}

// ==================== 统一的测试基类（写一次！） ====================

/// <summary>
/// 统一的测试基类 - 真正的写一次，所有方言都能运行
/// </summary>
public abstract class UnifiedDialectTestBase : IDisposable
{
    protected DbConnection? _connection;
    protected abstract string DialectName { get; }
    protected abstract string TableName { get; }

    protected abstract DbConnection CreateConnection();
    protected abstract void CreateTable();
    protected abstract IUnifiedDialectUserRepository CreateRepository();

    protected virtual long ExpectedFirstId => 1;

    [TestInitialize]
    public virtual void Initialize()
    {
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

    // ==================== 测试方法（写一次！所有方言都运行） ====================

    [TestMethod]
    public async Task Insert_ShouldReturnAutoIncrementId()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        var id = await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);

        Assert.IsTrue(id >= ExpectedFirstId);
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

    [TestMethod]
    public async Task Count_ShouldReturnCorrectCount()
    {
        var repo = CreateRepository();
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1", "user1@test.com", 25, 100m, now, null, true);
        await repo.InsertAsync("user2", "user2@test.com", 30, 200m, now, null, false);
        await repo.InsertAsync("user3", "user3@test.com", 35, 300m, now, null, true);

        var totalCount = await repo.CountAsync();
        var activeCount = await repo.CountActiveAsync(true);

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

    // ... 还有更多测试方法，但都只写一次！
}

// ==================== 各方言的测试类（只需3-4行配置！） ====================

[TestClass]
[TestCategory(TestCategories.SQLite)]
[TestCategory(TestCategories.Unit)]
public class UnifiedSQLiteTests : UnifiedDialectTestBase
{
    protected override string DialectName => "SQLite";
    protected override string TableName => "dialect_users";

    protected override DbConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE dialect_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                email TEXT NOT NULL,
                age INTEGER NOT NULL CHECK (age >= 0),
                balance DECIMAL(10,2) NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                last_login_at TEXT,
                is_active INTEGER NOT NULL DEFAULT 1
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IUnifiedDialectUserRepository CreateRepository()
    {
        return new UnifiedSQLiteUserRepository(_connection!);
    }
}

[TestClass]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.RequiresDatabase)]
public class UnifiedPostgreSQLTests : UnifiedDialectTestBase
{
    protected override string DialectName => "PostgreSQL";
    protected override string TableName => "dialect_users";

    protected override DbConnection CreateConnection()
    {
        var conn = DatabaseConnectionHelper.GetPostgreSQLConnection();
        if (conn == null)
            throw new InvalidOperationException("PostgreSQL connection not available.");
        return conn;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            DROP TABLE IF EXISTS dialect_users;

            CREATE TABLE dialect_users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(255) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL,
                age INTEGER NOT NULL CHECK (age >= 0),
                balance DECIMAL(10,2) NOT NULL DEFAULT 0,
                created_at TIMESTAMP NOT NULL,
                last_login_at TIMESTAMP,
                is_active BOOLEAN NOT NULL DEFAULT true
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IUnifiedDialectUserRepository CreateRepository()
    {
        return new UnifiedPostgreSQLUserRepository(_connection!);
    }
}

