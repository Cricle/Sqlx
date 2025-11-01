// -----------------------------------------------------------------------
// <copyright file="TDD_PostgreSQL_Comprehensive.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Collections.Generic;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

// ==================== PostgreSQL仓储接口 ====================

public partial interface IPostgreSQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_postgresql (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive) RETURNING id")]
    [ReturnInsertedId]
    new Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE id = @id")]
    new Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql")]
    new Task<List<DialectUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_postgresql SET email = @email, balance = @balance WHERE id = @id")]
    new Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_postgresql WHERE id = @id")]
    new Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE username = @username")]
    new Task<DialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE age >= @minAge AND age <= @maxAge")]
    new Task<List<DialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE balance > @minBalance")]
    new Task<List<DialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE is_active = @isActive")]
    new Task<List<DialectUser>> GetByActiveStatusAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE last_login_at IS NULL")]
    new Task<List<DialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE last_login_at IS NOT NULL")]
    new Task<List<DialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_postgresql SET last_login_at = @lastLoginAt WHERE id = @id")]
    new Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_postgresql")]
    new Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_postgresql WHERE is_active = true")]
    new Task<int> CountActiveAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM dialect_users_postgresql")]
    new Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM dialect_users_postgresql")]
    new Task<double> GetAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM dialect_users_postgresql")]
    new Task<int> GetMinAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM dialect_users_postgresql")]
    new Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql ORDER BY username ASC")]
    new Task<List<DialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql ORDER BY balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql ORDER BY age ASC, balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql ORDER BY id LIMIT @limit")]
    new Task<List<DialectUser>> GetTopUsersAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql ORDER BY id LIMIT @limit OFFSET @offset")]
    new Task<List<DialectUser>> GetUsersPaginatedAsync(int limit, int offset, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE username LIKE @pattern")]
    new Task<List<DialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE created_at BETWEEN @startDate AND @endDate")]
    new Task<List<DialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE age IN (18, 25, 30, 35)")]
    new Task<List<DialectUser>> GetUsersBySpecificAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT age, COUNT(*) as count FROM dialect_users_postgresql GROUP BY age")]
    new Task<List<Dictionary<string, object>>> GetUserCountByAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT DISTINCT age FROM dialect_users_postgresql ORDER BY age")]
    new Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE age > (SELECT AVG(age) FROM dialect_users_postgresql)")]
    new Task<List<DialectUser>> GetAboveAverageAgeUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE LOWER(username) = LOWER(@username)")]
    new Task<DialectUser?> GetByCaseInsensitiveUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_postgresql WHERE is_active = false")]
    new Task<int> DeleteInactiveUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_postgresql SET balance = balance * 1.05 WHERE is_active = true")]
    new Task<int> ApplyBalanceBonusAsync(CancellationToken ct = default);
}

// ==================== PostgreSQL仓储实现（使用源生成器） ====================

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IPostgreSQLUserRepository))]
public partial class PostgreSQLUserRepository(DbConnection connection) : IPostgreSQLUserRepository
{
}

// ==================== PostgreSQL测试类 ====================

/// <summary>
/// PostgreSQL数据库全面功能测试 - 仅在CI环境运行
/// </summary>
[TestClass]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.RequiresDatabase)]
public class TDD_PostgreSQL_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "PostgreSQL";
    protected override string TableName => "dialect_users_postgresql";

    protected override DbConnection CreateConnection()
    {
        var conn = DatabaseConnectionHelper.GetPostgreSQLConnection();
        if (conn == null)
            throw new InvalidOperationException("PostgreSQL connection not available. Ensure you are running in CI environment with proper database setup.");
        return conn;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            DROP TABLE IF EXISTS dialect_users_postgresql;

            CREATE TABLE dialect_users_postgresql (
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

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new PostgreSQLUserRepository(_connection!);
    }
}

