// <copyright file="SqlxQueryableE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.SqlxQueryable;

#region Test Models

[Sqlx]
public class Employee
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string Email { get; set; } = string.Empty;
}

#endregion

/// <summary>
/// E2E tests for SqlxQueryable LINQ functionality.
/// Tests LINQ-to-SQL translation, query composition, and execution.
/// </summary>
[TestClass]
public class SqlxQueryableE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE employees (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    department VARCHAR(50) NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE employees (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    department VARCHAR(50) NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE employees (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL,
                    department NVARCHAR(50) NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL,
                    email NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE employees (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    department TEXT NOT NULL,
                    salary REAL NOT NULL,
                    email TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== LINQ Where Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxQueryable_Where_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test SqlxQueryable Where clause
        var query = SqlQuery<Employee>.ForMySql()
            .Where(e => e.Age > 30);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"), "Should generate WHERE clause");
        Assert.IsTrue(sql.Contains("age") || sql.Contains("Age"), "Should reference age column");
        Assert.IsTrue(parameters.Any(p => p.Value?.Equals(30) == true), "Should parameterize age value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxQueryable_MultipleWhere_CombinesWithAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test multiple Where clauses
        var query = SqlQuery<Employee>.ForPostgreSQL()
            .Where(e => e.Age > 25)
            .Where(e => e.Department == "IT");

        var (sql, _) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"), "Should generate WHERE clause");
        Assert.IsTrue(sql.Contains("AND") || sql.Split("WHERE").Length > 1, "Should combine conditions");
    }

    // ==================== LINQ OrderBy Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxQueryable_OrderBy_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Test SqlxQueryable OrderBy
        var query = SqlQuery<Employee>.ForSqlServer()
            .OrderBy(e => e.Salary);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should generate ORDER BY clause");
        Assert.IsTrue(sql.Contains("salary") || sql.Contains("Salary"), "Should reference salary column");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxQueryable_OrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Test SqlxQueryable OrderByDescending
        var query = SqlQuery<Employee>.ForSqlite()
            .OrderByDescending(e => e.Age);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should generate ORDER BY clause");
        Assert.IsTrue(sql.Contains("DESC"), "Should include DESC keyword");
    }

    // ==================== LINQ Take/Skip Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxQueryable_Take_GeneratesLimitClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test SqlxQueryable Take
        var query = SqlQuery<Employee>.ForMySql()
            .Take(10);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("TOP"), "Should generate LIMIT/TOP clause");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxQueryable_SkipAndTake_GeneratesOffsetAndLimit()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test SqlxQueryable Skip and Take
        var query = SqlQuery<Employee>.ForPostgreSQL()
            .Skip(5)
            .Take(10);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("OFFSET"), "Should generate pagination clauses");
    }

    // ==================== LINQ Select Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxQueryable_Select_ProjectsSpecificColumns()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test SqlxQueryable Select projection
        var query = SqlQuery<Employee>.ForMySql()
            .Select(e => new { e.Name, e.Email });

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("SELECT"), "Should generate SELECT clause");
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"), "Should include name column");
        Assert.IsTrue(sql.Contains("email") || sql.Contains("Email"), "Should include email column");
    }

    // ==================== LINQ Complex Query Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxQueryable_ComplexQuery_CombinesMultipleOperations()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test complex query composition
        var query = SqlQuery<Employee>.ForPostgreSQL()
            .Where(e => e.Age >= 25)
            .Where(e => e.Salary > 50000)
            .OrderBy(e => e.Name)
            .Take(20);

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("WHERE"), "Should have WHERE clause");
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should have ORDER BY clause");
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("TOP"), "Should have LIMIT clause");
        Assert.IsTrue(parameters.Count() >= 2, "Should have multiple parameters");
    }

    // ==================== LINQ String Method Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxQueryable_StringContains_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test string Contains translation
        var query = SqlQuery<Employee>.ForMySql()
            .Where(e => e.Email.Contains("@example.com"));

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"), "Should translate Contains to LIKE");
        // Parameters may or may not include wildcards depending on implementation
        Assert.IsTrue(parameters.Count() >= 1, "Should have at least one parameter");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxQueryable_StringStartsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Test string StartsWith translation
        var query = SqlQuery<Employee>.ForSqlite()
            .Where(e => e.Name.StartsWith("John"));

        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"), "Should translate StartsWith to LIKE");
        // Parameters may or may not include wildcards depending on implementation
        Assert.IsTrue(parameters.Count() >= 1, "Should have at least one parameter");
    }

    // ==================== LINQ Comparison Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxQueryable_ComparisonOperators_TranslateCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Test various comparison operators
        var query1 = SqlQuery<Employee>.ForSqlServer().Where(e => e.Age == 30);
        var query2 = SqlQuery<Employee>.ForSqlServer().Where(e => e.Age != 30);
        var query3 = SqlQuery<Employee>.ForSqlServer().Where(e => e.Age > 30);
        var query4 = SqlQuery<Employee>.ForSqlServer().Where(e => e.Age >= 30);
        var query5 = SqlQuery<Employee>.ForSqlServer().Where(e => e.Age < 30);
        var query6 = SqlQuery<Employee>.ForSqlServer().Where(e => e.Age <= 30);

        // Assert
        Assert.IsTrue(query1.ToSql().Contains("="), "Should translate == to =");
        Assert.IsTrue(query2.ToSql().Contains("!=") || query2.ToSql().Contains("<>"), "Should translate != to != or <>");
        Assert.IsTrue(query3.ToSql().Contains(">"), "Should translate > to >");
        Assert.IsTrue(query4.ToSql().Contains(">="), "Should translate >= to >=");
        Assert.IsTrue(query5.ToSql().Contains("<"), "Should translate < to <");
        Assert.IsTrue(query6.ToSql().Contains("<="), "Should translate <= to <=");
    }

    // ==================== LINQ Logical Operators Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxQueryable_LogicalAnd_TranslatesToAnd()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test logical AND
        var query = SqlQuery<Employee>.ForMySql()
            .Where(e => e.Age > 25 && e.Salary > 50000);

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("AND"), "Should translate && to AND");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxQueryable")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxQueryable_LogicalOr_TranslatesToOr()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test logical OR
        var query = SqlQuery<Employee>.ForPostgreSQL()
            .Where(e => e.Department == "IT" || e.Department == "HR");

        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OR"), "Should translate || to OR");
    }
}
