// <copyright file="StringFunctionsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.StringFunctions;

/// <summary>
/// E2E tests for SQL string functions (UPPER, LOWER, TRIM, CONCAT, SUBSTRING, LENGTH, LIKE).
/// </summary>
[TestClass]
public class StringFunctionsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_strings (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    text_value VARCHAR(255) NOT NULL,
                    description TEXT
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_strings (
                    id BIGSERIAL PRIMARY KEY,
                    text_value VARCHAR(255) NOT NULL,
                    description TEXT
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_strings (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    text_value NVARCHAR(255) NOT NULL,
                    description NVARCHAR(MAX)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_strings (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    text_value TEXT NOT NULL,
                    description TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var testData = new[]
        {
            ("Hello World", "A greeting"),
            ("  Trimmed  ", "Has spaces"),
            ("UPPERCASE", "All caps"),
            ("lowercase", "All lower"),
            ("MixedCase", "Mixed case"),
        };

        foreach (var (text, desc) in testData)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_strings (text_value, description) VALUES (@text, @desc)";
            AddParameter(cmd, "@text", text);
            AddParameter(cmd, "@desc", desc);
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

    // ==================== UPPER/LOWER Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_UpperLower_TransformsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT UPPER(text_value), LOWER(text_value) FROM test_strings WHERE id = 1";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var upper = reader.GetString(0);
        var lower = reader.GetString(1);

        // Assert
        Assert.AreEqual("HELLO WORLD", upper);
        Assert.AreEqual("hello world", lower);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_UpperLower_TransformsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT UPPER(text_value), LOWER(text_value) FROM test_strings WHERE id = 1";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var upper = reader.GetString(0);
        var lower = reader.GetString(1);

        // Assert
        Assert.AreEqual("HELLO WORLD", upper);
        Assert.AreEqual("hello world", lower);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_UpperLower_TransformsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT UPPER(text_value), LOWER(text_value) FROM test_strings WHERE id = 1";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var upper = reader.GetString(0);
        var lower = reader.GetString(1);

        // Assert
        Assert.AreEqual("HELLO WORLD", upper);
        Assert.AreEqual("hello world", lower);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_UpperLower_TransformsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT UPPER(text_value), LOWER(text_value) FROM test_strings WHERE id = 1";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var upper = reader.GetString(0);
        var lower = reader.GetString(1);

        // Assert
        Assert.AreEqual("HELLO WORLD", upper);
        Assert.AreEqual("hello world", lower);
    }

    // ==================== TRIM Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Trim_RemovesWhitespace()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TRIM(text_value) FROM test_strings WHERE id = 2";
        var trimmed = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Trimmed", trimmed);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Trim_RemovesWhitespace()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TRIM(text_value) FROM test_strings WHERE id = 2";
        var trimmed = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Trimmed", trimmed);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Trim_RemovesWhitespace()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TRIM(text_value) FROM test_strings WHERE id = 2";
        var trimmed = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Trimmed", trimmed);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Trim_RemovesWhitespace()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TRIM(text_value) FROM test_strings WHERE id = 2";
        var trimmed = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Trimmed", trimmed);
    }

    // ==================== LENGTH Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Length_ReturnsCorrectLength()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT LENGTH(text_value) FROM test_strings WHERE id = 1";
        var length = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(11, length); // "Hello World"
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Length_ReturnsCorrectLength()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT LENGTH(text_value) FROM test_strings WHERE id = 1";
        var length = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(11, length);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Length_ReturnsCorrectLength()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT LEN(text_value) FROM test_strings WHERE id = 1";
        var length = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(11, length);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Length_ReturnsCorrectLength()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT LENGTH(text_value) FROM test_strings WHERE id = 1";
        var length = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(11, length);
    }

    // ==================== LIKE Pattern Matching Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Like_MatchesPattern()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_strings WHERE text_value LIKE '%World%'";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Like_MatchesPattern()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_strings WHERE text_value LIKE '%World%'";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Like_MatchesPattern()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_strings WHERE text_value LIKE '%World%'";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Like_MatchesPattern()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_strings WHERE text_value LIKE '%World%'";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(1, count);
    }

    // ==================== SUBSTRING Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Substring_ExtractsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUBSTRING(text_value, 1, 5) FROM test_strings WHERE id = 1";
        var substring = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello", substring);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Substring_ExtractsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUBSTRING(text_value, 1, 5) FROM test_strings WHERE id = 1";
        var substring = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello", substring);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Substring_ExtractsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUBSTRING(text_value, 1, 5) FROM test_strings WHERE id = 1";
        var substring = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello", substring);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Substring_ExtractsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUBSTR(text_value, 1, 5) FROM test_strings WHERE id = 1";
        var substring = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello", substring);
    }

    // ==================== CONCAT Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Concat_CombinesStrings()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT CONCAT(text_value, ' - ', description) FROM test_strings WHERE id = 1";
        var result = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello World - A greeting", result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Concat_CombinesStrings()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT text_value || ' - ' || description FROM test_strings WHERE id = 1";
        var result = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello World - A greeting", result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Concat_CombinesStrings()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT CONCAT(text_value, ' - ', description) FROM test_strings WHERE id = 1";
        var result = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello World - A greeting", result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("StringFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Concat_CombinesStrings()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT text_value || ' - ' || description FROM test_strings WHERE id = 1";
        var result = (string)(await cmd.ExecuteScalarAsync())!;

        // Assert
        Assert.AreEqual("Hello World - A greeting", result);
    }
}
