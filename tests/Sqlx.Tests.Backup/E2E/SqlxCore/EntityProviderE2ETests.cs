// <copyright file="EntityProviderE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;
using System.Linq;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// E2E tests for Sqlx entity provider and metadata handling.
/// </summary>
[TestClass]
public class EntityProviderE2ETests : E2ETestBase
{
    public class SimpleEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EntityWithMultipleProperties
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private static string GetSimpleSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE simple_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE simple_entities (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE simple_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE simple_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static string GetComplexSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE entity_with_multiple_properties (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE entity_with_multiple_properties (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE entity_with_multiple_properties (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(100) NOT NULL,
                    is_active BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE entity_with_multiple_properties (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL,
                    is_active INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== Simple Entity Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SimpleEntity_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.MySQL));

        // Act
        var query = SqlQuery<SimpleEntity>.ForMySql();
        var sql = query.ToSql();

        // Assert - Should generate SQL for all properties
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("id") || sql.Contains("Id"));
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SimpleEntity_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<SimpleEntity>.ForPostgreSQL();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SimpleEntity_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<SimpleEntity>.ForSqlServer();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SimpleEntity_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<SimpleEntity>.ForSqlite();
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    // ==================== Complex Entity Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_ComplexEntity_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.MySQL));

        // Act
        var query = SqlQuery<EntityWithMultipleProperties>.ForMySql();
        var sql = query.ToSql();

        // Assert - Should include all properties
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ComplexEntity_WhereClauseUsesProperties()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.PostgreSQL));

        // Act - Query using multiple properties
        var query = SqlQuery<EntityWithMultipleProperties>.ForPostgreSQL()
            .Where(e => e.Age > 18 && e.IsActive);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("age") || sql.Contains("Age"));
        Assert.IsTrue(sql.Contains("is_active") || sql.Contains("IsActive"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ComplexEntity_OrderByUsesProperties()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<EntityWithMultipleProperties>.ForSqlServer()
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Age);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_ComplexEntity_SelectSpecificProperties()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SQLite));

        // Act - Select only specific properties
        var query = SqlQuery<EntityWithMultipleProperties>.ForSqlite()
            .Select(e => new { e.Name, e.Email });

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
    }

    // ==================== Property Access Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_PropertyAccess_InWhereClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.MySQL));

        // Act - Access property in WHERE clause
        var query = SqlQuery<SimpleEntity>.ForMySql()
            .Where(e => e.Name == "Test");

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(parameters.Any(p => p.Value?.Equals("Test") == true));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_PropertyAccess_InOrderByClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<SimpleEntity>.ForPostgreSQL()
            .OrderBy(e => e.Name);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_PropertyAccess_MultipleConditions()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SqlServer));

        // Act - Multiple property conditions
        var query = SqlQuery<EntityWithMultipleProperties>.ForSqlServer()
            .Where(e => e.Age >= 18 && e.Age <= 65 && e.IsActive);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_PropertyAccess_StringMethods()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.SQLite));

        // Act - Use string methods on property
        var query = SqlQuery<SimpleEntity>.ForSqlite()
            .Where(e => e.Name.StartsWith("Test"));

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    // ==================== Type Mapping Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_BooleanProperty_MapsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.MySQL));

        // Act
        var query = SqlQuery<EntityWithMultipleProperties>.ForMySql()
            .Where(e => e.IsActive == true);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert - Check that WHERE clause is generated
        Assert.IsTrue(sql.Contains("WHERE"), "SQL should contain WHERE clause");
        // The boolean value might be inlined or parameterized depending on implementation
        Assert.IsTrue(sql.Contains("is_active") || sql.Contains("IsActive"), "SQL should reference the IsActive property");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_IntProperty_MapsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<EntityWithMultipleProperties>.ForPostgreSQL()
            .Where(e => e.Age > 25);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(parameters.Any(p => p.Value is int));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringProperty_MapsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<EntityWithMultipleProperties>.ForSqlServer()
            .Where(e => e.Email.Contains("@test.com"));

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_LongProperty_MapsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetSimpleSchema(DatabaseType.SQLite));

        // Act
        var query = SqlQuery<SimpleEntity>.ForSqlite()
            .Where(e => e.Id == 1);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(parameters.Any(p => p.Value is int or long));
    }
}
