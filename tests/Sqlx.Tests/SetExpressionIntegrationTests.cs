// <copyright file="SetExpressionIntegrationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// Integration tests for SetExpressionExtensions with actual database operations.
/// </summary>
[TestClass]
public class SetExpressionIntegrationTests
{
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
    }

    private SqliteConnection? _connection;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        // 创建表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                version INTEGER NOT NULL DEFAULT 1,
                is_active INTEGER NOT NULL DEFAULT 1
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ========== 基础集成测试 ==========

    [TestMethod]
    public async Task ToSetClause_WithRealDatabase_GeneratesValidSql()
    {
        // Arrange
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO test_users (name, email, age, version) VALUES (@name, @email, @age, @version)";
        cmd.Parameters.AddWithValue("@name", "John");
        cmd.Parameters.AddWithValue("@email", "john@example.com");
        cmd.Parameters.AddWithValue("@age", 25);
        cmd.Parameters.AddWithValue("@version", 1);
        await cmd.ExecuteNonQueryAsync();

        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Name = "Jane" };
        var setClause = expr.ToSetClause();

        // Act
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = $"UPDATE test_users SET {setClause} WHERE id = 1";
        updateCmd.Parameters.AddWithValue("@p0", "Jane");
        var affected = await updateCmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affected);

        // Verify
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT name FROM test_users WHERE id = 1";
        var name = await selectCmd.ExecuteScalarAsync();
        Assert.AreEqual("Jane", name);
    }

    [TestMethod]
    public async Task ToSetClause_IncrementExpression_WorksWithRealDatabase()
    {
        // Arrange
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO test_users (name, email, age, version) VALUES (@name, @email, @age, @version)";
        cmd.Parameters.AddWithValue("@name", "John");
        cmd.Parameters.AddWithValue("@email", "john@example.com");
        cmd.Parameters.AddWithValue("@age", 25);
        cmd.Parameters.AddWithValue("@version", 1);
        await cmd.ExecuteNonQueryAsync();

        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Version = u.Version + 1 };
        var setClause = expr.ToSetClause();

        // Act
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = $"UPDATE test_users SET {setClause} WHERE id = 1";
        updateCmd.Parameters.AddWithValue("@p0", 1);
        await updateCmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT version FROM test_users WHERE id = 1";
        var version = await selectCmd.ExecuteScalarAsync();
        Assert.AreEqual(2L, version);
    }

    [TestMethod]
    public async Task ToSetClause_StringFunction_WorksWithRealDatabase()
    {
        // Arrange
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO test_users (name, email, age, version) VALUES (@name, @email, @age, @version)";
        cmd.Parameters.AddWithValue("@name", "  John  ");
        cmd.Parameters.AddWithValue("@email", "JOHN@EXAMPLE.COM");
        cmd.Parameters.AddWithValue("@age", 25);
        cmd.Parameters.AddWithValue("@version", 1);
        await cmd.ExecuteNonQueryAsync();

        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Trim(),
            Email = u.Email.ToLower()
        };
        var setClause = expr.ToSetClause();

        // Act
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = $"UPDATE test_users SET {setClause} WHERE id = 1";
        await updateCmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT name, email FROM test_users WHERE id = 1";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual("John", reader.GetString(0));
        Assert.AreEqual("john@example.com", reader.GetString(1));
    }

    [TestMethod]
    public async Task ToSetClause_MathFunction_WorksWithRealDatabase()
    {
        // Arrange
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO test_users (name, email, age, version) VALUES (@name, @email, @age, @version)";
        cmd.Parameters.AddWithValue("@name", "John");
        cmd.Parameters.AddWithValue("@email", "john@example.com");
        cmd.Parameters.AddWithValue("@age", -25);
        cmd.Parameters.AddWithValue("@version", 1);
        await cmd.ExecuteNonQueryAsync();

        Expression<Func<TestUser, TestUser>> expr = u => new TestUser { Age = Math.Abs(u.Age) };
        var setClause = expr.ToSetClause();

        // Act
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = $"UPDATE test_users SET {setClause} WHERE id = 1";
        await updateCmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT age FROM test_users WHERE id = 1";
        var age = await selectCmd.ExecuteScalarAsync();
        Assert.AreEqual(25L, age);
    }

    [TestMethod]
    public async Task ToSetClause_ComplexExpression_WorksWithRealDatabase()
    {
        // Arrange
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO test_users (name, email, age, version) VALUES (@name, @email, @age, @version)";
        cmd.Parameters.AddWithValue("@name", "  john  ");
        cmd.Parameters.AddWithValue("@email", "john@example.com");
        cmd.Parameters.AddWithValue("@age", 25);
        cmd.Parameters.AddWithValue("@version", 1);
        await cmd.ExecuteNonQueryAsync();

        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Trim().ToUpper(),
            Age = u.Age + 5,
            Version = u.Version + 1
        };
        var setClause = expr.ToSetClause();
        var parameters = expr.GetSetParameters();

        // Act
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = $"UPDATE test_users SET {setClause} WHERE id = 1";
        foreach (var param in parameters)
        {
            updateCmd.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
        }
        await updateCmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT name, age, version FROM test_users WHERE id = 1";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual("JOHN", reader.GetString(0));
        Assert.AreEqual(30L, reader.GetInt64(1));
        Assert.AreEqual(2L, reader.GetInt64(2));
    }
}
