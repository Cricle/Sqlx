// <copyright file="PaginationE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Tests.E2E.Models;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Pagination;

/// <summary>
/// E2E tests for pagination queries across all supported databases.
/// Tests LIMIT/OFFSET, TOP, and ROW_NUMBER() pagination strategies.
/// </summary>
[TestClass]
public class PaginationE2ETests : E2ETestBase
{
    private static string GetTestUsersSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    score INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    score INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    score INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL,
                    score INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, int count, DatabaseType dbType)
    {
        for (int i = 1; i <= count; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, score)
                VALUES (@name, @age, @email, @score)";

            AddParameter(cmd, "@name", $"User{i:D3}");
            AddParameter(cmd, "@age", 20 + (i % 50));
            AddParameter(cmd, "@email", $"user{i}@example.com");
            AddParameter(cmd, "@score", i * 10);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<List<(long Id, string Name, int Score)>> SelectPageAsync(
        DbConnection connection,
        int pageSize,
        int pageNumber,
        DatabaseType dbType)
    {
        var offset = (pageNumber - 1) * pageSize;
        using var cmd = connection.CreateCommand();

        cmd.CommandText = dbType switch
        {
            DatabaseType.MySQL => $"SELECT id, name, score FROM test_users ORDER BY id LIMIT {pageSize} OFFSET {offset}",
            DatabaseType.PostgreSQL => $"SELECT id, name, score FROM test_users ORDER BY id LIMIT {pageSize} OFFSET {offset}",
            DatabaseType.SqlServer => $"SELECT id, name, score FROM test_users ORDER BY id OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
            DatabaseType.SQLite => $"SELECT id, name, score FROM test_users ORDER BY id LIMIT {pageSize} OFFSET {offset}",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        var results = new List<(long Id, string Name, int Score)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        return results;
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== MySQL Pagination Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("MySQL")]
    public async Task MySQL_Pagination_FirstPage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.MySQL);

        // Act
        var page1 = await SelectPageAsync(fixture.Connection, 10, 1, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(10, page1.Count);
        Assert.AreEqual("User001", page1[0].Name);
        Assert.AreEqual("User010", page1[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("MySQL")]
    public async Task MySQL_Pagination_MiddlePage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.MySQL);

        // Act
        var page3 = await SelectPageAsync(fixture.Connection, 10, 3, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(10, page3.Count);
        Assert.AreEqual("User021", page3[0].Name);
        Assert.AreEqual("User030", page3[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("MySQL")]
    public async Task MySQL_Pagination_LastPage_ReturnsRemainingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, 45, DatabaseType.MySQL);

        // Act
        var page5 = await SelectPageAsync(fixture.Connection, 10, 5, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(5, page5.Count);
        Assert.AreEqual("User041", page5[0].Name);
        Assert.AreEqual("User045", page5[4].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("MySQL")]
    public async Task MySQL_Pagination_BeyondLastPage_ReturnsEmpty()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, 30, DatabaseType.MySQL);

        // Act
        var page10 = await SelectPageAsync(fixture.Connection, 10, 10, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(0, page10.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("MySQL")]
    public async Task MySQL_Pagination_LargePageSize_ReturnsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, 25, DatabaseType.MySQL);

        // Act
        var page1 = await SelectPageAsync(fixture.Connection, 100, 1, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(25, page1.Count);
    }

    // ==================== PostgreSQL Pagination Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Pagination_FirstPage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.PostgreSQL);

        // Act
        var page1 = await SelectPageAsync(fixture.Connection, 10, 1, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(10, page1.Count);
        Assert.AreEqual("User001", page1[0].Name);
        Assert.AreEqual("User010", page1[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Pagination_MiddlePage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.PostgreSQL);

        // Act
        var page3 = await SelectPageAsync(fixture.Connection, 10, 3, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(10, page3.Count);
        Assert.AreEqual("User021", page3[0].Name);
        Assert.AreEqual("User030", page3[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Pagination_LastPage_ReturnsRemainingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, 45, DatabaseType.PostgreSQL);

        // Act
        var page5 = await SelectPageAsync(fixture.Connection, 10, 5, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(5, page5.Count);
        Assert.AreEqual("User041", page5[0].Name);
        Assert.AreEqual("User045", page5[4].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Pagination_BeyondLastPage_ReturnsEmpty()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, 30, DatabaseType.PostgreSQL);

        // Act
        var page10 = await SelectPageAsync(fixture.Connection, 10, 10, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(0, page10.Count);
    }

    // ==================== SQL Server Pagination Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Pagination_FirstPage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.SqlServer);

        // Act
        var page1 = await SelectPageAsync(fixture.Connection, 10, 1, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(10, page1.Count);
        Assert.AreEqual("User001", page1[0].Name);
        Assert.AreEqual("User010", page1[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Pagination_MiddlePage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.SqlServer);

        // Act
        var page3 = await SelectPageAsync(fixture.Connection, 10, 3, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(10, page3.Count);
        Assert.AreEqual("User021", page3[0].Name);
        Assert.AreEqual("User030", page3[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Pagination_LastPage_ReturnsRemainingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, 45, DatabaseType.SqlServer);

        // Act
        var page5 = await SelectPageAsync(fixture.Connection, 10, 5, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(5, page5.Count);
        Assert.AreEqual("User041", page5[0].Name);
        Assert.AreEqual("User045", page5[4].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Pagination_BeyondLastPage_ReturnsEmpty()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, 30, DatabaseType.SqlServer);

        // Act
        var page10 = await SelectPageAsync(fixture.Connection, 10, 10, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(0, page10.Count);
    }

    // ==================== SQLite Pagination Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SQLite")]
    public async Task SQLite_Pagination_FirstPage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.SQLite);

        // Act
        var page1 = await SelectPageAsync(fixture.Connection, 10, 1, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(10, page1.Count);
        Assert.AreEqual("User001", page1[0].Name);
        Assert.AreEqual("User010", page1[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SQLite")]
    public async Task SQLite_Pagination_MiddlePage_ReturnsCorrectRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, 50, DatabaseType.SQLite);

        // Act
        var page3 = await SelectPageAsync(fixture.Connection, 10, 3, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(10, page3.Count);
        Assert.AreEqual("User021", page3[0].Name);
        Assert.AreEqual("User030", page3[9].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SQLite")]
    public async Task SQLite_Pagination_LastPage_ReturnsRemainingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, 45, DatabaseType.SQLite);

        // Act
        var page5 = await SelectPageAsync(fixture.Connection, 10, 5, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(5, page5.Count);
        Assert.AreEqual("User041", page5[0].Name);
        Assert.AreEqual("User045", page5[4].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SQLite")]
    public async Task SQLite_Pagination_BeyondLastPage_ReturnsEmpty()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, 30, DatabaseType.SQLite);

        // Act
        var page10 = await SelectPageAsync(fixture.Connection, 10, 10, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(0, page10.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    [TestCategory("SQLite")]
    public async Task SQLite_Pagination_SingleItemPerPage_WorksCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, 10, DatabaseType.SQLite);

        // Act
        var page1 = await SelectPageAsync(fixture.Connection, 1, 1, DatabaseType.SQLite);
        var page5 = await SelectPageAsync(fixture.Connection, 1, 5, DatabaseType.SQLite);
        var page10 = await SelectPageAsync(fixture.Connection, 1, 10, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(1, page1.Count);
        Assert.AreEqual("User001", page1[0].Name);
        
        Assert.AreEqual(1, page5.Count);
        Assert.AreEqual("User005", page5[0].Name);
        
        Assert.AreEqual(1, page10.Count);
        Assert.AreEqual("User010", page10[0].Name);
    }
}
