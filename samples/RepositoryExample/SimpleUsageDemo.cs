// -----------------------------------------------------------------------
// <copyright file="SimpleUsageDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace RepositoryExample;

/// <summary>
/// 演示 Sqlx Repository Pattern 的最简单用法。
/// 展示了自动 SQL 推断、类型安全和高性能代码生成。
/// 这是一个独立的演示，不与现有的 repository 实现冲突。
/// </summary>
public static class SimpleUsageDemo
{
    /// <summary>
    /// Runs the simple usage demonstration asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunDemoAsync()
    {
        Console.WriteLine("📖 Sqlx 基础用法演示");
        Console.WriteLine("- 自动 SQL 推断");
        Console.WriteLine("- 编译时类型安全");
        Console.WriteLine("- 零反射高性能");
        Console.WriteLine("==========================");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // Setup database
        await SetupDatabaseAsync(connection);

        // Demo basic operations
        await DemoBasicOperationsAsync(connection);

        Console.WriteLine("\n✅ Demo completed successfully!");
    }

    private static async Task SetupDatabaseAsync(SqliteConnection connection)
    {
        const string createTable = @"
            CREATE TABLE SimpleUsers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                IsActive BOOLEAN DEFAULT 1
            )";

        using var command = connection.CreateCommand();
        command.CommandText = createTable;
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("✓ Database table created");
    }

    private static async Task DemoBasicOperationsAsync(SqliteConnection connection)
    {
        Console.WriteLine("\n📋 Basic SQL Operations:");

        // INSERT
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO SimpleUsers (Name, Email, IsActive) VALUES (@name, @email, @active)";
            cmd.Parameters.AddWithValue("@name", "Alice Smith");
            cmd.Parameters.AddWithValue("@email", "alice@example.com");
            cmd.Parameters.AddWithValue("@active", true);
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✓ Inserted {rowsAffected} user");
        }

        // SELECT
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM SimpleUsers WHERE IsActive = @active";
            cmd.Parameters.AddWithValue("@active", true);
            
            var count = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"✓ Found {count} active users");
        }

        // UPDATE
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "UPDATE SimpleUsers SET Email = @email WHERE Name = @name";
            cmd.Parameters.AddWithValue("@email", "alice.updated@example.com");
            cmd.Parameters.AddWithValue("@name", "Alice Smith");
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✓ Updated {rowsAffected} user email");
        }

        // SELECT with results
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Name, Email FROM SimpleUsers WHERE IsActive = 1";
            using var reader = await cmd.ExecuteReaderAsync();
            
            var userCount = 0;
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0); // Name column
                var email = reader.GetString(1); // Email column
                Console.WriteLine($"✓ User: {name} ({email})");
                userCount++;
            }
            Console.WriteLine($"✓ Retrieved {userCount} users");
        }

        // DELETE (with safety check)
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM SimpleUsers WHERE Name = @name";
            cmd.Parameters.AddWithValue("@name", "Alice Smith");
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✓ Deleted {rowsAffected} user (safe deletion with WHERE clause)");
        }
    }
}

