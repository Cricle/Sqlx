using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FullFeatureDemo;
using System.Reflection;

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
        Console.WriteLine("Inserting test data...");
        await repo.InsertAsync("User1", "user1@test.com", 25, 1000m, DateTime.Now, true);
        await repo.InsertAsync("User2", "user2@test.com", 25, 2000m, DateTime.Now, true);
        await repo.InsertAsync("User3", "user3@test.com", 30, 3000m, DateTime.Now, true);
        
        // Verify data was inserted
        var allUsers = await repo.GetAllAsync();
        Console.WriteLine($"Total users inserted: {allUsers.Count}");
        foreach (var user in allUsers)
        {
            Console.WriteLine($"  - {user.Name}, Age: {user.Age}");
        }
        
        // Test distinct - let's see what SQL is executed
        Console.WriteLine("\nTesting GetDistinctAgesAsync...");
        
        // Use reflection to see the generated method
        var method = typeof(UserRepository).GetMethod("GetDistinctAgesAsync");
        Console.WriteLine($"Method found: {method != null}");
        
        try
        {
            var ages = await repo.GetDistinctAgesAsync();
            Console.WriteLine($"Distinct ages count: {ages.Count}");
            Console.WriteLine($"Distinct ages: {string.Join(", ", ages)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
        
        // Try manual SQL to verify the query works
        Console.WriteLine("\nTrying manual SQL...");
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT DISTINCT age FROM users ORDER BY age";
            using var reader = cmd.ExecuteReader();
            Console.WriteLine("Manual SQL results:");
            while (reader.Read())
            {
                Console.WriteLine($"  Age: {reader.GetInt32(0)}");
            }
        }
    }
}
