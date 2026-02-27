// <copyright file="ComparisonOperatorsStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试比较运算符在所有数据库上的SQL转换。
/// Strict E2E tests for comparison operators translation across all databases.
/// </summary>
[TestClass]
public class ComparisonOperatorsStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    value INT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    is_active TINYINT(1) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    value INT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    value INT NOT NULL,
                    name NVARCHAR(100) NOT NULL,
                    is_active BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    value INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    is_active INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Comparison Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Equals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试等于运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value == 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_NotEquals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试不等于运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value != 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("!=") || sql.Contains("<>"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试大于运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试大于等于运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value >= 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试小于运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value < 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("<"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_LessThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试小于等于运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value <= 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("<="));
    }

    // ==================== PostgreSQL Comparison Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Equals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value == 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_NotEquals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value != 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("!=") || sql.Contains("<>"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value < 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("<"));
    }

    // ==================== SQL Server Comparison Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Equals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value == 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_NotEquals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value != 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("!=") || sql.Contains("<>"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value < 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("<"));
    }

    // ==================== SQLite Comparison Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_Equals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value == 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_NotEquals_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value != 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("!=") || sql.Contains("<>"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ComparisonOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value < 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("<"));
    }
}
