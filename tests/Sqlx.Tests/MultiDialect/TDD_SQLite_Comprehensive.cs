// -----------------------------------------------------------------------
// <copyright file="TDD_SQLite_Comprehensive.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Collections.Generic;

namespace Sqlx.Tests.MultiDialect;

// ==================== SQLite仓储接口 ====================

public partial interface ISQLiteUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_sqlite (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive)")]
    [ReturnInsertedId]
    new Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE id = @id")]
    new Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite")]
    new Task<List<DialectUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_sqlite SET email = @email, balance = @balance WHERE id = @id")]
    new Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_sqlite WHERE id = @id")]
    new Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE username = @username")]
    new Task<DialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE age >= @minAge AND age <= @maxAge")]
    new Task<List<DialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE balance > @minBalance")]
    new Task<List<DialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE is_active = @isActive")]
    new Task<List<DialectUser>> GetByActiveStatusAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE last_login_at IS NULL")]
    new Task<List<DialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE last_login_at IS NOT NULL")]
    new Task<List<DialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_sqlite SET last_login_at = @lastLoginAt WHERE id = @id")]
    new Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_sqlite")]
    new Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_sqlite WHERE is_active = 1")]
    new Task<int> CountActiveAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM dialect_users_sqlite")]
    new Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM dialect_users_sqlite")]
    new Task<double> GetAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM dialect_users_sqlite")]
    new Task<int> GetMinAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM dialect_users_sqlite")]
    new Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite ORDER BY username ASC")]
    new Task<List<DialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite ORDER BY balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite ORDER BY age ASC, balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite ORDER BY id LIMIT @limit")]
    new Task<List<DialectUser>> GetTopUsersAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite ORDER BY id LIMIT @limit OFFSET @offset")]
    new Task<List<DialectUser>> GetUsersPaginatedAsync(int limit, int offset, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE username LIKE @pattern")]
    new Task<List<DialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE created_at BETWEEN @startDate AND @endDate")]
    new Task<List<DialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE age IN (18, 25, 30, 35)")]
    new Task<List<DialectUser>> GetUsersBySpecificAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT age, COUNT(*) as count FROM dialect_users_sqlite GROUP BY age")]
    new Task<List<Dictionary<string, object>>> GetUserCountByAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT DISTINCT age FROM dialect_users_sqlite ORDER BY age")]
    new Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE age > (SELECT AVG(age) FROM dialect_users_sqlite)")]
    new Task<List<DialectUser>> GetAboveAverageAgeUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlite WHERE LOWER(username) = LOWER(@username)")]
    new Task<DialectUser?> GetByCaseInsensitiveUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_sqlite WHERE is_active = 0")]
    new Task<int> DeleteInactiveUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_sqlite SET balance = balance * 1.05 WHERE is_active = 1")]
    new Task<int> ApplyBalanceBonusAsync(CancellationToken ct = default);
}

// ==================== SQLite仓储实现（使用源生成器） ====================

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISQLiteUserRepository))]
public partial class SQLiteUserRepository(DbConnection connection) : ISQLiteUserRepository
{
}

// ==================== SQLite测试类 ====================

/// <summary>
/// SQLite数据库全面功能测试
/// </summary>
[TestClass]
[TestCategory(TestCategories.SQLite)]
[TestCategory(TestCategories.Unit)]
public class TDD_SQLite_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "SQLite";
    protected override string TableName => "dialect_users_sqlite";

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
            CREATE TABLE dialect_users_sqlite (
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

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new SQLiteUserRepository(_connection!);
    }
}
