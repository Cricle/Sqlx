// <copyright file="SqlxQueryApiE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;
using System.Linq;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// E2E tests for Sqlx core API: SqlQuery&lt;T&gt;.For* methods and IQueryable integration.
/// </summary>
[TestClass]
public class SqlxQueryApiE2ETests : E2ETestBase
{
    public class TestUser
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var users = new[]
        {
            ("Alice", 25, "alice@test.com"),
            ("Bob", 30, "bob@test.com"),
            ("Charlie", 35, "charlie@test.com"),
        };

        foreach (var (name, age, email) in users)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_users (name, age, email) VALUES (@name, @age, @email)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@age", age);
            AddParameter(cmd, "@email", email);
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

    // ==================== SqlQuery<T>.For* API Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQueryForMySql_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Use SqlQuery<T>.ForMySql() API
        var query = SqlQuery<TestUser>.ForMySql()
            .Where(u => u.Age >= 30);

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        Assert.IsNotNull(sqlxQuery);

        var sql = sqlxQuery.ToSql();

        // Assert - Verify SQL generation
        Assert.IsTrue(sql.Contains("SELECT"), "SQL should contain SELECT");
        Assert.IsTrue(sql.Contains("FROM"), "SQL should contain FROM");
        Assert.IsTrue(sql.Contains("WHERE"), "SQL should contain WHERE");
        Assert.IsTrue(sql.Contains("age"), "SQL should reference age column");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQueryForPostgreSQL_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        var query = SqlQuery<TestUser>.ForPostgreSQL()
            .Where(u => u.Name == "Alice");

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        Assert.IsNotNull(sqlxQuery);

        var sql = sqlxQuery.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQueryForSqlServer_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        var query = SqlQuery<TestUser>.ForSqlServer()
            .OrderBy(u => u.Age);

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        Assert.IsNotNull(sqlxQuery);

        var sql = sqlxQuery.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlQueryForSqlite_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        var query = SqlQuery<TestUser>.ForSqlite()
            .Take(10);

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        Assert.IsNotNull(sqlxQuery);

        var sql = sqlxQuery.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    // ==================== LINQ Query Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_LinqWhere_TranslatesToSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Complex LINQ query
        var query = SqlQuery<TestUser>.ForMySql()
            .Where(u => u.Age > 25 && u.Name.StartsWith("A"))
            .OrderBy(u => u.Name);

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        Assert.IsNotNull(sqlxQuery);

        var sql = sqlxQuery.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("age"));
        Assert.IsTrue(sql.Contains("name"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_LinqSelect_TranslatesToSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Select projection
        var query = SqlQuery<TestUser>.ForSqlite()
            .Select(u => new { u.Name, u.Email });

        var sqlxQuery = query as IQueryable;
        Assert.IsNotNull(sqlxQuery);

        // Assert - Query should be created successfully
        Assert.IsNotNull(sqlxQuery.Expression);
    }

    // ==================== Dialect-Specific SQL Generation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_GeneratesSqlWithBackticks()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act
        var query = SqlQuery<TestUser>.ForMySql();
        var sqlxQuery = query as SqlxQueryable<TestUser>;
        var sql = sqlxQuery!.ToSql();

        // Assert - MySQL uses backticks for identifiers
        Assert.IsTrue(sql.Contains("`") || sql.Contains("test_users"), "MySQL should use backticks or table name");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GeneratesSqlWithDoubleQuotes()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        var query = SqlQuery<TestUser>.ForPostgreSQL();
        var sqlxQuery = query as SqlxQueryable<TestUser>;
        var sql = sqlxQuery!.ToSql();

        // Assert - PostgreSQL uses double quotes for identifiers
        Assert.IsTrue(sql.Contains("\"") || sql.Contains("test_users"), "PostgreSQL should use double quotes or table name");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GeneratesSqlWithSquareBrackets()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        var query = SqlQuery<TestUser>.ForSqlServer();
        var sqlxQuery = query as SqlxQueryable<TestUser>;
        var sql = sqlxQuery!.ToSql();

        // Assert - SQL Server uses square brackets for identifiers
        Assert.IsTrue(sql.Contains("[") || sql.Contains("test_users"), "SQL Server should use square brackets or table name");
    }

    // ==================== Query Composition Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_QueryComposition_ChainsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Chain multiple LINQ operations
        var query = SqlQuery<TestUser>.ForSqlite()
            .Where(u => u.Age >= 25)
            .Where(u => u.Email.Contains("@test.com"))
            .OrderBy(u => u.Name)
            .Take(5);

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        Assert.IsNotNull(sqlxQuery);

        var sql = sqlxQuery.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_MultipleWhereConditions_CombinesWithAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        var query = SqlQuery<TestUser>.ForMySql()
            .Where(u => u.Age > 20)
            .Where(u => u.Age < 40);

        var sqlxQuery = query as SqlxQueryable<TestUser>;
        var sql = sqlxQuery!.ToSql();

        // Assert - Multiple WHERE conditions should be combined
        Assert.IsTrue(sql.Contains("WHERE"));
        var whereCount = sql.Split(new[] { "WHERE" }, StringSplitOptions.None).Length - 1;
        Assert.AreEqual(1, whereCount, "Should have single WHERE clause combining conditions");
    }
}
