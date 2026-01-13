using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks;

/// <summary>
/// Database setup and seeding for benchmarks.
/// </summary>
public static class DatabaseSetup
{
    public const string ConnectionString = "Data Source=:memory:";
    
    public static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }
    
    public static void InitializeDatabase(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT,
                balance REAL NOT NULL DEFAULT 0,
                description TEXT,
                score INTEGER NOT NULL DEFAULT 0
            );
            
            CREATE INDEX IF NOT EXISTS idx_users_age ON users(age);
            CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);
            CREATE INDEX IF NOT EXISTS idx_users_name ON users(name);
            """;
        cmd.ExecuteNonQuery();
    }
    
    public static void SeedData(SqliteConnection connection, int count)
    {
        using var transaction = connection.BeginTransaction();
        
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT INTO users (name, email, age, is_active, created_at, balance, description, score)
            VALUES (@name, @email, @age, @isActive, @createdAt, @balance, @description, @score)
            """;
        
        var nameParam = cmd.CreateParameter();
        nameParam.ParameterName = "@name";
        cmd.Parameters.Add(nameParam);
        
        var emailParam = cmd.CreateParameter();
        emailParam.ParameterName = "@email";
        cmd.Parameters.Add(emailParam);
        
        var ageParam = cmd.CreateParameter();
        ageParam.ParameterName = "@age";
        cmd.Parameters.Add(ageParam);
        
        var isActiveParam = cmd.CreateParameter();
        isActiveParam.ParameterName = "@isActive";
        cmd.Parameters.Add(isActiveParam);
        
        var createdAtParam = cmd.CreateParameter();
        createdAtParam.ParameterName = "@createdAt";
        cmd.Parameters.Add(createdAtParam);
        
        var balanceParam = cmd.CreateParameter();
        balanceParam.ParameterName = "@balance";
        cmd.Parameters.Add(balanceParam);
        
        var descriptionParam = cmd.CreateParameter();
        descriptionParam.ParameterName = "@description";
        cmd.Parameters.Add(descriptionParam);
        
        var scoreParam = cmd.CreateParameter();
        scoreParam.ParameterName = "@score";
        cmd.Parameters.Add(scoreParam);
        
        var random = new Random(42); // Fixed seed for reproducibility
        var baseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        for (int i = 0; i < count; i++)
        {
            nameParam.Value = $"User{i:D6}";
            emailParam.Value = $"user{i}@example.com";
            ageParam.Value = random.Next(18, 80);
            isActiveParam.Value = random.Next(100) < 80 ? 1 : 0; // 80% active
            createdAtParam.Value = baseDate.AddDays(random.Next(0, 1000)).ToString("O");
            balanceParam.Value = Math.Round(random.NextDouble() * 10000, 2);
            descriptionParam.Value = i % 3 == 0 ? $"Description for user {i}" : DBNull.Value;
            scoreParam.Value = random.Next(0, 1000);
            
            cmd.ExecuteNonQuery();
        }
        
        transaction.Commit();
    }
    
    public static BenchmarkUser CreateTestUser(int index = 0)
    {
        return new BenchmarkUser
        {
            Name = $"TestUser{index:D6}",
            Email = $"test{index}@example.com",
            Age = 25 + (index % 50),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Balance = 1000.50m + index,
            Description = $"Test description {index}",
            Score = index * 10
        };
    }
}
