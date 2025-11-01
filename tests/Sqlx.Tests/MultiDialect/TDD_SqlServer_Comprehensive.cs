using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

// ==================== SQL Server仓储接口 ====================

public partial interface ISqlServerUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_sqlserver (username, email, age, balance, created_at, last_login_at, is_active) VALUES (@username, @email, @age, @balance, @createdAt, @lastLoginAt, @isActive); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    new Task<long> InsertAsync(string username, string email, int age, decimal balance, DateTime createdAt, DateTime? lastLoginAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE id = @id")]
    new Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver")]
    new Task<List<DialectUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_sqlserver SET email = @email, balance = @balance WHERE id = @id")]
    new Task<int> UpdateAsync(long id, string email, decimal balance, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_sqlserver WHERE id = @id")]
    new Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE username = @username")]
    new Task<DialectUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE age >= @minAge AND age <= @maxAge")]
    new Task<List<DialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE balance > @minBalance")]
    new Task<List<DialectUser>> GetByMinBalanceAsync(decimal minBalance, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE is_active = @isActive")]
    new Task<List<DialectUser>> GetByActiveStatusAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE last_login_at IS NULL")]
    new Task<List<DialectUser>> GetNeverLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE last_login_at IS NOT NULL")]
    new Task<List<DialectUser>> GetLoggedInUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_sqlserver SET last_login_at = @lastLoginAt WHERE id = @id")]
    new Task<int> UpdateLastLoginAsync(long id, DateTime? lastLoginAt, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_sqlserver")]
    new Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_sqlserver WHERE is_active = 1")]
    new Task<int> CountActiveAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM dialect_users_sqlserver")]
    new Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM dialect_users_sqlserver")]
    new Task<double> GetAverageAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM dialect_users_sqlserver")]
    new Task<int> GetMinAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM dialect_users_sqlserver")]
    new Task<decimal> GetMaxBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver ORDER BY username ASC")]
    new Task<List<DialectUser>> GetAllOrderByUsernameAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver ORDER BY balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByBalanceDescAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver ORDER BY age ASC, balance DESC")]
    new Task<List<DialectUser>> GetAllOrderByAgeAndBalanceAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT TOP (@limit) {{columns}} FROM dialect_users_sqlserver")]
    new Task<List<DialectUser>> GetTopUsersAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver ORDER BY id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY")]
    new Task<List<DialectUser>> GetUsersPaginatedAsync(int limit, int offset, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE username LIKE @pattern")]
    new Task<List<DialectUser>> SearchByUsernameAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE created_at BETWEEN @startDate AND @endDate")]
    new Task<List<DialectUser>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE age IN (25, 30, 35)")]
    new Task<List<DialectUser>> GetUsersBySpecificAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT age, COUNT(*) as count FROM dialect_users_sqlserver GROUP BY age ORDER BY age")]
    new Task<List<Dictionary<string, object>>> GetUserCountByAgeAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT DISTINCT age FROM dialect_users_sqlserver ORDER BY age")]
    new Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE age > (SELECT AVG(age) FROM dialect_users_sqlserver)")]
    new Task<List<DialectUser>> GetAboveAverageAgeUsersAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users_sqlserver WHERE LOWER(username) = LOWER(@username)")]
    new Task<DialectUser?> GetByCaseInsensitiveUsernameAsync(string username, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users_sqlserver WHERE is_active = 0")]
    new Task<int> DeleteInactiveUsersAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users_sqlserver SET balance = balance * 1.1 WHERE is_active = 1")]
    new Task<int> ApplyBalanceBonusAsync(CancellationToken ct = default);
}

/// <summary>
/// SQL Server综合功能测试 - 使用统一接口
/// </summary>
[TestClass]
[TestCategory(TestCategories.SqlServer)]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.CI)]
public class TDD_SqlServer_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "SQL Server";
    protected override string TableName => "dialect_users_sqlserver";

    protected override DbConnection CreateConnection()
    {
        // 检查是否在CI环境
        if (!DatabaseConnectionHelper.IsCI)
        {
            Assert.Inconclusive("SQL Server tests are only run in CI environment.");
        }

        var conn = DatabaseConnectionHelper.GetSqlServerConnection();
        if (conn == null)
        {
            Assert.Inconclusive("SQL Server connection not available. Microsoft.Data.SqlClient package not installed or connection failed.");
        }
        return conn!;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            IF OBJECT_ID('dialect_users_sqlserver', 'U') IS NOT NULL
                DROP TABLE dialect_users_sqlserver;

            CREATE TABLE dialect_users_sqlserver (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                username NVARCHAR(50) NOT NULL,
                email NVARCHAR(100),
                age INT NOT NULL,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                is_active BIT NOT NULL DEFAULT 1,
                created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
                last_login_at DATETIME2 NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new SqlServerUserRepository(_connection!);
    }
}

/// <summary>
/// SQL Server用户仓储
/// </summary>
[RepositoryFor(typeof(ISqlServerUserRepository))]
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class SqlServerUserRepository(DbConnection connection) : ISqlServerUserRepository
{
}
