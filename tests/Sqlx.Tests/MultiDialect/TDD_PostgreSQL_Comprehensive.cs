// -----------------------------------------------------------------------
// <copyright file="TDD_PostgreSQL_Comprehensive.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// PostgreSQL 综合功能测试
/// 注意：这些测试需要真实的PostgreSQL数据库连接，目前暂时跳过
/// </summary>
[TestClass]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.CI)]
public class TDD_PostgreSQL_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "PostgreSQL";
    protected override string TableName => "dialect_users_postgresql";

    protected override DbConnection CreateConnection()
    {
        // 检查是否在CI环境
        if (!DatabaseConnectionHelper.IsCI)
        {
            Assert.Inconclusive("PostgreSQL tests are only run in CI environment.");
        }

        var conn = DatabaseConnectionHelper.GetPostgreSQLConnection();
        if (conn == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available. Npgsql package not installed or connection failed.");
        }
        return conn!;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            DROP TABLE IF EXISTS dialect_users_postgresql;

            CREATE TABLE dialect_users_postgresql (
                id SERIAL PRIMARY KEY,
                username VARCHAR(50) NOT NULL,
                email VARCHAR(100),
                age INT NOT NULL,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new PostgreSQLUserRepository(_connection!);
    }
}

/// <summary>
/// PostgreSQL用户仓储
/// </summary>
[RepositoryFor(typeof(IPostgreSQLUserRepository))]
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class PostgreSQLUserRepository(DbConnection connection) : IPostgreSQLUserRepository
{
}

/// <summary>
/// PostgreSQL用户仓储接口
/// </summary>
public interface IPostgreSQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_postgresql (username, email, age, balance, is_active) VALUES (@username, @email, @age, @balance, @isActive) RETURNING id")]
    Task<long> InsertAsync(string username, string? email, int age, decimal balance, bool isActive, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE id = @id")]
    Task<DialectUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql")]
    Task<List<DialectUser>> GetAllAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("UPDATE dialect_users_postgresql SET username = @username, email = @email, age = @age, balance = @balance, is_active = @isActive WHERE id = @id")]
    Task<int> UpdateAsync(long id, string username, string? email, int age, decimal balance, bool isActive, CancellationToken cancellationToken = default);

    [SqlTemplate("DELETE FROM dialect_users_postgresql WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users_postgresql")]
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE age BETWEEN @minAge AND @maxAge")]
    Task<List<DialectUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE username = @username")]
    Task<DialectUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM dialect_users_postgresql")]
    Task<decimal> GetTotalBalanceAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COALESCE(AVG(age), 0) FROM dialect_users_postgresql")]
    Task<double> GetAverageAgeAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COALESCE(MIN(age), 0) FROM dialect_users_postgresql")]
    Task<int> GetMinAgeAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COALESCE(MAX(balance), 0) FROM dialect_users_postgresql")]
    Task<decimal> GetMaxBalanceAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql ORDER BY age ASC")]
    Task<List<DialectUser>> GetOrderedByAgeAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql LIMIT @limit")]
    Task<List<DialectUser>> GetTopNAsync(int limit, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql LIMIT @limit OFFSET @offset")]
    Task<List<DialectUser>> GetPagedAsync(int limit, int offset, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE email IS NULL")]
    Task<List<DialectUser>> GetUsersWithoutEmailAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT DISTINCT age FROM dialect_users_postgresql ORDER BY age")]
    Task<List<Dictionary<string, object>>> GetDistinctAgesAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE id IN (SELECT id FROM dialect_users_postgresql WHERE balance > 100)")]
    Task<List<DialectUser>> GetRichUsersAsync(CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE LOWER(username) = LOWER(@username)")]
    Task<DialectUser?> GetByUsernameCaseInsensitiveAsync(string username, CancellationToken cancellationToken = default);

    [SqlTemplate("DELETE FROM dialect_users_postgresql WHERE id IN (@id1, @id2)")]
    Task<int> BatchDeleteAsync(long id1, long id2, CancellationToken cancellationToken = default);

    [SqlTemplate("UPDATE dialect_users_postgresql SET balance = balance + @amount WHERE id IN (@id1, @id2)")]
    Task<int> BatchUpdateBalanceAsync(long id1, long id2, decimal amount, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, username, email, age, balance, is_active as isActive, created_at as createdAt FROM dialect_users_postgresql WHERE username LIKE @pattern")]
    Task<List<DialectUser>> SearchByUsernameAsync(string pattern, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT age, COUNT(*) as count FROM dialect_users_postgresql GROUP BY age ORDER BY age")]
    Task<List<Dictionary<string, object>>> GetAgeDistributionAsync(CancellationToken cancellationToken = default);
}

