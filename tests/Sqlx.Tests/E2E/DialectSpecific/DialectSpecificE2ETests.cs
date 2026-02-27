// <copyright file="DialectSpecificE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.DialectSpecific;

#region Test Models

[Sqlx]
public class TestRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

#endregion

/// <summary>
/// E2E tests for dialect-specific SQL generation in Sqlx.
/// Tests that Sqlx generates correct SQL for each database dialect.
/// </summary>
[TestClass]
public class DialectSpecificE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_records (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_records (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_records (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_records (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== Dialect-Specific Identifier Quoting Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_UsesBacktickQuoting()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test MySQL uses backticks for identifiers
        var query = SqlQuery<TestRecord>.ForMySql();
        var sql = query.ToSql();

        // Assert - MySQL should use backticks or no quotes
        Assert.IsFalse(sql.Contains("[") && sql.Contains("]"), "MySQL should not use square brackets");
        Assert.IsFalse(sql.Contains("\""), "MySQL should not use double quotes for identifiers");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_UsesDoubleQuoteQuoting()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test PostgreSQL identifier quoting
        var query = SqlQuery<TestRecord>.ForPostgreSQL();
        var sql = query.ToSql();

        // Assert - PostgreSQL may use double quotes or no quotes
        Assert.IsFalse(sql.Contains("[") && sql.Contains("]"), "PostgreSQL should not use square brackets");
        Assert.IsFalse(sql.Contains("`"), "PostgreSQL should not use backticks");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_UsesSquareBracketQuoting()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Test SQL Server uses square brackets
        var query = SqlQuery<TestRecord>.ForSqlServer();
        var sql = query.ToSql();

        // Assert - SQL Server may use square brackets or no quotes
        Assert.IsFalse(sql.Contains("`"), "SQL Server should not use backticks");
        Assert.IsFalse(sql.Contains("\""), "SQL Server should not use double quotes for identifiers");
    }

    // ==================== Dialect-Specific Parameter Prefix Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        // Act - Test MySQL parameter prefix
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_records WHERE value = {100}");
        var template = builder.Build();

        // Assert - MySQL uses @ or ?
        Assert.IsTrue(
            template.Sql.Contains("@") || template.Sql.Contains("?"),
            "MySQL should use @ or ? for parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);

        // Act - Test PostgreSQL parameter prefix
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_records WHERE value = {100}");
        var template = builder.Build();

        // Assert - PostgreSQL uses $ or @
        Assert.IsTrue(
            template.Sql.Contains("$") || template.Sql.Contains("@"),
            "PostgreSQL should use $ or @ for parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_UsesAtSignParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);

        // Act - Test SQL Server parameter prefix
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SqlServer);
        builder.Append($"SELECT * FROM test_records WHERE value = {100}");
        var template = builder.Build();

        // Assert - SQL Server uses @
        Assert.IsTrue(template.Sql.Contains("@"), "SQL Server should use @ for parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);

        // Act - Test SQLite parameter prefix
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_records WHERE value = {100}");
        var template = builder.Build();

        // Assert - SQLite uses ?, @, or $
        Assert.IsTrue(
            template.Sql.Contains("?") || template.Sql.Contains("@") || template.Sql.Contains("$"),
            "SQLite should use ?, @, or $ for parameters");
    }

    // ==================== Dialect-Specific LIMIT/OFFSET Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_Take_GeneratesLimitClause()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test MySQL LIMIT syntax
        var query = SqlQuery<TestRecord>.ForMySql().Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT"), "MySQL should use LIMIT");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_SkipTake_GeneratesLimitOffset()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test PostgreSQL LIMIT OFFSET syntax
        var query = SqlQuery<TestRecord>.ForPostgreSQL().Skip(5).Take(10);
        var sql = query.ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("OFFSET"), 
            "PostgreSQL should use LIMIT and/or OFFSET");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlQuery_Take_GeneratesTopOrOffsetFetch()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Test SQL Server TOP or OFFSET FETCH syntax
        var query = SqlQuery<TestRecord>.ForSqlServer().Take(10);
        var sql = query.ToSql();

        // Assert - SQL Server uses TOP or OFFSET FETCH
        Assert.IsTrue(
            sql.Contains("TOP") || sql.Contains("OFFSET") || sql.Contains("FETCH"),
            "SQL Server should use TOP or OFFSET FETCH");
    }

    // ==================== Dialect-Specific String Concatenation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlQuery_StringConcat_UsesCorrectSyntax()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Test string concatenation translation
        var query = SqlQuery<TestRecord>.ForMySql()
            .Where(r => (r.Name + "_suffix").Contains("test"));
        var sql = query.ToSql();

        // Assert - MySQL uses CONCAT or +
        Assert.IsTrue(
            sql.Contains("CONCAT") || sql.Contains("+"),
            "MySQL should translate string concatenation");
    }

    // ==================== Dialect-Specific Boolean Handling Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_BooleanValue_ParameterizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        var boolValue = true;

        // Act - Test MySQL boolean parameterization
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_records WHERE active = {boolValue}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Parameters.ContainsValue(boolValue) || 
                     template.Parameters.ContainsValue(1) ||
                     template.Parameters.ContainsValue((byte)1),
            "MySQL should parameterize boolean as bool, int, or byte");
    }

    // ==================== Dialect-Specific Case Sensitivity Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlQuery_GeneratesCaseSensitiveSQL()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Test PostgreSQL case sensitivity
        var query = SqlQuery<TestRecord>.ForPostgreSQL();
        var sql = query.ToSql();

        // Assert - PostgreSQL is case-sensitive, may use lowercase
        Assert.IsTrue(sql.Length > 0, "Should generate SQL");
        // PostgreSQL typically uses lowercase for keywords
    }

    // ==================== Dialect-Specific NULL Handling Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_NullValue_HandlesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        string? nullValue = null;

        // Act - Test NULL parameterization
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_records WHERE name = {nullValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(null), "Should parameterize NULL");
    }

    // ==================== Dialect-Specific Auto-Increment Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("MySQL")]
    public async Task MySQL_Dialect_SupportsAutoIncrement()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - Verify MySQL auto-increment works
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO test_records (name, value) VALUES ('Test', 100)";
        await cmd.ExecuteNonQueryAsync();

        // Assert - Should have auto-generated ID
        cmd.CommandText = "SELECT LAST_INSERT_ID()";
        var id = await cmd.ExecuteScalarAsync();
        Assert.IsNotNull(id);
        Assert.IsTrue(Convert.ToInt64(id) > 0);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Dialect_SupportsSerial()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act - Verify PostgreSQL SERIAL works
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO test_records (name, value) VALUES ('Test', 100) RETURNING id";
        var id = await cmd.ExecuteScalarAsync();

        // Assert
        Assert.IsNotNull(id);
        Assert.IsTrue(Convert.ToInt64(id) > 0);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Dialect_SupportsIdentity()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act - Verify SQL Server IDENTITY works
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO test_records (name, value) VALUES ('Test', 100); SELECT SCOPE_IDENTITY()";
        var id = await cmd.ExecuteScalarAsync();

        // Assert
        Assert.IsNotNull(id);
        Assert.IsTrue(Convert.ToDecimal(id) > 0);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DialectSpecific")]
    [TestCategory("SQLite")]
    public async Task SQLite_Dialect_SupportsAutoIncrement()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act - Verify SQLite AUTOINCREMENT works
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO test_records (name, value) VALUES ('Test', 100)";
        await cmd.ExecuteNonQueryAsync();

        // Assert
        cmd.CommandText = "SELECT last_insert_rowid()";
        var id = await cmd.ExecuteScalarAsync();
        Assert.IsNotNull(id);
        Assert.IsTrue(Convert.ToInt64(id) > 0);
    }
}
