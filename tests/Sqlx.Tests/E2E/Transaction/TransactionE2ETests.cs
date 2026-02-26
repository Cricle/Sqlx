// <copyright file="TransactionE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Tests.E2E.Models;
using System.Data;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Transaction;

/// <summary>
/// E2E tests for transaction operations across all supported databases.
/// </summary>
[TestClass]
public class TransactionE2ETests : E2ETestBase
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

    private static async Task<long> InsertUserAsync(DbConnection connection, E2ETestUser user, DatabaseType dbType, DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;

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

            using var idCmd = connection.CreateCommand();
            idCmd.Transaction = transaction;
            idCmd.CommandText = dbType == DatabaseType.MySQL
                ? "SELECT LAST_INSERT_ID()"
                : "SELECT last_insert_rowid()";

            var idResult = await idCmd.ExecuteScalarAsync();
            return Convert.ToInt64(idResult);
        }
    }

    private static async Task<E2ETestUser?> SelectUserByIdAsync(DbConnection connection, long id, DatabaseType dbType, DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
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

    private static async Task<int> UpdateUserAsync(DbConnection connection, E2ETestUser user, DatabaseType dbType, DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
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

    private static async Task<int> DeleteUserAsync(DbConnection connection, long id, DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "DELETE FROM test_users WHERE id = @id";
        AddParameter(cmd, "@id", id);

        return await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountUsersAsync(DbConnection connection, DbTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
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

    // ==================== MySQL Transaction Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("MySQL")]
    public async Task MySQL_TransactionCommit_PersistsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL, transaction);
            await transaction.CommitAsync();
        }

        // Assert - verify data persists outside transaction
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(user.Name, retrievedUser.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("MySQL")]
    public async Task MySQL_TransactionRollback_DiscardsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL, transaction);
            await transaction.RollbackAsync();
        }

        // Assert - verify data does not persist outside transaction
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);
        Assert.IsNull(retrievedUser);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("MySQL")]
    public async Task MySQL_TransactionIsolation_UpdatesNotVisibleOutsideTransaction()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Create a second connection to verify isolation
        await using var secondConnection = await fixture.CreateNewConnectionAsync();

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            user.Id = insertedId;
            user.Name = "Updated Name";
            await UpdateUserAsync(fixture.Connection, user, DatabaseType.MySQL, transaction);

            // Assert - verify original value visible from second connection (outside transaction)
            var outsideUser = await SelectUserByIdAsync(secondConnection, insertedId, DatabaseType.MySQL);
            Assert.IsNotNull(outsideUser);
            Assert.AreNotEqual("Updated Name", outsideUser.Name);

            await transaction.CommitAsync();
        }

        // Assert - verify updated value visible after commit
        var finalUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.MySQL);
        Assert.IsNotNull(finalUser);
        Assert.AreEqual("Updated Name", finalUser.Name);
    }

    // ==================== PostgreSQL Transaction Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TransactionCommit_PersistsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL, transaction);
            await transaction.CommitAsync();
        }

        // Assert
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(user.Name, retrievedUser.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TransactionRollback_DiscardsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL, transaction);
            await transaction.RollbackAsync();
        }

        // Assert
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);
        Assert.IsNull(retrievedUser);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TransactionIsolation_UpdatesNotVisibleOutsideTransaction()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Create a second connection to verify isolation
        await using var secondConnection = await fixture.CreateNewConnectionAsync();

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            user.Id = insertedId;
            user.Name = "Updated Name";
            await UpdateUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL, transaction);

            // Assert - verify original value visible from second connection (outside transaction)
            var outsideUser = await SelectUserByIdAsync(secondConnection, insertedId, DatabaseType.PostgreSQL);
            Assert.IsNotNull(outsideUser);
            Assert.AreNotEqual("Updated Name", outsideUser.Name);

            await transaction.CommitAsync();
        }

        // Assert - verify updated value visible after commit
        var finalUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.PostgreSQL);
        Assert.IsNotNull(finalUser);
        Assert.AreEqual("Updated Name", finalUser.Name);
    }

    // ==================== SQL Server Transaction Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TransactionCommit_PersistsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer, transaction);
            await transaction.CommitAsync();
        }

        // Assert
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SqlServer);
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(user.Name, retrievedUser.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TransactionRollback_DiscardsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer, transaction);
            await transaction.RollbackAsync();
        }

        // Assert
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SqlServer);
        Assert.IsNull(retrievedUser);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    // ==================== SQLite Transaction Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SQLite")]
    public async Task SQLite_TransactionCommit_PersistsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite, transaction);
            await transaction.CommitAsync();
        }

        // Assert
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SQLite);
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(user.Name, retrievedUser.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SQLite")]
    public async Task SQLite_TransactionRollback_DiscardsChanges()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();

        // Act
        long insertedId;
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite, transaction);
            await transaction.RollbackAsync();
        }

        // Assert
        var retrievedUser = await SelectUserByIdAsync(fixture.Connection, insertedId, DatabaseType.SQLite);
        Assert.IsNull(retrievedUser);
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    // ==================== Additional Transaction Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("MySQL")]
    public async Task MySQL_TransactionMultipleOperations_AllOrNothing()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL, transaction);
            }
            await transaction.CommitAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TransactionMultipleOperations_AllOrNothing()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL, transaction);
            }
            await transaction.CommitAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TransactionMultipleOperations_AllOrNothing()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer, transaction);
            }
            await transaction.CommitAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SQLite")]
    public async Task SQLite_TransactionMultipleOperations_AllOrNothing()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite, transaction);
            }
            await transaction.CommitAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("MySQL")]
    public async Task MySQL_TransactionRollbackMultipleOperations_NothingPersists()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL, transaction);
            }
            await transaction.RollbackAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TransactionRollbackMultipleOperations_NothingPersists()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL, transaction);
            }
            await transaction.RollbackAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TransactionRollbackMultipleOperations_NothingPersists()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer, transaction);
            }
            await transaction.RollbackAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    [TestCategory("SQLite")]
    public async Task SQLite_TransactionRollbackMultipleOperations_NothingPersists()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));

        // Act
        using (var transaction = await fixture.Connection.BeginTransactionAsync())
        {
            // Insert 3 users
            for (int i = 0; i < 3; i++)
            {
                var user = GenerateRandomUser();
                await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite, transaction);
            }
            await transaction.RollbackAsync();
        }

        // Assert
        var count = await CountUsersAsync(fixture.Connection);
        Assert.AreEqual(0, count);
    }
}
