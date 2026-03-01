// <copyright file="StringMethodsStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试字符串方法在所有数据库上的SQL转换。
/// Strict E2E tests for string method translation across all databases.
/// </summary>
[TestClass]
public class StringMethodsStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL String Methods ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("MySQL")]
    public async Task MySQL_StringContains_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Contains转换为LIKE
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Email.Contains("@example.com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("MySQL")]
    public async Task MySQL_StringStartsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试StartsWith转换为LIKE
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Name.StartsWith("John"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("MySQL")]
    public async Task MySQL_StringEndsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试EndsWith转换为LIKE
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Email.EndsWith(".com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("MySQL")]
    public async Task MySQL_StringToLower_TranslatesToLower()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试ToLower转换
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Name.ToLower() == "john");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LOWER") || sql.Contains("lower"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("MySQL")]
    public async Task MySQL_StringToUpper_TranslatesToUpper()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试ToUpper转换
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Name.ToUpper() == "JOHN");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("UPPER") || sql.Contains("upper"));
    }

    // ==================== PostgreSQL String Methods ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringContains_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Email.Contains("@example.com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like") || sql.Contains("ILIKE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringStartsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Name.StartsWith("John"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like") || sql.Contains("ILIKE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringEndsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Email.EndsWith(".com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like") || sql.Contains("ILIKE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringToLower_TranslatesToLower()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Name.ToLower() == "john");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LOWER") || sql.Contains("lower"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringToUpper_TranslatesToUpper()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Name.ToUpper() == "JOHN");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("UPPER") || sql.Contains("upper"));
    }

    // ==================== SQL Server String Methods ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringContains_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Email.Contains("@example.com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringStartsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Name.StartsWith("John"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringEndsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Email.EndsWith(".com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringToLower_TranslatesToLower()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Name.ToLower() == "john");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LOWER") || sql.Contains("lower"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringToUpper_TranslatesToUpper()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Name.ToUpper() == "JOHN");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("UPPER") || sql.Contains("upper"));
    }

    // ==================== SQLite String Methods ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SQLite")]
    public async Task SQLite_StringContains_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Email.Contains("@example.com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SQLite")]
    public async Task SQLite_StringStartsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Name.StartsWith("John"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SQLite")]
    public async Task SQLite_StringEndsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Email.EndsWith(".com"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SQLite")]
    public async Task SQLite_StringToLower_TranslatesToLower()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Name.ToLower() == "john");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LOWER") || sql.Contains("lower"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringMethods")]
    [TestCategory("SQLite")]
    public async Task SQLite_StringToUpper_TranslatesToUpper()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Name.ToUpper() == "JOHN");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("UPPER") || sql.Contains("upper"));
    }
}
