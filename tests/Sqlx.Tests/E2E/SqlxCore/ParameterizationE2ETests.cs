// <copyright file="ParameterizationE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// E2E tests for Sqlx parameterization and SQL injection prevention.
/// </summary>
[TestClass]
public class ParameterizationE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_data (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    text_value VARCHAR(200) NOT NULL,
                    int_value INT NOT NULL,
                    decimal_value DECIMAL(10, 2) NOT NULL,
                    bool_value BOOLEAN NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_data (
                    id BIGSERIAL PRIMARY KEY,
                    text_value VARCHAR(200) NOT NULL,
                    int_value INT NOT NULL,
                    decimal_value DECIMAL(10, 2) NOT NULL,
                    bool_value BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_data (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    text_value NVARCHAR(200) NOT NULL,
                    int_value INT NOT NULL,
                    decimal_value DECIMAL(10, 2) NOT NULL,
                    bool_value BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    text_value TEXT NOT NULL,
                    int_value INTEGER NOT NULL,
                    decimal_value REAL NOT NULL,
                    bool_value INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== String Parameterization Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_StringWithQuotes_ParameterizedSafely()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var dangerousString = "O'Reilly"; // Contains single quote

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_data WHERE text_value = {dangerousString}");
        var template = builder.Build();

        // Assert - String should be parameterized, not embedded
        Assert.IsFalse(template.Sql.Contains("O'Reilly"), "String should not be embedded in SQL");
        Assert.IsTrue(template.Parameters.ContainsValue(dangerousString), "String should be in parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_StringWithSqlKeywords_ParameterizedSafely()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var sqlInjectionAttempt = "'; DROP TABLE test_data; --";

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_data WHERE text_value = {sqlInjectionAttempt}");
        var template = builder.Build();

        // Assert - SQL injection attempt should be parameterized
        Assert.IsFalse(template.Sql.Contains("DROP TABLE"), "SQL injection should not be in SQL");
        Assert.IsTrue(template.Parameters.ContainsValue(sqlInjectionAttempt), "Value should be in parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_EmptyString_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var emptyString = string.Empty;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SqlServer);
        builder.Append($"SELECT * FROM test_data WHERE text_value = {emptyString}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(emptyString));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_UnicodeString_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var unicodeString = "测试数据 🎉";

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_data WHERE text_value = {unicodeString}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Parameters.ContainsValue(unicodeString));
    }

    // ==================== Numeric Parameterization Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_IntegerValue_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var intValue = 42;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_data WHERE int_value = {intValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(intValue));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_DecimalValue_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var decimalValue = 123.45m;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_data WHERE decimal_value = {decimalValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(decimalValue));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_NegativeNumber_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var negativeValue = -999;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SqlServer);
        builder.Append($"SELECT * FROM test_data WHERE int_value = {negativeValue}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Parameters.ContainsValue(negativeValue));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_ZeroValue_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var zeroValue = 0;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_data WHERE int_value = {zeroValue}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Parameters.ContainsValue(zeroValue));
    }

    // ==================== Boolean Parameterization Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_BooleanTrue_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var boolValue = true;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_data WHERE bool_value = {boolValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(boolValue));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_BooleanFalse_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var boolValue = false;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_data WHERE bool_value = {boolValue}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Parameters.ContainsValue(boolValue));
    }

    // ==================== Null Parameterization Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_NullValue_ParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        string? nullValue = null;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_data WHERE text_value = {nullValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(null));
    }

    // ==================== Multiple Parameters Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_MixedTypes_AllParameterizedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var stringVal = "test";
        var intVal = 42;
        var decimalVal = 99.99m;
        var boolVal = true;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_data WHERE text_value = {stringVal} AND int_value = {intVal} AND decimal_value = {decimalVal} AND bool_value = {boolVal}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(4, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(stringVal));
        Assert.IsTrue(template.Parameters.ContainsValue(intVal));
        Assert.IsTrue(template.Parameters.ContainsValue(decimalVal));
        Assert.IsTrue(template.Parameters.ContainsValue(boolVal));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_SameValueMultipleTimes_CreatesMultipleParameters()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_data WHERE int_value = {value} OR int_value = {value}");
        var template = builder.Build();

        // Assert - Should create separate parameters even for same value
        Assert.AreEqual(2, template.Parameters.Count);
    }
}
