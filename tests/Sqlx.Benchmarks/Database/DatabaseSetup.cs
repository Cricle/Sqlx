using Dapper;
using System.Data;

namespace Sqlx.Benchmarks.Database;

public static class DatabaseSetup
{
    public static void InitializeDatabase(IDbConnection connection)
    {
        // Create tables
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER DEFAULT 1,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP
            )");

        // Clear existing data
        connection.Execute("DELETE FROM users");

        // Seed test data (1000 users for realistic benchmarking)
        SeedUsers(connection, 1000);
    }

    private static void SeedUsers(IDbConnection connection, int count)
    {
        using var transaction = connection.BeginTransaction();

        for (int i = 1; i <= count; i++)
        {
            connection.Execute(
                "INSERT INTO users (name, email, age, is_active) VALUES (@Name, @Email, @Age, @IsActive)",
                new
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + (i % 50),
                    IsActive = i % 10 != 0
                },
                transaction);
        }

        transaction.Commit();
    }

    public static void CleanupDatabase(IDbConnection connection)
    {
        connection.Execute("DELETE FROM users WHERE id > 1000");
    }
}

