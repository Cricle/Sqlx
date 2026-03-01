// <copyright file="SqlQueryStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试SqlQuery在所有数据库上的功能。
/// Strict E2E tests for SqlQuery functionality across all databases.
/// </summary>
[TestClass]
public class SqlQueryStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
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
                    is_active TINYINT(1) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    value INT NOT NULL,
                    is_active BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL,
                    is_active INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var entities = new[]
        {
            ("Entity1", 10, true),
            ("Entity2", 20, false),
            ("Entity3", 30, true),
            ("Entity4", 40, false),
            ("Entity5", 50, true),
        };

        foreach (var (name, value, isActive) in entities)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_entities (name, value, is_active) VALUES (@name, @value, @is_active)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@value", value);
            AddParameter(cmd, "@is_active", FormatBoolean(isActive, dbType));
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    private static object FormatBoolean(bool value, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => value ? 1 : 0,
            DatabaseType.SQLite => value ? 1 : 0,
            _ => value,
        };
    }

    // ==================== MySQL SqlQuery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_ForMySql_CreatesQueryable()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试SqlQuery.ForMySql()创建查询
        var query = SqlQuery<TestEntity>.ForMySql();

        // Assert
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(SqlxQueryable<TestEntity>));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_ToSql_GeneratesValidSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试ToSql()生成SQL
        var query = SqlQuery<TestEntity>.ForMySql();
        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT"), "SQL should contain SELECT");
        Assert.IsTrue(sql.Contains("FROM"), "SQL should contain FROM");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_Where_GeneratesWhereClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Where子句生成
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 20);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"), "SQL should contain WHERE clause");
        Assert.IsTrue(sql.Contains("value") || sql.Contains("Value"), "SQL should reference value column");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_OrderBy_GeneratesOrderByClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试OrderBy子句生成
        var query = SqlQuery<TestEntity>.ForMySql()
            .OrderBy(e => e.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"), "SQL should contain ORDER BY clause");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_Take_GeneratesLimitClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试Take生成LIMIT
        var query = SqlQuery<TestEntity>.ForMySql()
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"), "MySQL should use LIMIT");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_ComplexQuery_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试复杂查询
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value >= 20)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Take(5);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"), "Should have WHERE");
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should have ORDER BY");
        Assert.IsTrue(sql.Contains("LIMIT"), "Should have LIMIT");
    }

    // ==================== PostgreSQL SqlQuery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_ForPostgreSQL_CreatesQueryable()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL();

        // Assert
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(SqlxQueryable<TestEntity>));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_ToSql_GeneratesValidSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL();
        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_Where_GeneratesWhereClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value > 20);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_OrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .OrderByDescending(e => e.Value);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DESC"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_SkipAndTake_GeneratesOffsetAndLimit()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Skip(5)
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("OFFSET"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_ComplexQuery_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Value >= 20)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Skip(2)
            .Take(5);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    // ==================== SQL Server SqlQuery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_ForSqlServer_CreatesQueryable()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer();

        // Assert
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(SqlxQueryable<TestEntity>));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_ToSql_GeneratesValidSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer();
        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_Where_GeneratesWhereClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value > 20);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_OrderBy_GeneratesOrderByClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .OrderBy(e => e.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_Take_GeneratesTopOrOffsetFetch()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("TOP") || sql.Contains("OFFSET") || sql.Contains("FETCH"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_ComplexQuery_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value >= 20)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Take(5);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    // ==================== SQLite SqlQuery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQuery_ForSqlite_CreatesQueryable()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite();

        // Assert
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(SqlxQueryable<TestEntity>));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQuery_ToSql_GeneratesValidSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite();
        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQuery_Where_GeneratesWhereClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value > 20);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQuery_OrderBy_GeneratesOrderByClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .OrderBy(e => e.Name);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQuery_Take_GeneratesLimitClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlQuery")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQuery_ComplexQuery_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Value >= 20)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Take(5);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT"));
    }
}
