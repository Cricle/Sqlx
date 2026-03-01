// <copyright file="DistinctStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试Distinct在所有数据库上的SQL转换。
/// Strict E2E tests for Distinct translation across all databases.
/// </summary>
[TestClass]
public class DistinctStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    category VARCHAR(50) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    category VARCHAR(50) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    category NVARCHAR(50) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    value INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Distinct Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("MySQL")]
    public async Task MySQL_Distinct_GeneratesDistinctKeyword()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Distinct
        var query = SqlQuery<TestEntity>.ForMySql()
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("MySQL")]
    public async Task MySQL_Distinct_WithSelect_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Distinct配合Select
        var query = SqlQuery<TestEntity>.ForMySql()
            .Select(e => new { e.Category })
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("MySQL")]
    public async Task MySQL_Distinct_WithWhere_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Distinct配合Where
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 50)
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
        Assert.IsTrue(sql.Contains("WHERE") || sql.Contains("where"));
    }

    // ==================== PostgreSQL Distinct Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Distinct_GeneratesDistinctKeyword()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Distinct_WithSelect_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Select(e => new { e.Category })
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Distinct_WithWhere_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value > 50)
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
        Assert.IsTrue(sql.Contains("WHERE") || sql.Contains("where"));
    }

    // ==================== SQL Server Distinct Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Distinct_GeneratesDistinctKeyword()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Distinct_WithSelect_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Select(e => new { e.Category })
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Distinct_WithWhere_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value > 50)
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
        Assert.IsTrue(sql.Contains("WHERE") || sql.Contains("where"));
    }

    // ==================== SQLite Distinct Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SQLite")]
    public async Task SQLite_Distinct_GeneratesDistinctKeyword()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SQLite")]
    public async Task SQLite_Distinct_WithSelect_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Category })
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SQLite")]
    public async Task SQLite_Distinct_WithWhere_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value > 50)
            .Distinct();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"));
        Assert.IsTrue(sql.Contains("WHERE") || sql.Contains("where"));
    }
}
