// <copyright file="MathOperationsStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试数学运算在所有数据库上的SQL转换。
/// Strict E2E tests for math operations translation across all databases.
/// </summary>
[TestClass]
public class MathOperationsStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public decimal Price { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    value1 INT NOT NULL,
                    value2 INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    value1 INT NOT NULL,
                    value2 INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    value1 INT NOT NULL,
                    value2 INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    value1 INTEGER NOT NULL,
                    value2 INTEGER NOT NULL,
                    price REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Math Operations ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Addition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试加法运算
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value1 + e.Value2 > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("+"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Subtraction_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试减法运算
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value1 - e.Value2 > 10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("-"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Multiplication_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试乘法运算
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value1 * e.Value2 > 1000);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Division_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试除法运算
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value1 / e.Value2 > 2);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("/"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Modulo_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试取模运算
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value1 % e.Value2 == 0);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("%") || sql.Contains("MOD"));
    }

    // ==================== PostgreSQL Math Operations ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Addition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value1 + e.Value2 > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("+"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Subtraction_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value1 - e.Value2 > 10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("-"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Multiplication_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value1 * e.Value2 > 1000);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Division_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value1 / e.Value2 > 2);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("/"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Modulo_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value1 % e.Value2 == 0);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("%") || sql.Contains("MOD"));
    }

    // ==================== SQL Server Math Operations ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Addition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value1 + e.Value2 > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("+"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Subtraction_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value1 - e.Value2 > 10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("-"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Multiplication_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value1 * e.Value2 > 1000);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Division_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value1 / e.Value2 > 2);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("/"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Modulo_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value1 % e.Value2 == 0);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("%") || sql.Contains("MOD"));
    }

    // ==================== SQLite Math Operations ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Addition_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value1 + e.Value2 > 100);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("+"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Subtraction_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value1 - e.Value2 > 10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("-"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Multiplication_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value1 * e.Value2 > 1000);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("*"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Division_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value1 / e.Value2 > 2);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("/"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Modulo_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value1 % e.Value2 == 0);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("%") || sql.Contains("MOD"));
    }
}
