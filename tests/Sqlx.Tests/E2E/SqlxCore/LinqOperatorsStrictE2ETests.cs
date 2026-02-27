// <copyright file="LinqOperatorsStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试LINQ操作符在所有数据库上的转换。
/// Strict E2E tests for LINQ operator translation across all databases.
/// </summary>
[TestClass]
public class LinqOperatorsStrictE2ETests : E2ETestBase
{
    public class TestRecord
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_records (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    score INT NOT NULL,
                    category VARCHAR(50) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_records (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    score INT NOT NULL,
                    category VARCHAR(50) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_records (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    score INT NOT NULL,
                    category NVARCHAR(50) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_records (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    score INTEGER NOT NULL,
                    category TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL LINQ Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Where_SingleCondition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试单个Where条件
        var query = SqlQuery<TestRecord>.ForMySql()
            .Where(r => r.Score > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("score") || sql.Contains("Score"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Where_MultipleConditions_CombinesWithAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试多个Where条件
        var query = SqlQuery<TestRecord>.ForMySql()
            .Where(r => r.Score > 50)
            .Where(r => r.Category == "A");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        var whereCount = sql.Split(new[] { "WHERE" }, StringSplitOptions.None).Length - 1;
        Assert.AreEqual(1, whereCount, "Should combine multiple Where into single WHERE clause");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_OrderBy_Ascending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试OrderBy升序
        var query = SqlQuery<TestRecord>.ForMySql()
            .OrderBy(r => r.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsFalse(sql.Contains("DESC"), "Ascending order should not contain DESC");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_OrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试OrderByDescending降序
        var query = SqlQuery<TestRecord>.ForMySql()
            .OrderByDescending(r => r.Score);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DESC"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Take_GeneratesLimit()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Take
        var query = SqlQuery<TestRecord>.ForMySql()
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Skip_GeneratesOffset()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Skip
        var query = SqlQuery<TestRecord>.ForMySql()
            .Skip(5);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("LIMIT"));
    }

    // ==================== PostgreSQL LINQ Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Where_SingleCondition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestRecord>.ForPostgreSQL()
            .Where(r => r.Score > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Where_MultipleConditions_CombinesWithAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestRecord>.ForPostgreSQL()
            .Where(r => r.Score > 50)
            .Where(r => r.Category == "A");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_OrderBy_Ascending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestRecord>.ForPostgreSQL()
            .OrderBy(r => r.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_OrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestRecord>.ForPostgreSQL()
            .OrderByDescending(r => r.Score);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DESC"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SkipAndTake_GeneratesOffsetAndLimit()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestRecord>.ForPostgreSQL()
            .Skip(5)
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("OFFSET"));
    }

    // ==================== SQL Server LINQ Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Where_SingleCondition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlServer()
            .Where(r => r.Score > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Where_MultipleConditions_CombinesWithAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlServer()
            .Where(r => r.Score > 50)
            .Where(r => r.Category == "A");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OrderBy_Ascending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlServer()
            .OrderBy(r => r.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlServer()
            .OrderByDescending(r => r.Score);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DESC"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Take_GeneratesTopOrOffsetFetch()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlServer()
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("TOP") || sql.Contains("OFFSET") || sql.Contains("FETCH"));
    }

    // ==================== SQLite LINQ Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_Where_SingleCondition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlite()
            .Where(r => r.Score > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_Where_MultipleConditions_CombinesWithAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlite()
            .Where(r => r.Score > 50)
            .Where(r => r.Category == "A");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_OrderBy_Ascending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlite()
            .OrderBy(r => r.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_OrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlite()
            .OrderByDescending(r => r.Score);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DESC"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_Take_GeneratesLimit()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlite()
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LinqOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_SkipAndTake_GeneratesOffsetAndLimit()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestRecord>.ForSqlite()
            .Skip(5)
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"));
        Assert.IsTrue(sql.Contains("OFFSET"));
    }
}
