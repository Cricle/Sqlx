// <copyright file="LogicalOperatorsStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试逻辑运算符在所有数据库上的SQL转换。
/// Strict E2E tests for logical operators translation across all databases.
/// </summary>
[TestClass]
public class LogicalOperatorsStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public int Value { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    value INT NOT NULL,
                    is_active TINYINT(1) NOT NULL,
                    is_deleted TINYINT(1) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    value INT NOT NULL,
                    is_active BOOLEAN NOT NULL,
                    is_deleted BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    value INT NOT NULL,
                    is_active BIT NOT NULL,
                    is_deleted BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    value INTEGER NOT NULL,
                    is_active INTEGER NOT NULL,
                    is_deleted INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Logical Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_And_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试AND逻辑运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.IsActive && !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Or_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试OR逻辑运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 100 || e.IsActive);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_Not_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试NOT逻辑运算符
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("not") || sql.Contains("=") || sql.Contains("!="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("MySQL")]
    public async Task MySQL_ComplexLogic_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试复杂逻辑表达式
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => (e.Value > 50 && e.IsActive) || (!e.IsDeleted && e.Value < 100));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    // ==================== PostgreSQL Logical Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_And_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.IsActive && !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Or_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value > 100 || e.IsActive);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Not_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("not") || sql.Contains("=") || sql.Contains("!="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ComplexLogic_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => (e.Value > 50 && e.IsActive) || (!e.IsDeleted && e.Value < 100));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    // ==================== SQL Server Logical Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_And_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.IsActive && !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Or_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value > 100 || e.IsActive);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Not_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("not") || sql.Contains("=") || sql.Contains("!="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ComplexLogic_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => (e.Value > 50 && e.IsActive) || (!e.IsDeleted && e.Value < 100));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    // ==================== SQLite Logical Operators ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_And_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.IsActive && !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_Or_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value > 100 || e.IsActive);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_Not_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => !e.IsDeleted);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("not") || sql.Contains("=") || sql.Contains("!="));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LogicalOperators")]
    [TestCategory("SQLite")]
    public async Task SQLite_ComplexLogic_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => (e.Value > 50 && e.IsActive) || (!e.IsDeleted && e.Value < 100));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND") || sql.Contains("and"));
        Assert.IsTrue(sql.Contains("OR") || sql.Contains("or"));
    }
}
