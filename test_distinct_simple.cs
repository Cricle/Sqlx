using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITestRepo))]
public partial class TestRepo : ITestRepo
{
    private readonly IDbConnection _connection;
    public TestRepo(IDbConnection connection) => _connection = connection;
}

public interface ITestRepo
{
    [SqlTemplate("SELECT DISTINCT age FROM users ORDER BY age")]
    Task<List<int>> GetDistinctAgesAsync();
    
    [SqlTemplate("SELECT age FROM users")]
    Task<List<int>> GetAllAgesAsync();
}

class Program
{
    static async Task Main()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE users (age INTEGER)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO users VALUES (25), (25), (30)";
            cmd.ExecuteNonQuery();
        }
        
        var repo = new TestRepo(connection);
        
        Console.WriteLine("Testing GetAllAgesAsync...");
        var allAges = await repo.GetAllAgesAsync();
        Console.WriteLine($"All ages: {string.Join(", ", allAges)}");
        
        Console.WriteLine("\nTesting GetDistinctAgesAsync...");
        var distinctAges = await repo.GetDistinctAgesAsync();
        Console.WriteLine($"Distinct ages: {string.Join(", ", distinctAges)}");
    }
}
