using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

// ==================== MySQL仓储接口 ====================

public partial interface IMySQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_mysql (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive)")]
    [ReturnInsertedId]
    new Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE id = @id")]
    new Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql")]
    new Task<List<DialectUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_mysql SET email = @email, balance = @balance WHERE id = @id")]
    new Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_mysql WHERE id = @id")]
    new Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE username = @username")]
    new Task<DialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE age >= @minAge AND age <= @maxAge")]
    new Task<List<DialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE balance > @minBalance")]
    new Task<List<DialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE is_active = @isActive")]
    new Task<List<DialectUser>> GetByActiveStatusAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE last_login_at IS NULL")]
    new Task<List<DialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE last_login_at IS NOT NULL")]
    new Task<List<DialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_mysql SET last_login_at = @lastLoginAt WHERE id = @id")]
    new Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_mysql")]
    new Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_mysql WHERE is_active = 1")]
    new Task<int> CountActiveAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM dialect_users_mysql")]
    new Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM dialect_users_mysql")]
    new Task<double> GetAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM dialect_users_mysql")]
    new Task<int> GetMinAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM dialect_users_mysql")]
    new Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql ORDER BY username ASC")]
    new Task<List<DialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql ORDER BY balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql ORDER BY age ASC, balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql LIMIT @limit")]
    new Task<List<DialectUser>> GetTopUsersAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql LIMIT @limit OFFSET @offset")]
    new Task<List<DialectUser>> GetUsersPaginatedAsync(int limit, int offset, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE username LIKE @pattern")]
    new Task<List<DialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE created_at BETWEEN @startDate AND @endDate")]
    new Task<List<DialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE age IN (25, 30, 35)")]
    new Task<List<DialectUser>> GetUsersBySpecificAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT age, COUNT(*) as count FROM dialect_users_mysql GROUP BY age ORDER BY age")]
    new Task<List<Dictionary<string, object>>> GetUserCountByAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT DISTINCT age FROM dialect_users_mysql ORDER BY age")]
    new Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE age > (SELECT AVG(age) FROM dialect_users_mysql)")]
    new Task<List<DialectUser>> GetAboveAverageAgeUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_mysql WHERE LOWER(username) = LOWER(@username)")]
    new Task<DialectUser?> GetByCaseInsensitiveUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_mysql WHERE is_active = 0")]
    new Task<int> DeleteInactiveUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_mysql SET balance = balance * 1.1 WHERE is_active = 1")]
    new Task<int> ApplyBalanceBonusAsync(CancellationToken ct = default);
}

/// <summary>
/// MySQL综合功能测试 - 使用统一接口
/// </summary>
[TestClass]
[TestCategory(TestCategories.MySQL)]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.CI)]
public class TDD_MySQL_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "MySQL";
    protected override string TableName => "dialect_users_mysql";

    protected override DbConnection CreateConnection()
    {
        // 检查是否在CI环境
        if (!DatabaseConnectionHelper.IsCI)
        {
            Assert.Inconclusive("MySQL tests are only run in CI environment.");
        }

        var conn = DatabaseConnectionHelper.GetMySQLConnection();
        if (conn == null)
        {
            Assert.Inconclusive("MySQL connection not available. MySqlConnector package not installed or connection failed.");
        }
        return conn!;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            DROP TABLE IF EXISTS dialect_users_mysql;

            CREATE TABLE dialect_users_mysql (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                username VARCHAR(50) NOT NULL,
                email VARCHAR(100),
                age INT NOT NULL,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_login_at TIMESTAMP NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new MySQLUserRepository(_connection!);
    }
}

/// <summary>
/// MySQL用户仓储
/// </summary>
[RepositoryFor(typeof(IMySQLUserRepository))]
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySQLUserRepository(DbConnection connection) : IMySQLUserRepository
{
}

