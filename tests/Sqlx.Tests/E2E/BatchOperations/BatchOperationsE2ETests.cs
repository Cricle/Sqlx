// <copyright file="BatchOperationsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Tests.E2E.Models;
using System.Data.Common;

namespace Sqlx.Tests.E2E.BatchOperations;

/// <summary>
/// E2E tests for batch operations across all supported databases.
/// Tests bulk inserts, updates, and deletes with various batch sizes.
/// </summary>
[TestClass]
public class BatchOperationsE2ETests : E2ETestBase
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
                    created_at DATETIME NOT NULL,
                    is_active TINYINT(1) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    is_active INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task<int> BatchInsertUsersAsync(DbConnection connection, List<E2ETestUser> users, DatabaseType dbType)
    {
        int totalInserted = 0;

        foreach (var user in users)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active)";

            AddParameter(cmd, "@name", user.Name);
            AddParameter(cmd, "@age", user.Age);
            AddParameter(cmd, "@email", user.Email);
            AddParameter(cmd, "@created_at", FormatDateTime(user.CreatedAt, dbType));
            AddParameter(cmd, "@is_active", FormatBoolean(user.IsActive, dbType));

            totalInserted += await cmd.ExecuteNonQueryAsync();
        }

        return totalInserted;
    }

    private static async Task<long> CountUsersAsync(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_users";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static async Task<List<E2ETestUser>> SelectAllUsersAsync(DbConnection connection, DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, age, email, created_at, is_active FROM test_users ORDER BY id";

        var users = new List<E2ETestUser>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new E2ETestUser
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2),
                Email = reader.GetString(3),
                CreatedAt = ParseDateTime(reader.GetValue(4), dbType),
                IsActive = ParseBoolean(reader.GetValue(5), dbType),
            });
        }

        return users;
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    private static object FormatDateTime(DateTime dt, DatabaseType dbType)
    {
        return dbType == DatabaseType.SQLite
            ? dt.ToString("yyyy-MM-dd HH:mm:ss")
            : dt;
    }

    private static DateTime ParseDateTime(object value, DatabaseType dbType)
    {
        return dbType == DatabaseType.SQLite
            ? DateTime.Parse(value.ToString()!)
            : Convert.ToDateTime(value);
    }

    private static object FormatBoolean(bool value, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => value ? 1 : 0,
            DatabaseType.SQLite => value ? 1 : 0,
            _ => value,
        };
    }

    private static bool ParseBoolean(object value, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => Convert.ToInt32(value) != 0,
            DatabaseType.SQLite => Convert.ToInt32(value) != 0,
            _ => Convert.ToBoolean(value),
        };
    }

    private List<E2ETestUser> GenerateRandomUsers(int count)
    {
        var users = new List<E2ETestUser>();
        for (int i = 0; i < count; i++)
        {
            users.Add(new E2ETestUser
            {
                Name = DataGenerator.GenerateString(5, 50),
                Age = DataGenerator.GenerateInt(18, 100),
                Email = $"{DataGenerator.GenerateString(5, 20)}@example.com",
                CreatedAt = DataGenerator.GenerateDateTime(DateTime.Now.AddYears(-5), DateTime.Now),
                IsActive = DataGenerator.GenerateBool(),
            });
        }
        return users;
    }

    // ==================== MySQL Batch Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_BatchInsert_SmallBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var users = GenerateRandomUsers(10);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(10, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(10, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_BatchInsert_MediumBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var users = GenerateRandomUsers(50);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(50, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(50, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_BatchInsert_LargeBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var users = GenerateRandomUsers(100);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(100, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(100, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_BatchUpdate_UpdatesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var users = GenerateRandomUsers(20);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.MySQL);

        // Act - Update all users to age 99
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "UPDATE test_users SET age = 99";
        var updated = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(20, updated);

        var allUsers = await SelectAllUsersAsync(fixture.Connection, DatabaseType.MySQL);
        Assert.IsTrue(allUsers.All(u => u.Age == 99));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_BatchDelete_DeletesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var users = GenerateRandomUsers(30);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.MySQL);

        // Act - Delete users with age >= 50
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM test_users WHERE age >= 50";
        var deleted = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.IsTrue(deleted > 0);
        var remaining = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(30 - deleted, remaining);
    }

    // ==================== PostgreSQL Batch Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_BatchInsert_SmallBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var users = GenerateRandomUsers(10);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(10, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(10, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_BatchInsert_MediumBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var users = GenerateRandomUsers(50);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(50, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(50, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_BatchInsert_LargeBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var users = GenerateRandomUsers(100);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(100, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(100, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_BatchUpdate_UpdatesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var users = GenerateRandomUsers(20);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "UPDATE test_users SET age = 99";
        var updated = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(20, updated);

        var allUsers = await SelectAllUsersAsync(fixture.Connection, DatabaseType.PostgreSQL);
        Assert.IsTrue(allUsers.All(u => u.Age == 99));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_BatchDelete_DeletesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var users = GenerateRandomUsers(30);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM test_users WHERE age >= 50";
        var deleted = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.IsTrue(deleted > 0);
        var remaining = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(30 - deleted, remaining);
    }

    // ==================== SQL Server Batch Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_BatchInsert_SmallBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var users = GenerateRandomUsers(10);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(10, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(10, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_BatchInsert_MediumBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var users = GenerateRandomUsers(50);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(50, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(50, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_BatchInsert_LargeBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var users = GenerateRandomUsers(100);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(100, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(100, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_BatchUpdate_UpdatesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var users = GenerateRandomUsers(20);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "UPDATE test_users SET age = 99";
        var updated = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(20, updated);

        var allUsers = await SelectAllUsersAsync(fixture.Connection, DatabaseType.SqlServer);
        Assert.IsTrue(allUsers.All(u => u.Age == 99));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_BatchDelete_DeletesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var users = GenerateRandomUsers(30);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM test_users WHERE age >= 50";
        var deleted = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.IsTrue(deleted > 0);
        var remaining = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(30 - deleted, remaining);
    }

    // ==================== SQLite Batch Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchInsert_SmallBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var users = GenerateRandomUsers(10);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(10, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(10, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchInsert_MediumBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var users = GenerateRandomUsers(50);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(50, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(50, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchInsert_LargeBatch_InsertsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var users = GenerateRandomUsers(100);

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(100, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(100, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchUpdate_UpdatesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var users = GenerateRandomUsers(20);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "UPDATE test_users SET age = 99";
        var updated = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(20, updated);

        var allUsers = await SelectAllUsersAsync(fixture.Connection, DatabaseType.SQLite);
        Assert.IsTrue(allUsers.All(u => u.Age == 99));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchDelete_DeletesMultipleRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var users = GenerateRandomUsers(30);
        await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM test_users WHERE age >= 50";
        var deleted = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.IsTrue(deleted > 0);
        var remaining = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(30 - deleted, remaining);
    }

    // ==================== Edge Case: Empty Batch ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BatchOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchInsert_EmptyBatch_InsertsNothing()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var users = new List<E2ETestUser>();

        // Act
        var inserted = await BatchInsertUsersAsync(fixture.Connection, users, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(0, inserted);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }
}
