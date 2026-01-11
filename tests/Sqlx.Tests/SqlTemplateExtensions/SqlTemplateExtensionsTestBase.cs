using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Sqlx.Tests.SqlTemplateExtensions;

/// <summary>
/// Base class for SqlTemplate extension method tests.
/// Provides in-memory SQLite database for testing.
/// </summary>
public abstract class SqlTemplateExtensionsTestBase : IDisposable
{
    protected DbConnection Connection { get; }
    protected const string TestTableName = "Users";

    protected SqlTemplateExtensionsTestBase()
    {
        // Create in-memory SQLite database
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        // Create test table
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Age INTEGER NOT NULL,
                Email TEXT,
                CreatedAt TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        // Insert test data
        InsertTestData();
    }

    private void InsertTestData()
    {
        var users = new[]
        {
            (1, "Alice", 25, "alice@example.com", "2024-01-01"),
            (2, "Bob", 30, "bob@example.com", "2024-01-02"),
            (3, "Charlie", 35, null, "2024-01-03"),
            (4, "David", 40, "david@example.com", "2024-01-04"),
            (5, "Eve", 28, "eve@example.com", "2024-01-05")
        };

        foreach (var (id, name, age, email, createdAt) in users)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Users (Id, Name, Age, Email, CreatedAt) VALUES (@id, @name, @age, @email, @createdAt)";
            cmd.Parameters.Add(new SqliteParameter("@id", id));
            cmd.Parameters.Add(new SqliteParameter("@name", name));
            cmd.Parameters.Add(new SqliteParameter("@age", age));
            cmd.Parameters.Add(new SqliteParameter("@email", email ?? (object)DBNull.Value));
            cmd.Parameters.Add(new SqliteParameter("@createdAt", createdAt));
            cmd.ExecuteNonQuery();
        }
    }

    protected SqlTemplate CreateSimpleTemplate(string sql, Dictionary<string, object?>? parameters = null)
    {
        return new SqlTemplate(sql, parameters ?? new Dictionary<string, object?>());
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}
