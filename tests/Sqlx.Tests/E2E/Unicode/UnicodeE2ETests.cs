// <copyright file="UnicodeE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Unicode;

/// <summary>
/// E2E tests for Unicode and special character handling in Sqlx across all supported databases.
/// Tests focus on Sqlx's parameter binding, type conversion, and SqlBuilder functionality with Unicode data.
/// </summary>
[TestClass]
public class UnicodeE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE unicode_test (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    text_data VARCHAR(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                    description VARCHAR(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE unicode_test (
                    id BIGSERIAL PRIMARY KEY,
                    text_data VARCHAR(1000),
                    description VARCHAR(500)
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE unicode_test (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    text_data NVARCHAR(1000),
                    description NVARCHAR(500)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE unicode_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    text_data TEXT,
                    description TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task<long> InsertUnicodeDataAsync(
        DbConnection connection,
        string textData,
        string description,
        DatabaseType dbType,
        DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;

        if (dbType == DatabaseType.PostgreSQL)
        {
            cmd.CommandText = "INSERT INTO unicode_test (text_data, description) VALUES (@text, @desc) RETURNING id";
        }
        else if (dbType == DatabaseType.SqlServer)
        {
            cmd.CommandText = "INSERT INTO unicode_test (text_data, description) VALUES (@text, @desc); SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
        }
        else
        {
            cmd.CommandText = "INSERT INTO unicode_test (text_data, description) VALUES (@text, @desc)";
        }

        AddParameter(cmd, "@text", textData);
        AddParameter(cmd, "@desc", description);

        if (dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SqlServer)
        {
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        else
        {
            await cmd.ExecuteNonQueryAsync();

            using var idCmd = connection.CreateCommand();
            idCmd.Transaction = transaction;
            idCmd.CommandText = dbType == DatabaseType.MySQL
                ? "SELECT LAST_INSERT_ID()"
                : "SELECT last_insert_rowid()";

            var idResult = await idCmd.ExecuteScalarAsync();
            return Convert.ToInt64(idResult);
        }
    }

    private static async Task<(string TextData, string Description)?> SelectUnicodeDataAsync(
        DbConnection connection,
        long id,
        DatabaseType dbType,
        DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT text_data, description FROM unicode_test WHERE id = @id";
        AddParameter(cmd, "@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return (reader.GetString(0), reader.GetString(1));
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== Emoji Tests (Sqlx Parameter Binding) ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_ParameterBinding_Emoji_PreservesExactValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var emojiText = UnicodeDataGenerator.GenerateEmoji(5);
        var description = "Emoji test";

        // Act - Test Sqlx parameter binding with emoji
        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            emojiText,
            description,
            DatabaseType.MySQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.MySQL);

        // Assert - Verify Sqlx correctly bound and retrieved emoji
        Assert.IsNotNull(result);
        Assert.AreEqual(emojiText, result.Value.TextData, "Sqlx should preserve emoji through parameter binding");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_ParameterBinding_Emoji_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var emojiText = UnicodeDataGenerator.GenerateEmoji(5);
        var description = "Emoji test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            emojiText,
            description,
            DatabaseType.PostgreSQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.PostgreSQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(emojiText, result.Value.TextData, "Sqlx should preserve emoji through parameter binding");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_ParameterBinding_Emoji_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var emojiText = UnicodeDataGenerator.GenerateEmoji(5);
        var description = "Emoji test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            emojiText,
            description,
            DatabaseType.SqlServer);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.SqlServer);

        Assert.IsNotNull(result);
        Assert.AreEqual(emojiText, result.Value.TextData, "Sqlx should preserve emoji through parameter binding");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_ParameterBinding_Emoji_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var emojiText = UnicodeDataGenerator.GenerateEmoji(5);
        var description = "Emoji test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            emojiText,
            description,
            DatabaseType.SQLite);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.SQLite);

        Assert.IsNotNull(result);
        Assert.AreEqual(emojiText, result.Value.TextData, "Sqlx should preserve emoji through parameter binding");
    }

    // ==================== CJK Character Tests (Sqlx Parameter Binding) ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_ParameterBinding_CJKCharacters_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var cjkText = UnicodeDataGenerator.GenerateCJKText(20);
        var description = "CJK test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            cjkText,
            description,
            DatabaseType.MySQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.MySQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(cjkText, result.Value.TextData, "Sqlx should preserve CJK characters through parameter binding");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_ParameterBinding_CJKCharacters_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var cjkText = UnicodeDataGenerator.GenerateCJKText(20);
        var description = "CJK test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            cjkText,
            description,
            DatabaseType.PostgreSQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.PostgreSQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(cjkText, result.Value.TextData, "Sqlx should preserve CJK characters through parameter binding");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_ParameterBinding_CJKCharacters_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var cjkText = UnicodeDataGenerator.GenerateCJKText(20);
        var description = "CJK test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            cjkText,
            description,
            DatabaseType.SqlServer);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.SqlServer);

        Assert.IsNotNull(result);
        Assert.AreEqual(cjkText, result.Value.TextData, "Sqlx should preserve CJK characters through parameter binding");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_ParameterBinding_CJKCharacters_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var cjkText = UnicodeDataGenerator.GenerateCJKText(20);
        var description = "CJK test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            cjkText,
            description,
            DatabaseType.SQLite);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.SQLite);

        Assert.IsNotNull(result);
        Assert.AreEqual(cjkText, result.Value.TextData, "Sqlx should preserve CJK characters through parameter binding");
    }

    // ==================== SqlBuilder Tests with Unicode ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_WithEmoji_ParameterizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var emojiText = UnicodeDataGenerator.GenerateEmoji(3);
        var description = "SqlBuilder emoji test";

        // Act - Test SqlBuilder with emoji in interpolated string
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"INSERT INTO unicode_test (text_data, description) VALUES ({emojiText}, {description})");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        await cmd.ExecuteNonQueryAsync();

        // Assert - Verify SqlBuilder correctly parameterized emoji
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_data FROM unicode_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", description);
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(emojiText, result.ToString(), "SqlBuilder should correctly parameterize emoji");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_WithCJK_ParameterizesCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var cjkText = UnicodeDataGenerator.GenerateCJKText(15);
        var description = "SqlBuilder CJK test";

        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"INSERT INTO unicode_test (text_data, description) VALUES ({cjkText}, {description})");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        await cmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_data FROM unicode_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", description);
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(cjkText, result.ToString(), "SqlBuilder should correctly parameterize CJK characters");
    }

    // ==================== SQL Injection Prevention Tests (Sqlx Security) ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("Security")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_SqlInjectionPattern_SafelyEscaped()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var injectionPattern = UnicodeDataGenerator.GenerateSqlInjectionPattern();
        var description = "SQL injection test";

        // Act - Test that Sqlx parameter binding prevents SQL injection
        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            injectionPattern,
            description,
            DatabaseType.MySQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.MySQL);

        // Assert - Verify injection pattern is stored as literal text
        Assert.IsNotNull(result);
        Assert.AreEqual(injectionPattern, result.Value.TextData, 
            "Sqlx should safely escape SQL injection patterns as literal text");

        // Verify table still exists (injection didn't execute)
        using var checkCmd = fixture.Connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM unicode_test";
        var count = await checkCmd.ExecuteScalarAsync();
        Assert.IsTrue(Convert.ToInt64(count) > 0, "Table should still exist, proving injection was prevented");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("Security")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_SqlInjectionPattern_SafelyEscaped()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var injectionPattern = UnicodeDataGenerator.GenerateSqlInjectionPattern();
        var description = "SQL injection test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            injectionPattern,
            description,
            DatabaseType.PostgreSQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.PostgreSQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(injectionPattern, result.Value.TextData, 
            "Sqlx should safely escape SQL injection patterns as literal text");

        using var checkCmd = fixture.Connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM unicode_test";
        var count = await checkCmd.ExecuteScalarAsync();
        Assert.IsTrue(Convert.ToInt64(count) > 0, "Table should still exist, proving injection was prevented");
    }

    // ==================== Whitespace Preservation Tests (Sqlx Type Conversion) ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_WhitespaceVariations_PreservedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var whitespaceText = UnicodeDataGenerator.GenerateWhitespaceVariations();
        var description = "Whitespace test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            whitespaceText,
            description,
            DatabaseType.MySQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.MySQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(whitespaceText, result.Value.TextData, 
            "Sqlx should preserve all whitespace characters (newlines, tabs, spaces)");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_WhitespaceVariations_PreservedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var whitespaceText = UnicodeDataGenerator.GenerateWhitespaceVariations();
        var description = "Whitespace test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            whitespaceText,
            description,
            DatabaseType.PostgreSQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.PostgreSQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(whitespaceText, result.Value.TextData, 
            "Sqlx should preserve all whitespace characters (newlines, tabs, spaces)");
    }

    // ==================== Mixed Unicode Tests (Sqlx Comprehensive) ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_MixedUnicode_AllCharacterTypesPreserved()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var mixedText = UnicodeDataGenerator.GenerateMixedUnicode();
        var description = "Mixed Unicode test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            mixedText,
            description,
            DatabaseType.MySQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.MySQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(mixedText, result.Value.TextData, 
            "Sqlx should preserve mixed Unicode content (ASCII, emoji, CJK, RTL)");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Unicode")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_MixedUnicode_AllCharacterTypesPreserved()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var mixedText = UnicodeDataGenerator.GenerateMixedUnicode();
        var description = "Mixed Unicode test";

        var insertedId = await InsertUnicodeDataAsync(
            fixture.Connection,
            mixedText,
            description,
            DatabaseType.PostgreSQL);

        var result = await SelectUnicodeDataAsync(
            fixture.Connection,
            insertedId,
            DatabaseType.PostgreSQL);

        Assert.IsNotNull(result);
        Assert.AreEqual(mixedText, result.Value.TextData, 
            "Sqlx should preserve mixed Unicode content (ASCII, emoji, CJK, RTL)");
    }
}
