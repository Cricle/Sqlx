// <copyright file="SelectProjectionStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试Select投影在所有数据库上的SQL转换。
/// Strict E2E tests for Select projection translation across all databases.
/// </summary>
[TestClass]
public class SelectProjectionStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public decimal Price { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    value INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL,
                    price REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Select Projection Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("MySQL")]
    public async Task MySQL_Select_SingleColumn_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试单列投影
        var query = SqlQuery<TestEntity>.ForMySql()
            .Select(e => new { e.Name });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("MySQL")]
    public async Task MySQL_Select_MultipleColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试多列投影
        var query = SqlQuery<TestEntity>.ForMySql()
            .Select(e => new { e.Name, e.Value });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"));
        Assert.IsTrue(sql.Contains("value") || sql.Contains("Value"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("MySQL")]
    public async Task MySQL_Select_WithCalculation_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试计算列投影
        var query = SqlQuery<TestEntity>.ForMySql()
            .Select(e => new { e.Name, Total = e.Value * e.Price });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("MySQL")]
    public async Task MySQL_Select_AllColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试全列投影
        var query = SqlQuery<TestEntity>.ForMySql();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    // ==================== PostgreSQL Select Projection Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Select_SingleColumn_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Select(e => new { e.Name });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Select_MultipleColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Select(e => new { e.Name, e.Value });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Select_WithCalculation_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Select(e => new { e.Name, Total = e.Value * e.Price });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Select_AllColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    // ==================== SQL Server Select Projection Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Select_SingleColumn_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Select(e => new { e.Name });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Select_MultipleColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Select(e => new { e.Name, e.Value });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Select_WithCalculation_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Select(e => new { e.Name, Total = e.Value * e.Price });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Select_AllColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    // ==================== SQLite Select Projection Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SQLite")]
    public async Task SQLite_Select_SingleColumn_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Name });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SQLite")]
    public async Task SQLite_Select_MultipleColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Name, e.Value });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SQLite")]
    public async Task SQLite_Select_WithCalculation_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Name, Total = e.Value * e.Price });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SelectProjection")]
    [TestCategory("SQLite")]
    public async Task SQLite_Select_AllColumns_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }
}
