// <copyright file="CrudE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Tests.E2E.Models;
using System.Data.Common;

namespace Sqlx.Tests.E2E.CRUD;

/// <summary>
/// E2E tests for CRUD operations across all supported databases.
/// </summary>
[TestClass]
public class CrudE2ETests : E2ETestBase
{
    /// <summary>
    /// Creates the test_users table schema for the specified database type.
    /// </summary>
    /// <param name="dbType">The database type.</param>
    /// <returns>The DDL statement.</returns>
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

    /// <summary>
    /// Inserts a test user into the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="user">The user to insert.</param>
    /// <param name="dbType">The database type.</param>
    /// <returns>The inserted user ID.</returns>
    private static async Task<long> InsertUserAsync(DbConnection connection, E2ETestUser user, DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();

        if (dbType == DatabaseType.PostgreSQL)
        {
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active)
                RETURNING id";
        }
        else if (dbType == DatabaseType.SqlServer)
        {
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
        }
        else
        {
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active)";
        }

        AddParameter(cmd, "@name", user.Name);
        AddParameter(cmd, "@age", user.Age);
        AddParameter(cmd, "@email", user.Email);
        AddParameter(cmd, "@created_at", FormatDateTime(user.CreatedAt, dbType));
        AddParameter(cmd, "@is_active", FormatBoolean(user.IsActive, dbType));

        if (dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SqlServer)
        {
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        else
        {
            await cmd.ExecuteNonQueryAsync();

            // Get last insert ID
            using var idCmd = connection.CreateCommand();
            idCmd.CommandText = dbType == DatabaseType.MySQL
                ? "SELECT LAST_INSERT_ID()"
                : "SELECT last_insert_rowid()";

            var idResult = await idCmd.ExecuteScalarAsync();
            return Convert.ToInt64(idResult);
        }
    }

    /// <summary>
    /// Selects a user by ID from the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="id">The user ID.</param>
    /// <param name="dbType">The database type.</param>
    /// <returns>The user, or null if not found.</returns>
    private static async Task<E2ETestUser?> SelectUserByIdAsync(DbConnection connection, long id, DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, age, email, created_at, is_active FROM test_users WHERE id = @id";
        AddParameter(cmd, "@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new E2ETestUser
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Age = reader.GetInt32(2),
            Email = reader.GetString(3),
            CreatedAt = ParseDateTime(reader.GetValue(4), dbType),
            IsActive = ParseBoolean(reader.GetValue(5), dbType),
        };
    }

    /// <summary>
    /// Updates a user in the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="user">The user to update.</param>
    /// <param name="dbType">The database type.</param>
    /// <returns>The number of rows affected.</returns>
    private static async Task<int> UpdateUserAsync(DbConnection connection, E2ETestUser user, DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE test_users
            SET name = @name, age = @age, email = @email, created_at = @created_at, is_active = @is_active
            WHERE id = @id";

        AddParameter(cmd, "@id", user.Id);
        AddParameter(cmd, "@name", user.Name);
        AddParameter(cmd, "@age", user.Age);
        AddParameter(cmd, "@email", user.Email);
        AddParameter(cmd, "@created_at", FormatDateTime(user.CreatedAt, dbType));
        AddParameter(cmd, "@is_active", FormatBoolean(user.IsActive, dbType));

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Deletes a user from the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="id">The user ID.</param>
    /// <returns>The number of rows affected.</returns>
    private static async Task<int> DeleteUserAsync(DbConnection connection, long id)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM test_users WHERE id = @id";
        AddParameter(cmd, "@id", id);

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Counts the number of users in the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <returns>The count.</returns>
    private static async Task<long> CountUsersAsync(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_users";

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
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

    /// <summary>
    /// Generates a random test user.
    /// </summary>
    /// <returns>A random test user.</returns>
    private E2ETestUser GenerateRandomUser()
    {
        return new E2ETestUser
        {
            Name = DataGenerator.GenerateString(5, 50),
            Age = DataGenerator.GenerateInt(18, 100),
            Email = $"{DataGenerator.GenerateString(5, 20)}@example.com",
            CreatedAt = DataGenerator.GenerateDateTime(DateTime.Now.AddYears(-5), DateTime.Now),
            IsActive = DataGenerator.GenerateBool(),
        };
    }

    // ==================== MySQL Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_InsertAndSelect_ReturnsCorrectUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();

        // Act
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(insertedId, retrievedUser.Id);
        Assert.AreEqual(user.Name, retrievedUser.Name);
        Assert.AreEqual(user.Age, retrievedUser.Age);
        Assert.AreEqual(user.Email, retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_UpdateUser_ModifiesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Act
        user.Id = insertedId;
        user.Name = "Updated Name";
        user.Age = 99;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.MySQL);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("Updated Name", retrievedUser.Name);
        Assert.AreEqual(99, retrievedUser.Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_DeleteUser_RemovesUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Act
        await DeleteUserAsync(fixture.Connection, insertedId);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_InsertMultipleUsers_AllHaveUniqueIds()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));

        // Act
        var ids = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var user = GenerateRandomUser();
            var id = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);
            ids.Add(id);
        }

        // Assert
        Assert.AreEqual(5, ids.Distinct().Count(), "All IDs should be unique");
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(5, count);
    }

    // ==================== PostgreSQL Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InsertAndSelect_ReturnsCorrectUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();

        // Act
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(insertedId, retrievedUser.Id);
        Assert.AreEqual(user.Name, retrievedUser.Name);
        Assert.AreEqual(user.Age, retrievedUser.Age);
        Assert.AreEqual(user.Email, retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_UpdateUser_ModifiesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Act
        user.Id = insertedId;
        user.Name = "Updated Name";
        user.Age = 99;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("Updated Name", retrievedUser.Name);
        Assert.AreEqual(99, retrievedUser.Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_DeleteUser_RemovesUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Act
        await DeleteUserAsync(fixture.Connection, insertedId);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InsertMultipleUsers_AllHaveUniqueIds()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));

        // Act
        var ids = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var user = GenerateRandomUser();
            var id = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);
            ids.Add(id);
        }

        // Assert
        Assert.AreEqual(5, ids.Distinct().Count(), "All IDs should be unique");
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(5, count);
    }

    // ==================== SQL Server Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InsertAndSelect_ReturnsCorrectUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();

        // Act
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SqlServer);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(insertedId, retrievedUser.Id);
        Assert.AreEqual(user.Name, retrievedUser.Name);
        Assert.AreEqual(user.Age, retrievedUser.Age);
        Assert.AreEqual(user.Email, retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_UpdateUser_ModifiesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);

        // Act
        user.Id = insertedId;
        user.Name = "Updated Name";
        user.Age = 99;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.SqlServer);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SqlServer);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("Updated Name", retrievedUser.Name);
        Assert.AreEqual(99, retrievedUser.Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_DeleteUser_RemovesUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);

        // Act
        await DeleteUserAsync(fixture.Connection, insertedId);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SqlServer);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InsertMultipleUsers_AllHaveUniqueIds()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));

        // Act
        var ids = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var user = GenerateRandomUser();
            var id = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);
            ids.Add(id);
        }

        // Assert
        Assert.AreEqual(5, ids.Distinct().Count(), "All IDs should be unique");
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(5, count);
    }

    // ==================== SQLite Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertAndSelect_ReturnsCorrectUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();

        // Act
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SQLite);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(insertedId, retrievedUser.Id);
        Assert.AreEqual(user.Name, retrievedUser.Name);
        Assert.AreEqual(user.Age, retrievedUser.Age);
        Assert.AreEqual(user.Email, retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_UpdateUser_ModifiesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);

        // Act
        user.Id = insertedId;
        user.Name = "Updated Name";
        user.Age = 99;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.SQLite);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SQLite);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("Updated Name", retrievedUser.Name);
        Assert.AreEqual(99, retrievedUser.Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SQLite")]
    [TestCategory("CRUD")]
    public async Task SQLite_DeleteUser_RemovesUser()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);

        // Act
        await DeleteUserAsync(fixture.Connection, insertedId);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SQLite);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertMultipleUsers_AllHaveUniqueIds()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));

        // Act
        var ids = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var user = GenerateRandomUser();
            var id = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);
            ids.Add(id);
        }

        // Assert
        Assert.AreEqual(5, ids.Distinct().Count(), "All IDs should be unique");
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(5, count);
    }

    // ==================== Additional Comprehensive Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_SelectNonExistentUser_ReturnsNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));

        // Act
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, 99999, DatabaseType.MySQL);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SelectNonExistentUser_ReturnsNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));

        // Act
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, 99999, DatabaseType.PostgreSQL);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SelectNonExistentUser_ReturnsNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));

        // Act
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, 99999, DatabaseType.SqlServer);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_SelectNonExistentUser_ReturnsNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));

        // Act
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, 99999, DatabaseType.SQLite);

        // Assert
        Assert.IsNull(retrievedUser);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_UpdateNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        user.Id = 99999;

        // Act
        var affectedRows = await UpdateUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_UpdateNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        user.Id = 99999;

        // Act
        var affectedRows = await UpdateUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_UpdateNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();
        user.Id = 99999;

        // Act
        var affectedRows = await UpdateUserAsync(fixture.Connection, user, DatabaseType.SqlServer);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_UpdateNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();
        user.Id = 99999;

        // Act
        var affectedRows = await UpdateUserAsync(fixture.Connection, user, DatabaseType.SQLite);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_DeleteNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));

        // Act
        var affectedRows = await DeleteUserAsync(fixture.Connection, 99999);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_DeleteNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));

        // Act
        var affectedRows = await DeleteUserAsync(fixture.Connection, 99999);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_DeleteNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));

        // Act
        var affectedRows = await DeleteUserAsync(fixture.Connection, 99999);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_DeleteNonExistentUser_ReturnsZeroRowsAffected()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));

        // Act
        var affectedRows = await DeleteUserAsync(fixture.Connection, 99999);

        // Assert
        Assert.AreEqual(0, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("MySQL")]
    public async Task MySQL_UpdateAllFields_AllFieldsModified()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Act - Update all fields
        user.Id = insertedId;
        user.Name = "New Name";
        user.Age = 50;
        user.Email = "newemail@example.com";
        user.CreatedAt = DateTime.Now.AddDays(-10);
        user.IsActive = !user.IsActive;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.MySQL);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("New Name", retrievedUser.Name);
        Assert.AreEqual(50, retrievedUser.Age);
        Assert.AreEqual("newemail@example.com", retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_UpdateAllFields_AllFieldsModified()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Act - Update all fields
        user.Id = insertedId;
        user.Name = "New Name";
        user.Age = 50;
        user.Email = "newemail@example.com";
        user.CreatedAt = DateTime.Now.AddDays(-10);
        user.IsActive = !user.IsActive;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("New Name", retrievedUser.Name);
        Assert.AreEqual(50, retrievedUser.Age);
        Assert.AreEqual("newemail@example.com", retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_UpdateAllFields_AllFieldsModified()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);

        // Act - Update all fields
        user.Id = insertedId;
        user.Name = "New Name";
        user.Age = 50;
        user.Email = "newemail@example.com";
        user.CreatedAt = DateTime.Now.AddDays(-10);
        user.IsActive = !user.IsActive;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.SqlServer);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SqlServer);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("New Name", retrievedUser.Name);
        Assert.AreEqual(50, retrievedUser.Age);
        Assert.AreEqual("newemail@example.com", retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    [TestCategory("SQLite")]
    public async Task SQLite_UpdateAllFields_AllFieldsModified()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);

        // Act - Update all fields
        user.Id = insertedId;
        user.Name = "New Name";
        user.Age = 50;
        user.Email = "newemail@example.com";
        user.CreatedAt = DateTime.Now.AddDays(-10);
        user.IsActive = !user.IsActive;
        await UpdateUserAsync(fixture.Connection, user, DatabaseType.SQLite);
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SQLite);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("New Name", retrievedUser.Name);
        Assert.AreEqual(50, retrievedUser.Age);
        Assert.AreEqual("newemail@example.com", retrievedUser.Email);
        Assert.AreEqual(user.IsActive, retrievedUser.IsActive);
    }
}
