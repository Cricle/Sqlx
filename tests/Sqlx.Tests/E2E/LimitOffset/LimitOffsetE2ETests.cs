// <copyright file="LimitOffsetE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.LimitOffset;

/// <summary>
/// E2E tests for LIMIT and OFFSET operations.
/// </summary>
[TestClass]
public class LimitOffsetE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_items (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_items (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_items (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        for (int i = 1; i <= 10; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_items (name, value) VALUES (@name, @value)";
            AddParameter(cmd, "@name", $"Item {i}");
            AddParameter(cmd, "@value", i * 10);
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

    // ==================== LIMIT Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("MySQL")]
    public async Task MySQL_Limit_ReturnsTopRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id LIMIT 3";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Item 1", results[0]);
        Assert.AreEqual("Item 2", results[1]);
        Assert.AreEqual("Item 3", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Limit_ReturnsTopRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id LIMIT 3";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Top_ReturnsTopRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TOP 3 name FROM test_items ORDER BY id";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("SQLite")]
    public async Task SQLite_Limit_ReturnsTopRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id LIMIT 3";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
    }

    // ==================== OFFSET Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("MySQL")]
    public async Task MySQL_LimitOffset_SkipsRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Skip first 3, get next 3
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id LIMIT 3 OFFSET 3";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Item 4", results[0]);
        Assert.AreEqual("Item 5", results[1]);
        Assert.AreEqual("Item 6", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_LimitOffset_SkipsRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id LIMIT 3 OFFSET 3";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Item 4", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OffsetFetch_SkipsRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id OFFSET 3 ROWS FETCH NEXT 3 ROWS ONLY";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Item 4", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("LimitOffset")]
    [TestCategory("SQLite")]
    public async Task SQLite_LimitOffset_SkipsRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_items ORDER BY id LIMIT 3 OFFSET 3";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Item 4", results[0]);
    }
}
