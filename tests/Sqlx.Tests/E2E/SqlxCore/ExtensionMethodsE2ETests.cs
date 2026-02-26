// <copyright file="ExtensionMethodsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;
using System.Linq;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// E2E tests for Sqlx extension methods: WithConnection, WithReader, ToSql, ToSqlWithParameters.
/// </summary>
[TestClass]
public class ExtensionMethodsE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== ToSql Extension Method Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_ToSql_GeneratesSqlString()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 10);

        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 0);
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ToSql_GeneratesSqlString()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .OrderBy(e => e.Name);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ToSql_GeneratesSqlString()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Take(5);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("TOP") || sql.Contains("FETCH"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_ToSql_GeneratesSqlString()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.Name == "test");

        var sql = query.ToSql();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    // ==================== ToSqlWithParameters Extension Method Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_ToSqlWithParameters_ReturnsSqlAndParameters()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var minValue = 100;

        // Act
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value >= minValue);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsNotNull(parameters);
        Assert.IsTrue(sql.Contains("WHERE"));
        
        var paramList = parameters.ToList();
        Assert.IsTrue(paramList.Count > 0, "Should have at least one parameter");
        Assert.IsTrue(paramList.Any(p => p.Value?.Equals(minValue) == true), "Should contain the minValue parameter");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ToSqlWithParameters_ReturnsSqlAndParameters()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var name = "TestName";

        // Act
        var query = SqlQuery<TestEntity>.ForPostgreSQL()
            .Where(e => e.Name == name);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        var paramList = parameters.ToList();
        Assert.IsTrue(paramList.Count > 0);
        Assert.IsTrue(paramList.Any(p => p.Value?.Equals(name) == true));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ToSqlWithParameters_MultipleParameters()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var minValue = 10;
        var maxValue = 100;

        // Act
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Where(e => e.Value >= minValue && e.Value <= maxValue);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        var paramList = parameters.ToList();
        Assert.IsTrue(paramList.Count >= 2, "Should have at least 2 parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_ToSqlWithParameters_NoParameters()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Query without parameters
        var query = SqlQuery<TestEntity>.ForSqlite();

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsNotNull(parameters);
        var paramList = parameters.ToList();
        Assert.AreEqual(0, paramList.Count, "Should have no parameters");
    }

    // ==================== Query Composition Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_ComplexQuery_ToSqlGeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Complex query with multiple operations
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value > 10)
            .Where(e => e.Name.StartsWith("Test"))
            .OrderBy(e => e.Value)
            .ThenBy(e => e.Name)
            .Skip(5)
            .Take(10);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SelectProjection_ToSqlGeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Select with projection
        var query = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Name, e.Value });

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"));
        Assert.IsTrue(sql.Contains("value") || sql.Contains("Value"));
    }

    // ==================== ToSql vs ToSqlWithParameters Comparison Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_ToSqlVsToSqlWithParameters_BothWork()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var value = 50;

        // Act
        var query = SqlQuery<TestEntity>.ForMySql()
            .Where(e => e.Value == value);

        var sql1 = query.ToSql();
        var (sql2, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsNotNull(sql1);
        Assert.IsNotNull(sql2);
        Assert.IsTrue(sql1.Length > 0);
        Assert.IsTrue(sql2.Length > 0);
        
        // ToSqlWithParameters should have parameters
        Assert.IsTrue(parameters.Any());
    }

    // ==================== Edge Cases Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_EmptyQuery_ToSqlGeneratesBasicSelect()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Query with no conditions
        var query = SqlQuery<TestEntity>.ForPostgreSQL();

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_OnlyOrderBy_ToSqlGeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Query with only ORDER BY
        var query = SqlQuery<TestEntity>.ForSqlite()
            .OrderBy(e => e.Name);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsFalse(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OnlyTake_ToSqlGeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Query with only TAKE
        var query = SqlQuery<TestEntity>.ForSqlServer()
            .Take(10);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("TOP") || sql.Contains("FETCH"));
    }
}
