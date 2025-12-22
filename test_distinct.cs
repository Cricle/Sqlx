using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FullFeatureDemo;

class Program
{
    static async Task Main()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // Create table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    balance REAL NOT NULL,
                    created_at TEXT NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1
                );
            ";
            cmd.ExecuteNonQuery();
        }
        
        var repo = new UserRepository(connection);
        
        // Insert test data
        await repo.InsertAsync("User1", "user1@test.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("User2", "user2@test.com", 25, 2000m, DateTime.Now, true);
        await repo.InsertAsync("User3", "user3@test.com", 30, 3000m, DateTime.Now, true);
        
        // Test distinct
        var ages = await repo.GetDistinctAgesAsync();
        
        Console.WriteLine($"Count: {ages.Count}");
        Console.WriteLine($"Ages: {string.Join(", ", ages)}");
    }
}
