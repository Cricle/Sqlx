// <copyright file="NullHandlingStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试NULL处理在所有数据库上的SQL转换。
/// Strict E2E tests for NULL handling translation across all databases.
/// </summary>
[TestClass]
public class NullHandlingStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public int? Value { get; set; }
        public string? Description { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NULL,
                    value INT NULL,
                    description TEXT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NULL,
                    value INT NULL,
                    description TEXT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NULL,
                    value INT NULL,
                    description NVARCHAR(MAX) NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NULL,
                    value INTEGER NULL,
                    description TEXT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL NULL Handling ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_IsNull_GeneratesIsNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试IS NULL检查
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Name == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_IsNotNull_GeneratesIsNotNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试IS NOT NULL检查
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Name != null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NOT NULL") || sql.Contains("is not null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_NullableValue_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试可空值类型
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_NullableValueComparison_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试可空值比较
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    // ==================== PostgreSQL NULL Handling ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_IsNull_GeneratesIsNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Name == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_IsNotNull_GeneratesIsNotNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Name != null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NOT NULL") || sql.Contains("is not null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_NullableValue_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_NullableValueComparison_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    // ==================== SQL Server NULL Handling ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_IsNull_GeneratesIsNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Name == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_IsNotNull_GeneratesIsNotNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Name != null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NOT NULL") || sql.Contains("is not null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_NullableValue_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_NullableValueComparison_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }

    // ==================== SQLite NULL Handling ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_IsNull_GeneratesIsNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Name == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_IsNotNull_GeneratesIsNotNullCheck()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Name != null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NOT NULL") || sql.Contains("is not null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_NullableValue_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value == null);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_NullableValueComparison_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains(">"));
    }
}
