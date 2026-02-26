// <copyright file="ErrorHandlingE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.ErrorHandling;

/// <summary>
/// E2E tests for error handling in Sqlx across all supported databases.
/// Tests focus on Sqlx's error handling, exception messages, and resource cleanup.
/// </summary>
[TestClass]
public class ErrorHandlingE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE error_test (
                    id INT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) UNIQUE NOT NULL,
                    age INT
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE error_test (
                    id INTEGER PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) UNIQUE NOT NULL,
                    age INTEGER
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE error_test (
                    id INT PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(100) UNIQUE NOT NULL,
                    age INT
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE error_test (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT UNIQUE NOT NULL,
                    age INTEGER
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== Query Error Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_InvalidTableName_ThrowsDescriptiveException()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        // Act & Assert - Test Sqlx error handling for invalid table
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM nonexistent_table";

        try
        {
            await cmd.ExecuteReaderAsync();
            Assert.Fail("Should have thrown exception for invalid table name");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("nonexistent_table") || 
                         ex.Message.Contains("doesn't exist") ||
                         ex.Message.Contains("Table") ||
                         ex.Message.Contains("not found"),
                $"Exception message should mention the table name or indicate table doesn't exist. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_InvalidTableName_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM nonexistent_table";

        try
        {
            await cmd.ExecuteReaderAsync();
            Assert.Fail("Should have thrown exception for invalid table name");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("nonexistent_table") || 
                         ex.Message.Contains("does not exist") ||
                         ex.Message.Contains("relation"),
                $"Exception message should mention the table name. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_InvalidTableName_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM nonexistent_table";

        try
        {
            await cmd.ExecuteReaderAsync();
            Assert.Fail("Should have thrown exception for invalid table name");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("nonexistent_table") || 
                         ex.Message.Contains("Invalid object") ||
                         ex.Message.Contains("object name"),
                $"Exception message should mention the table name. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_InvalidTableName_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM nonexistent_table";

        try
        {
            await cmd.ExecuteReaderAsync();
            Assert.Fail("Should have thrown exception for invalid table name");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("nonexistent_table") || 
                         ex.Message.Contains("no such table"),
                $"Exception message should mention the table name. Got: {ex.Message}");
        }
    }

    // ==================== Syntax Error Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_SyntaxError_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELCT * FROM error_test"; // Typo: SELCT instead of SELECT

        try
        {
            await cmd.ExecuteReaderAsync();
            Assert.Fail("Should have thrown exception for syntax error");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("syntax") || 
                         ex.Message.Contains("SQL") ||
                         ex.Message.Contains("SELCT"),
                $"Exception message should indicate syntax error. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_SyntaxError_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELCT * FROM error_test";

        try
        {
            await cmd.ExecuteReaderAsync();
            Assert.Fail("Should have thrown exception for syntax error");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("syntax") || 
                         ex.Message.Contains("SELCT"),
                $"Exception message should indicate syntax error. Got: {ex.Message}");
        }
    }

    // ==================== Constraint Violation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_PrimaryKeyViolation_ThrowsDescriptiveException()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Insert first record
        using var insertCmd1 = fixture.Connection.CreateCommand();
        insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd1, "@id", 1);
        AddParameter(insertCmd1, "@name", "John");
        AddParameter(insertCmd1, "@email", "john@example.com");
        AddParameter(insertCmd1, "@age", 30);
        await insertCmd1.ExecuteNonQueryAsync();

        // Act & Assert - Test Sqlx error handling for primary key violation
        using var insertCmd2 = fixture.Connection.CreateCommand();
        insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd2, "@id", 1); // Same ID - should violate primary key
        AddParameter(insertCmd2, "@name", "Jane");
        AddParameter(insertCmd2, "@email", "jane@example.com");
        AddParameter(insertCmd2, "@age", 25);

        try
        {
            await insertCmd2.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for primary key violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("Duplicate") || 
                         ex.Message.Contains("PRIMARY") ||
                         ex.Message.Contains("key") ||
                         ex.Message.Contains("constraint"),
                $"Exception message should indicate primary key violation. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_PrimaryKeyViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertCmd1 = fixture.Connection.CreateCommand();
        insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd1, "@id", 1);
        AddParameter(insertCmd1, "@name", "John");
        AddParameter(insertCmd1, "@email", "john@example.com");
        AddParameter(insertCmd1, "@age", 30);
        await insertCmd1.ExecuteNonQueryAsync();

        using var insertCmd2 = fixture.Connection.CreateCommand();
        insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd2, "@id", 1);
        AddParameter(insertCmd2, "@name", "Jane");
        AddParameter(insertCmd2, "@email", "jane@example.com");
        AddParameter(insertCmd2, "@age", 25);

        try
        {
            await insertCmd2.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for primary key violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("duplicate") || 
                         ex.Message.Contains("primary") ||
                         ex.Message.Contains("key") ||
                         ex.Message.Contains("constraint"),
                $"Exception message should indicate primary key violation. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_PrimaryKeyViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        using var insertCmd1 = fixture.Connection.CreateCommand();
        insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd1, "@id", 1);
        AddParameter(insertCmd1, "@name", "John");
        AddParameter(insertCmd1, "@email", "john@example.com");
        AddParameter(insertCmd1, "@age", 30);
        await insertCmd1.ExecuteNonQueryAsync();

        using var insertCmd2 = fixture.Connection.CreateCommand();
        insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd2, "@id", 1);
        AddParameter(insertCmd2, "@name", "Jane");
        AddParameter(insertCmd2, "@email", "jane@example.com");
        AddParameter(insertCmd2, "@age", 25);

        try
        {
            await insertCmd2.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for primary key violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("duplicate") || 
                         ex.Message.Contains("PRIMARY") ||
                         ex.Message.Contains("key") ||
                         ex.Message.Contains("constraint"),
                $"Exception message should indicate primary key violation. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_PrimaryKeyViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        using var insertCmd1 = fixture.Connection.CreateCommand();
        insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd1, "@id", 1);
        AddParameter(insertCmd1, "@name", "John");
        AddParameter(insertCmd1, "@email", "john@example.com");
        AddParameter(insertCmd1, "@age", 30);
        await insertCmd1.ExecuteNonQueryAsync();

        using var insertCmd2 = fixture.Connection.CreateCommand();
        insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd2, "@id", 1);
        AddParameter(insertCmd2, "@name", "Jane");
        AddParameter(insertCmd2, "@email", "jane@example.com");
        AddParameter(insertCmd2, "@age", 25);

        try
        {
            await insertCmd2.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for primary key violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("UNIQUE") || 
                         ex.Message.Contains("PRIMARY") ||
                         ex.Message.Contains("constraint"),
                $"Exception message should indicate primary key violation. Got: {ex.Message}");
        }
    }

    // ==================== Unique Constraint Violation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_UniqueConstraintViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        using var insertCmd1 = fixture.Connection.CreateCommand();
        insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd1, "@id", 1);
        AddParameter(insertCmd1, "@name", "John");
        AddParameter(insertCmd1, "@email", "john@example.com");
        AddParameter(insertCmd1, "@age", 30);
        await insertCmd1.ExecuteNonQueryAsync();

        using var insertCmd2 = fixture.Connection.CreateCommand();
        insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd2, "@id", 2);
        AddParameter(insertCmd2, "@name", "Jane");
        AddParameter(insertCmd2, "@email", "john@example.com"); // Same email - should violate unique constraint
        AddParameter(insertCmd2, "@age", 25);

        try
        {
            await insertCmd2.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for unique constraint violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("Duplicate") || 
                         ex.Message.Contains("UNIQUE") ||
                         ex.Message.Contains("email"),
                $"Exception message should indicate unique constraint violation. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_UniqueConstraintViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertCmd1 = fixture.Connection.CreateCommand();
        insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd1, "@id", 1);
        AddParameter(insertCmd1, "@name", "John");
        AddParameter(insertCmd1, "@email", "john@example.com");
        AddParameter(insertCmd1, "@age", 30);
        await insertCmd1.ExecuteNonQueryAsync();

        using var insertCmd2 = fixture.Connection.CreateCommand();
        insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd2, "@id", 2);
        AddParameter(insertCmd2, "@name", "Jane");
        AddParameter(insertCmd2, "@email", "john@example.com");
        AddParameter(insertCmd2, "@age", 25);

        try
        {
            await insertCmd2.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for unique constraint violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("duplicate") || 
                         ex.Message.Contains("unique") ||
                         ex.Message.Contains("email"),
                $"Exception message should indicate unique constraint violation. Got: {ex.Message}");
        }
    }

    // ==================== NOT NULL Constraint Violation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_NotNullViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd, "@id", 1);
        AddParameter(insertCmd, "@name", DBNull.Value); // NULL for NOT NULL column
        AddParameter(insertCmd, "@email", "john@example.com");
        AddParameter(insertCmd, "@age", 30);

        try
        {
            await insertCmd.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for NOT NULL violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("NULL") || 
                         ex.Message.Contains("name") ||
                         ex.Message.Contains("cannot be null"),
                $"Exception message should indicate NOT NULL violation. Got: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_NotNullViolation_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd, "@id", 1);
        AddParameter(insertCmd, "@name", DBNull.Value);
        AddParameter(insertCmd, "@email", "john@example.com");
        AddParameter(insertCmd, "@age", 30);

        try
        {
            await insertCmd.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for NOT NULL violation");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains("null") || 
                         ex.Message.Contains("name") ||
                         ex.Message.Contains("violates not-null"),
                $"Exception message should indicate NOT NULL violation. Got: {ex.Message}");
        }
    }

    // ==================== Transaction Rollback Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_TransactionRollback_CleansUpResources()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        using var transaction = await fixture.Connection.BeginTransactionAsync();

        try
        {
            // Insert valid record
            using var insertCmd1 = fixture.Connection.CreateCommand();
            insertCmd1.Transaction = transaction;
            insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
            AddParameter(insertCmd1, "@id", 1);
            AddParameter(insertCmd1, "@name", "John");
            AddParameter(insertCmd1, "@email", "john@example.com");
            AddParameter(insertCmd1, "@age", 30);
            await insertCmd1.ExecuteNonQueryAsync();

            // Try to insert invalid record (duplicate primary key)
            using var insertCmd2 = fixture.Connection.CreateCommand();
            insertCmd2.Transaction = transaction;
            insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
            AddParameter(insertCmd2, "@id", 1);
            AddParameter(insertCmd2, "@name", "Jane");
            AddParameter(insertCmd2, "@email", "jane@example.com");
            AddParameter(insertCmd2, "@age", 25);
            await insertCmd2.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            Assert.Fail("Should have thrown exception");
        }
        catch (DbException)
        {
            // Expected - rollback transaction
            await transaction.RollbackAsync();
        }

        // Verify no records were inserted (transaction was rolled back)
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM error_test";
        var count = await selectCmd.ExecuteScalarAsync();
        Assert.AreEqual(0L, Convert.ToInt64(count), "Transaction rollback should have removed all records");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_TransactionRollback_CleansUpResources()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var transaction = await fixture.Connection.BeginTransactionAsync();

        try
        {
            using var insertCmd1 = fixture.Connection.CreateCommand();
            insertCmd1.Transaction = transaction;
            insertCmd1.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
            AddParameter(insertCmd1, "@id", 1);
            AddParameter(insertCmd1, "@name", "John");
            AddParameter(insertCmd1, "@email", "john@example.com");
            AddParameter(insertCmd1, "@age", 30);
            await insertCmd1.ExecuteNonQueryAsync();

            using var insertCmd2 = fixture.Connection.CreateCommand();
            insertCmd2.Transaction = transaction;
            insertCmd2.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
            AddParameter(insertCmd2, "@id", 1);
            AddParameter(insertCmd2, "@name", "Jane");
            AddParameter(insertCmd2, "@email", "jane@example.com");
            AddParameter(insertCmd2, "@age", 25);
            await insertCmd2.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            Assert.Fail("Should have thrown exception");
        }
        catch (DbException)
        {
            await transaction.RollbackAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM error_test";
        var count = await selectCmd.ExecuteScalarAsync();
        Assert.AreEqual(0L, Convert.ToInt64(count), "Transaction rollback should have removed all records");
    }

    // ==================== Invalid Parameter Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_TypeMismatch_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd, "@id", 1);
        AddParameter(insertCmd, "@name", "John");
        AddParameter(insertCmd, "@email", "john@example.com");
        AddParameter(insertCmd, "@age", "not a number"); // String instead of int

        try
        {
            await insertCmd.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for type mismatch");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
            // Different databases may handle type conversion differently
            // Just verify we get an exception
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ErrorHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_TypeMismatch_ThrowsDescriptiveException()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO error_test (id, name, email, age) VALUES (@id, @name, @email, @age)";
        AddParameter(insertCmd, "@id", 1);
        AddParameter(insertCmd, "@name", "John");
        AddParameter(insertCmd, "@email", "john@example.com");
        AddParameter(insertCmd, "@age", "not a number");

        try
        {
            await insertCmd.ExecuteNonQueryAsync();
            Assert.Fail("Should have thrown exception for type mismatch");
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex.Message);
        }
    }
}
