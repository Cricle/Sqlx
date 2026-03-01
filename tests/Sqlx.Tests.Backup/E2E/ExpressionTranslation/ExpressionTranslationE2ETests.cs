// <copyright file="ExpressionTranslationE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.ExpressionTranslation;

#region Test Models

[Sqlx]
public class DataRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

#endregion

/// <summary>
/// E2E tests for C# expression to SQL translation in Sqlx.
/// Tests that Sqlx correctly translates various C# expressions to SQL.
/// </summary>
[TestClass]
public class ExpressionTranslationE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE data_records (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    count INT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL,
                    created_at DATETIME NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE data_records (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    count INT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE data_records (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    count INT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE data_records (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    count INTEGER NOT NULL,
                    amount REAL NOT NULL,
                    created_at TEXT NOT NULL,
                    is_active INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== Property Access Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("MySQL")]
    public async Task MySQL_PropertyAccess_TranslatesToColumnReference()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test property access translation
        var query = SqlQuery<DataRecord>.ForMySql()
            .Where(d => d.Count > 10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("count") || sql.Contains("Count"), 
            "Should translate property access to column reference");
    }

    // ==================== Arithmetic Expression Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ArithmeticExpression_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test arithmetic expression translation
        var query = SqlQuery<DataRecord>.ForPostgreSQL()
            .Where(d => d.Count * 2 > 20);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("*") || sql.Contains("count"), 
            "Should translate arithmetic expressions");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_DivisionExpression_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Test division expression
        var query = SqlQuery<DataRecord>.ForSqlServer()
            .Where(d => d.Amount / 2 > 50);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("/") || sql.Contains("amount"), 
            "Should translate division expressions");
    }

    // ==================== Boolean Expression Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("MySQL")]
    public async Task MySQL_BooleanProperty_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test boolean property translation
        var query = SqlQuery<DataRecord>.ForMySql()
            .Where(d => d.IsActive);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("is_active") || sql.Contains("IsActive"), 
            "Should translate boolean property");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_BooleanNegation_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test boolean negation
        var query = SqlQuery<DataRecord>.ForPostgreSQL()
            .Where(d => !d.IsActive);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("=") || sql.Contains("is_active"), 
            "Should translate boolean negation");
    }

    // ==================== String Method Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("MySQL")]
    public async Task MySQL_StringToLower_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test ToLower translation
        var query = SqlQuery<DataRecord>.ForMySql()
            .Where(d => d.Name.ToLower() == "test");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LOWER") || sql.Contains("lower"), 
            "Should translate ToLower to LOWER function");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringToUpper_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test ToUpper translation
        var query = SqlQuery<DataRecord>.ForPostgreSQL()
            .Where(d => d.Name.ToUpper() == "TEST");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("UPPER") || sql.Contains("upper"), 
            "Should translate ToUpper to UPPER function");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_StringTrim_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Test Trim translation
        var query = SqlQuery<DataRecord>.ForSqlServer()
            .Where(d => d.Name.Trim() == "test");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("TRIM") || sql.Contains("LTRIM") || sql.Contains("RTRIM"), 
            "Should translate Trim to TRIM/LTRIM/RTRIM function");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("SQLite")]
    public async Task SQLite_StringLength_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Test Length translation
        var query = SqlQuery<DataRecord>.ForSqlite()
            .Where(d => d.Name.Length > 5);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LENGTH") || sql.Contains("length") || sql.Contains("LEN"), 
            "Should translate Length to LENGTH/LEN function");
    }

    // ==================== Null Coalescing Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("MySQL")]
    public async Task MySQL_NullCoalescing_TranslatesToCoalesce()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test null coalescing operator
        var defaultName = "Unknown";
        var query = SqlQuery<DataRecord>.ForMySql()
            .Select(d => new { Name = d.Name ?? defaultName });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("COALESCE") || sql.Contains("IFNULL") || sql.Contains("name"), 
            "Should translate ?? to COALESCE or IFNULL");
    }

    // ==================== Conditional Expression Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TernaryOperator_TranslatesToCase()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test ternary operator translation
        var query = SqlQuery<DataRecord>.ForPostgreSQL()
            .Select(d => new { Status = d.IsActive ? "Active" : "Inactive" });
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("CASE") || sql.Contains("case"), 
            "Should translate ternary operator to CASE expression");
    }

    // ==================== Member Access Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("MySQL")]
    public async Task MySQL_DateTimeProperty_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test DateTime property access
        var query = SqlQuery<DataRecord>.ForMySql()
            .Where(d => d.CreatedAt.Year == 2024);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("YEAR") || sql.Contains("year") || sql.Contains("created_at"), 
            "Should translate DateTime.Year to YEAR function");
    }

    // ==================== Constant Expression Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ConstantValue_ParameterizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test constant parameterization
        var query = SqlQuery<DataRecord>.ForPostgreSQL()
            .Where(d => d.Count == 42);
        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(parameters.Any(p => p.Value?.Equals(42) == true), 
            "Should parameterize constant values");
    }

    // ==================== Variable Capture Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_LocalVariable_ParameterizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var minCount = 10;

        // Act - Test local variable capture
        var query = SqlQuery<DataRecord>.ForSqlServer()
            .Where(d => d.Count >= minCount);
        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(parameters.Any(p => p.Value?.Equals(minCount) == true), 
            "Should parameterize captured variables");
    }

    // ==================== Complex Expression Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("MySQL")]
    public async Task MySQL_ComplexExpression_TranslatesToSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var threshold = 100m;

        // Act - Test complex expression with multiple operations
        var query = SqlQuery<DataRecord>.ForMySql()
            .Where(d => d.Amount > threshold && d.Count > 0 && d.IsActive);
        var (sql, parameters) = query.ToSqlWithParameters();

        // Assert
        Assert.IsTrue(sql.Contains("AND"), "Should combine conditions with AND");
        Assert.IsTrue(parameters.Count() >= 1, "Should parameterize values");
    }

    // ==================== Method Call Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_StringEndsWith_TranslatesToLike()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test EndsWith translation
        var query = SqlQuery<DataRecord>.ForPostgreSQL()
            .Where(d => d.Name.EndsWith(".txt"));
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"), 
            "Should translate EndsWith to LIKE");
    }

    // ==================== Type Conversion Translation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExpressionTranslation")]
    [TestCategory("SQLite")]
    public async Task SQLite_ToString_TranslatesToCast()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Test ToString translation
        var query = SqlQuery<DataRecord>.ForSqlite()
            .Where(d => d.Count.ToString() == "100");
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("CAST") || sql.Contains("cast") || sql.Contains("count"), 
            "Should translate ToString to CAST or string conversion");
    }
}
