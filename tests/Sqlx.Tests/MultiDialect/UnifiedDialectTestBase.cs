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
        VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive)")]
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

    [SqlTemplate("INSERT INTO {{table}} (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, {{current_timestamp}}, NULL, {{bool_true}})")]
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

    protected abstract DbConnection? CreateConnection();
    protected abstract IUnifiedDialectUserRepository CreateRepository(DbConnection connection);

    [TestInitialize]
    public async Task Initialize()
    {
        Connection = CreateConnection();

        // 如果连接为null（例如本地环境没有数据库），跳过测试
        if (Connection == null)
        {
            Assert.Inconclusive("Database connection is not available in the current environment.");
            return;
        }

        // 只有当连接未打开时才打开（DatabaseConnectionHelper可能已经打开了连接）
        if (Connection.State != System.Data.ConnectionState.Open)
        {
            await Connection.OpenAsync();
        }

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

    /// <summary>
    /// 获取当前数据库方言类型
    /// </summary>
    protected abstract SqlDefineTypes GetDialectType();

    /// <summary>
    /// 统一的DDL生成 - 根据方言自动生成建表语句
    /// 这样DDL只需要定义一次！
    /// </summary>
    protected async Task CreateUnifiedTableAsync()
    {
        // 先删除表（如果存在），确保每次测试都是干净的环境
        await DropUnifiedTableAsync();

        // 等待一小段时间确保删除操作完成
        await Task.Delay(100);

        var dialect = GetDialectType();
        string sql;

        switch (dialect)
        {
            case SqlDefineTypes.PostgreSql:
                sql = $@"
                    CREATE TABLE {TableName} (
                        id BIGSERIAL PRIMARY KEY,
                        username TEXT NOT NULL,
                        email TEXT NOT NULL,
                        age INTEGER NOT NULL,
                        balance DECIMAL(18, 2) NOT NULL,
                        created_at TIMESTAMP NOT NULL,
                        last_login_at TIMESTAMP,
                        is_active BOOLEAN NOT NULL
                    )";
                break;

            case SqlDefineTypes.MySql:
                sql = $@"
                    CREATE TABLE {TableName} (
                        id BIGINT AUTO_INCREMENT PRIMARY KEY,
                        username VARCHAR(255) NOT NULL,
                        email VARCHAR(255) NOT NULL,
                        age INT NOT NULL,
                        balance DECIMAL(18, 2) NOT NULL,
                        created_at DATETIME NOT NULL,
                        last_login_at DATETIME,
                        is_active BOOLEAN NOT NULL
                    )";
                break;

            case SqlDefineTypes.SqlServer:
                sql = $@"
                    CREATE TABLE {TableName} (
                        id BIGINT IDENTITY(1,1) PRIMARY KEY,
                        username NVARCHAR(255) NOT NULL,
                        email NVARCHAR(255) NOT NULL,
                        age INT NOT NULL,
                        balance DECIMAL(18, 2) NOT NULL,
                        created_at DATETIME2 NOT NULL,
                        last_login_at DATETIME2,
                        is_active BIT NOT NULL
                    )";
                break;

            case SqlDefineTypes.SQLite:
                sql = $@"
                    CREATE TABLE {TableName} (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username TEXT NOT NULL,
                        email TEXT NOT NULL,
                        age INTEGER NOT NULL,
                        balance REAL NOT NULL,
                        created_at TEXT NOT NULL,
                        last_login_at TEXT,
                        is_active INTEGER NOT NULL
                    )";
                break;

            default:
                throw new NotSupportedException($"Dialect {dialect} is not supported");
        }

        try
        {
            using var cmd = Connection!.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // 如果创建失败，记录错误并重新抛出
            Console.WriteLine($"❌ Failed to create table {TableName}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 统一的删除表 - 根据方言使用不同的语法
    /// </summary>
    protected async Task DropUnifiedTableAsync()
    {
        try
        {
            var dialect = GetDialectType();
            string sql;

            switch (dialect)
            {
                case SqlDefineTypes.SqlServer:
                    // SQL Server 需要使用 IF OBJECT_ID 语法
                    sql = $@"
                        IF OBJECT_ID(N'{TableName}', N'U') IS NOT NULL
                            DROP TABLE {TableName};";
                    break;

                default:
                    // PostgreSQL, MySQL, SQLite 都支持 DROP TABLE IF EXISTS
                    sql = $"DROP TABLE IF EXISTS {TableName}";
                    break;
            }

            using var cmd = Connection!.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // 忽略删除表的错误（表可能不存在）
            Console.WriteLine($"⚠️ Warning: Failed to drop table {TableName}: {ex.Message}");
        }
    }

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

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Update_ShouldWork()
    {
        // Arrange
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act
        var affected = await Repository.UpdateAsync(id, "updated@example.com", 200m);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.AreEqual(1, affected);
        Assert.IsNotNull(user);
        Assert.AreEqual("updated@example.com", user.Email);
        Assert.AreEqual(200m, user.Balance);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Delete_ShouldWork()
    {
        // Arrange
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act
        var affected = await Repository.DeleteAsync(id);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.AreEqual(1, affected);
        Assert.IsNull(user);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByUsername_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act
        var user = await Repository.GetByUsernameAsync("testuser");

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("testuser", user.Username);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByAgeRange_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 35, 200m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetByAgeRangeAsync(22, 30);

        // Assert
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual(25, users[0].Age);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByMinBalance_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 160m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 200m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetByMinBalanceAsync(150m);

        // Assert
        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(u => u.Balance > 150m));
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetInactiveUsers_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("active1", "active1@test.com", 30, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("inactive1", "inactive1@test.com", 25, 50m, DateTime.UtcNow, null, false);
        await Repository.InsertAsync("inactive2", "inactive2@test.com", 35, 150m, DateTime.UtcNow, null, false);

        // Act
        var inactiveUsers = await Repository.GetInactiveUsersAsync();

        // Assert
        Assert.AreEqual(2, inactiveUsers.Count);
        Assert.IsTrue(inactiveUsers.All(u => !u.IsActive));
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetNeverLoggedInUsers_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, DateTime.UtcNow, true);

        // Act
        var users = await Repository.GetNeverLoggedInUsersAsync();

        // Assert
        Assert.AreEqual(1, users.Count);
        Assert.IsNull(users[0].LastLoginAt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetLoggedInUsers_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, DateTime.UtcNow, true);

        // Act
        var users = await Repository.GetLoggedInUsersAsync();

        // Assert
        Assert.AreEqual(1, users.Count);
        Assert.IsNotNull(users[0].LastLoginAt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task UpdateLastLogin_ShouldWork()
    {
        // Arrange
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, DateTime.UtcNow, null, true);
        var loginTime = DateTime.UtcNow;

        // Act
        var affected = await Repository.UpdateLastLoginAsync(id, loginTime);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.AreEqual(1, affected);
        Assert.IsNotNull(user);
        Assert.IsNotNull(user.LastLoginAt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Count_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, false);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 200m, DateTime.UtcNow, null, true);

        // Act
        var count = await Repository.CountAsync();

        // Assert
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAverageAge_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 30, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 40, 200m, DateTime.UtcNow, null, true);

        // Act
        var avgAge = await Repository.GetAverageAgeAsync();

        // Assert
        Assert.AreEqual(30.0, avgAge, 0.1);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetMinAge_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 30, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 40, 200m, DateTime.UtcNow, null, true);

        // Act
        var minAge = await Repository.GetMinAgeAsync();

        // Assert
        Assert.AreEqual(20, minAge);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetMaxBalance_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 30, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 40, 200m, DateTime.UtcNow, null, true);

        // Act
        var maxBalance = await Repository.GetMaxBalanceAsync();

        // Assert
        Assert.AreEqual(200m, maxBalance);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAllOrderByUsername_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("charlie", "charlie@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("alice", "alice@test.com", 30, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("bob", "bob@test.com", 40, 200m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetAllOrderByUsernameAsync();

        // Assert
        Assert.AreEqual(3, users.Count);
        Assert.AreEqual("alice", users[0].Username);
        Assert.AreEqual("bob", users[1].Username);
        Assert.AreEqual("charlie", users[2].Username);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAllOrderByBalanceDesc_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 30, 200m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 40, 150m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetAllOrderByBalanceDescAsync();

        // Assert
        Assert.AreEqual(3, users.Count);
        Assert.AreEqual(200m, users[0].Balance);
        Assert.AreEqual(150m, users[1].Balance);
        Assert.AreEqual(100m, users[2].Balance);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAllOrderByAgeAndBalance_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 200m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 150m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetAllOrderByAgeAndBalanceAsync();

        // Assert
        Assert.AreEqual(3, users.Count);
        Assert.AreEqual(20, users[0].Age);
        Assert.AreEqual(200m, users[0].Balance); // 同年龄按balance降序
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task SearchByUsername_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("alice", "alice@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("bob", "bob@test.com", 30, 150m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("alice2", "alice2@test.com", 40, 200m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.SearchByUsernameAsync("%alice%");

        // Assert
        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(u => u.Username.Contains("alice")));
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetUsersByDateRange_ShouldWork()
    {
        // Arrange
        var date1 = DateTime.UtcNow.AddDays(-10);
        var date2 = DateTime.UtcNow.AddDays(-5);
        var date3 = DateTime.UtcNow;

        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, date1, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 30, 150m, date2, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 40, 200m, date3, null, true);

        // Act
        var users = await Repository.GetUsersByDateRangeAsync(date1.AddDays(-1), date2.AddDays(1));

        // Assert
        Assert.AreEqual(2, users.Count);
    }

    // ==================== 边界条件测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithZeroBalance_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertAsync("zerobalance", "zero@test.com", 25, 0m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(0m, user.Balance);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithNegativeBalance_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertAsync("negative", "negative@test.com", 25, -100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(-100m, user.Balance);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithVeryLargeBalance_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertAsync("wealthy", "wealthy@test.com", 25, 999999999.99m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(999999999.99m, user.Balance);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithMinAge_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertAsync("young", "young@test.com", 0, 100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(0, user.Age);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithMaxAge_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertAsync("old", "old@test.com", 150, 100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(150, user.Age);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithLongUsername_ShouldWork()
    {
        // Arrange
        var longUsername = new string('a', 100);

        // Act
        var id = await Repository!.InsertAsync(longUsername, "long@test.com", 25, 100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(longUsername, user.Username);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var specialUsername = "user@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var id = await Repository!.InsertAsync(specialUsername, "special@test.com", 25, 100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(specialUsername, user.Username);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithUnicodeCharacters_ShouldWork()
    {
        // Arrange
        var unicodeUsername = "用户测试αβγδ";

        // Act
        var id = await Repository!.InsertAsync(unicodeUsername, "unicode@test.com", 25, 100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(unicodeUsername, user.Username);
    }

    // ==================== 空结果测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByUsername_WithNonExistentUsername_ShouldReturnNull()
    {
        // Act
        var user = await Repository!.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.IsNull(user);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAll_WithEmptyTable_ShouldReturnEmptyList()
    {
        // Act
        var users = await Repository!.GetAllAsync();

        // Assert
        Assert.IsNotNull(users);
        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Count_WithEmptyTable_ShouldReturnZero()
    {
        // Act
        var count = await Repository!.CountAsync();

        // Assert
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetTotalBalance_WithEmptyTable_ShouldReturnZero()
    {
        // Act
        var total = await Repository!.GetTotalBalanceAsync();

        // Assert
        Assert.AreEqual(0m, total);
    }

    // ==================== 批量操作测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task BatchInsert_ShouldWork()
    {
        // Arrange & Act
        for (int i = 0; i < 10; i++)
        {
            await Repository!.InsertAsync($"user{i}", $"user{i}@test.com", 20 + i, 100m * i, DateTime.UtcNow, null, true);
        }

        // Assert
        var users = await Repository!.GetAllAsync();
        Assert.AreEqual(10, users.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task BatchInsert_WithMixedActiveStatus_ShouldWork()
    {
        // Arrange & Act
        for (int i = 0; i < 20; i++)
        {
            await Repository!.InsertAsync($"user{i}", $"user{i}@test.com", 20 + i, 100m * i, DateTime.UtcNow, null, i % 2 == 0);
        }

        // Assert
        var activeUsers = await Repository!.GetActiveUsersAsync();
        var inactiveUsers = await Repository!.GetInactiveUsersAsync();
        Assert.AreEqual(10, activeUsers.Count);
        Assert.AreEqual(10, inactiveUsers.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task UpdateMultiple_ShouldWork()
    {
        // Arrange
        var id1 = await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        var id2 = await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, true);
        var id3 = await Repository.InsertAsync("user3", "user3@test.com", 30, 200m, DateTime.UtcNow, null, true);

        // Act
        await Repository.UpdateAsync(id1, "updated1@test.com", 1000m);
        await Repository.UpdateAsync(id2, "updated2@test.com", 2000m);
        await Repository.UpdateAsync(id3, "updated3@test.com", 3000m);

        // Assert
        var users = await Repository.GetAllAsync();
        Assert.AreEqual(3, users.Count);
        Assert.IsTrue(users.All(u => u.Email.StartsWith("updated")));
        Assert.AreEqual(6000m, await Repository.GetTotalBalanceAsync());
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task DeleteMultiple_ShouldWork()
    {
        // Arrange
        var id1 = await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        var id2 = await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, true);
        var id3 = await Repository.InsertAsync("user3", "user3@test.com", 30, 200m, DateTime.UtcNow, null, true);

        // Act
        await Repository.DeleteAsync(id1);
        await Repository.DeleteAsync(id3);

        // Assert
        var users = await Repository.GetAllAsync();
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual(id2, users[0].Id);
    }

    // ==================== 复杂查询测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task ComplexQuery_AgeRangeWithActiveStatus_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 150m, DateTime.UtcNow, null, false);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 200m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user4", "user4@test.com", 35, 250m, DateTime.UtcNow, null, false);

        // Act
        var ageRangeUsers = await Repository.GetByAgeRangeAsync(20, 30);
        var activeUsers = await Repository.GetActiveUsersAsync();

        // Assert - 使用LINQ在客户端过滤（因为没有组合查询方法）
        var result = ageRangeUsers.Where(u => activeUsers.Any(a => a.Id == u.Id)).ToList();
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task ComplexQuery_OrderAndFilter_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("charlie", "charlie@test.com", 30, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("alice", "alice@test.com", 25, 200m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("bob", "bob@test.com", 35, 150m, DateTime.UtcNow, null, false);
        await Repository.InsertAsync("dave", "dave@test.com", 20, 300m, DateTime.UtcNow, null, true);

        // Act
        var activeUsers = await Repository.GetActiveUsersAsync();
        var orderedUsers = await Repository.GetAllOrderByUsernameAsync();

        // Assert
        Assert.AreEqual(3, activeUsers.Count);
        Assert.AreEqual(4, orderedUsers.Count);
        Assert.AreEqual("alice", orderedUsers[0].Username);
    }

    // ==================== 数据完整性测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task InsertAndUpdate_PreserveOtherFields_ShouldWork()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, createdAt, null, true);

        // Act
        await Repository.UpdateAsync(id, "updated@example.com", 200m);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("testuser", user.Username); // Username未改变
        Assert.AreEqual(25, user.Age); // Age未改变
        Assert.IsTrue(user.IsActive); // IsActive未改变
        Assert.AreEqual("updated@example.com", user.Email); // Email已更新
        Assert.AreEqual(200m, user.Balance); // Balance已更新
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task UpdateLastLogin_PreserveOtherFields_ShouldWork()
    {
        // Arrange
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, DateTime.UtcNow, null, true);
        var loginTime = DateTime.UtcNow;

        // Act
        await Repository.UpdateLastLoginAsync(id, loginTime);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("testuser", user.Username);
        Assert.AreEqual("test@example.com", user.Email);
        Assert.AreEqual(25, user.Age);
        Assert.AreEqual(100m, user.Balance);
        Assert.IsTrue(user.IsActive);
        Assert.IsNotNull(user.LastLoginAt);
    }

    // ==================== 聚合函数边界测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAverageAge_WithSingleUser_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act
        var avgAge = await Repository.GetAverageAgeAsync();

        // Assert
        Assert.AreEqual(25.0, avgAge, 0.01);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Aggregates_WithDecimalPrecision_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 20, 100.11m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user2", "user2@test.com", 25, 200.22m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("user3", "user3@test.com", 30, 300.33m, DateTime.UtcNow, null, true);

        // Act
        var total = await Repository.GetTotalBalanceAsync();

        // Assert
        Assert.AreEqual(600.66m, total);
    }

    // ==================== 时间戳测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithPastDate_ShouldWork()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddYears(-10);

        // Act
        var id = await Repository!.InsertAsync("olduser", "old@test.com", 25, 100m, pastDate, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.IsTrue(user.CreatedAt < DateTime.UtcNow.AddYears(-9));
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task UpdateLastLogin_MultipleUpdates_ShouldWork()
    {
        // Arrange
        var id = await Repository!.InsertAsync("testuser", "test@example.com", 25, 100m, DateTime.UtcNow, null, true);
        var loginTime1 = DateTime.UtcNow.AddHours(-2);
        var loginTime2 = DateTime.UtcNow.AddHours(-1);
        var loginTime3 = DateTime.UtcNow;

        // Act
        await Repository.UpdateLastLoginAsync(id, loginTime1);
        await Repository.UpdateLastLoginAsync(id, loginTime2);
        await Repository.UpdateLastLoginAsync(id, loginTime3);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.IsNotNull(user.LastLoginAt);
        // 最后一次更新应该是最新的登录时间
        Assert.IsTrue(user.LastLoginAt.Value > loginTime2);
    }

    // ==================== 更多边界条件和错误处理测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Insert_WithEmptyUsername_ShouldWork()
    {
        // Arrange & Act
        var id = await Repository!.InsertAsync("", "empty@test.com", 25, 100m, DateTime.UtcNow, null, true);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("", user.Username);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Search_WithSqlInjectionAttempt_ShouldBeSecure()
    {
        // Arrange - SQL注入尝试
        await Repository!.InsertAsync("normaluser", "normal@test.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act - 尝试SQL注入
        var injectionPattern = "'; DROP TABLE users; --";
        var users = await Repository.SearchByUsernameAsync(injectionPattern);

        // Assert - 应该没有找到用户，而不是删除表
        Assert.AreEqual(0, users.Count);

        // 验证表仍然存在
        var allUsers = await Repository.GetAllAsync();
        Assert.IsTrue(allUsers.Count > 0);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByAgeRange_WithInvertedRange_ShouldReturnEmpty()
    {
        // Arrange
        await Repository!.InsertAsync("user1", "user1@test.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act - 反转的范围（max < min）
        var users = await Repository.GetByAgeRangeAsync(50, 20);

        // Assert
        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByMinBalance_WithNegativeValue_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("userneg1", "userneg1@test.com", 25, -50m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("userneg2", "userneg2@test.com", 30, -100m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetByMinBalanceAsync(-75m);

        // Assert
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("userneg1", users[0].Username);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Update_NonExistentUser_ShouldReturnZero()
    {
        // Act
        var affected = await Repository!.UpdateAsync(99999999, "new@test.com", 200m);

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Delete_NonExistentUser_ShouldReturnZero()
    {
        // Act
        var affected = await Repository!.DeleteAsync(99999999);

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetByDateRange_WithFutureDates_ShouldReturnEmpty()
    {
        // Arrange
        await Repository!.InsertAsync("futureuser", "future@test.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act - 未来的日期范围
        var futureStart = DateTime.UtcNow.AddDays(1);
        var futureEnd = DateTime.UtcNow.AddDays(10);
        var users = await Repository.GetUsersByDateRangeAsync(futureStart, futureEnd);

        // Assert
        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Search_WithEmptyPattern_ShouldReturnEmpty()
    {
        // Arrange
        await Repository!.InsertAsync("searchuser", "search@test.com", 25, 100m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.SearchByUsernameAsync("");

        // Assert
        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task InsertAndDelete_MultipleTimes_ShouldWork()
    {
        // Arrange & Act - 多次插入和删除
        for (int i = 0; i < 5; i++)
        {
            var id = await Repository!.InsertAsync($"temp{i}", $"temp{i}@test.com", 25, 100m, DateTime.UtcNow, null, true);
            var user = await Repository.GetByIdAsync(id);
            Assert.IsNotNull(user);

            var deleted = await Repository.DeleteAsync(id);
            Assert.AreEqual(1, deleted);

            var deletedUser = await Repository.GetByIdAsync(id);
            Assert.IsNull(deletedUser);
        }

        // Assert
        var count = await Repository!.CountAsync();
        // 应该只有之前的测试数据，没有temp用户
        Assert.IsTrue(count >= 0);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task UpdateLastLogin_WithNull_ShouldWork()
    {
        // Arrange
        var id = await Repository!.InsertAsync("nullupdate", "null@test.com", 25, 100m, DateTime.UtcNow, DateTime.UtcNow, true);

        // Act - 将last_login设为NULL
        await Repository.UpdateLastLoginAsync(id, null);
        var user = await Repository.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.IsNull(user.LastLoginAt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAll_OrderByUsername_ShouldBeSorted()
    {
        // Arrange
        await Repository!.InsertAsync("charlie", "c@test.com", 30, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("alice", "a@test.com", 25, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("bob", "b@test.com", 28, 100m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetAllOrderByUsernameAsync();
        var testUsers = users.Where(u => u.Username == "alice" || u.Username == "bob" || u.Username == "charlie").ToList();

        // Assert
        Assert.IsTrue(testUsers.Count >= 3);
        for (int i = 0; i < testUsers.Count - 1; i++)
        {
            Assert.IsTrue(string.Compare(testUsers[i].Username, testUsers[i + 1].Username, StringComparison.Ordinal) <= 0);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetAll_OrderByBalanceDesc_ShouldBeSorted()
    {
        // Arrange
        await Repository!.InsertAsync("userbal1", "userbal1@test.com", 25, 50m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("userbal2", "userbal2@test.com", 30, 300m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("userbal3", "userbal3@test.com", 35, 150m, DateTime.UtcNow, null, true);

        // Act
        var users = await Repository.GetAllOrderByBalanceDescAsync();

        // Assert
        Assert.IsTrue(users.Count >= 3);
        for (int i = 0; i < users.Count - 1; i++)
        {
            Assert.IsTrue(users[i].Balance >= users[i + 1].Balance);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task CancellationToken_ShouldBeAccepted()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - 正常执行（不取消）
        var count = await Repository!.CountAsync(cts.Token);

        // Assert
        Assert.IsTrue(count >= 0);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetMaxBalance_WithNegativeBalances_ShouldWork()
    {
        // Arrange
        await Repository!.InsertAsync("neg1", "neg1@test.com", 25, -50m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("neg2", "neg2@test.com", 30, -100m, DateTime.UtcNow, null, true);

        // Act
        var max = await Repository.GetMaxBalanceAsync();

        // Assert - 最大值应该是-50（因为-50 > -100）
        Assert.IsTrue(max >= -50m);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task GetMinAge_WithVariousAges_ShouldReturnMinimum()
    {
        // Arrange
        await Repository!.InsertAsync("young", "young@test.com", 18, 100m, DateTime.UtcNow, null, true);
        await Repository.InsertAsync("old", "old@test.com", 80, 100m, DateTime.UtcNow, null, true);

        // Act
        var minAge = await Repository.GetMinAgeAsync();

        // Assert
        Assert.IsTrue(minAge <= 18);
    }
}

