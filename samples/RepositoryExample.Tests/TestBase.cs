// -----------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

/// <summary>
/// Base class for all repository tests providing common setup and teardown.
/// </summary>
public abstract class TestBase : IDisposable
{
    private readonly string _databasePath;
    private readonly string _connectionString;
    protected DbConnection Connection { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestBase"/> class.
    /// </summary>
    protected TestBase()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_databasePath}";
        Connection = new SqliteConnection(_connectionString);
    }

    /// <summary>
    /// Sets up the test database with required tables and initial data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task InitializeDatabaseAsync()
    {
        await Connection.OpenAsync();
        
        const string createTableSql = @"
            CREATE TABLE users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            
            CREATE INDEX idx_users_email ON users(Email);
            CREATE INDEX idx_users_name ON users(Name);
            CREATE INDEX idx_users_created_at ON users(CreatedAt);
        ";

        using var command = Connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task SeedTestDataAsync()
    {
        const string seedSql = @"
            INSERT INTO users (Name, Email, CreatedAt) VALUES 
            ('Test User 1', 'test1@example.com', datetime('now', '-30 days')),
            ('Test User 2', 'test2@example.com', datetime('now', '-15 days')),
            ('Test User 3', 'test3@example.com', datetime('now', '-5 days'));
        ";

        using var command = Connection.CreateCommand();
        command.CommandText = seedSql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates a valid test user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>A valid test user.</returns>
    protected static User CreateValidUser(int id = 0)
    {
        return new User
        {
            Id = id,
            Name = $"Test User {id}",
            Email = $"test{id}@example.com",
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Creates an invalid test user (for validation testing).
    /// </summary>
    /// <returns>An invalid test user.</returns>
    protected static User CreateInvalidUser()
    {
        return new User
        {
            Id = 0,
            Name = "", // Invalid: empty name
            Email = "invalid-email", // Invalid: no @ symbol
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Gets the count of users in the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation containing the user count.</returns>
    protected async Task<int> GetUserCountAsync()
    {
        using var command = Connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM users";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Clears all users from the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ClearUsersAsync()
    {
        using var command = Connection.CreateCommand();
        command.CommandText = "DELETE FROM users";
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    /// <param name="disposing">Whether we are disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection?.Dispose();
            
            // Clean up database file
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
