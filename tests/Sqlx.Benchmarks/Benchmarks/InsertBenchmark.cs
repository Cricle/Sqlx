using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Insert entity (fair comparison - both use single statement with RETURNING).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class InsertBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private int _counter;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _counter = 0;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users";
        cmd.ExecuteNonQuery();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx")]
    public async Task<long> Sqlx_Insert()
    {
        var user = DatabaseSetup.CreateTestUser(_counter++);
        return await _sqlxRepo.InsertAndGetIdAsync(user, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<long> DapperAot_Insert()
    {
        var user = DatabaseSetup.CreateTestUser(_counter++);
        var insertUser = new DapperInsertUser
        {
            name = user.Name,
            email = user.Email,
            age = user.Age,
            is_active = user.IsActive ? 1 : 0,
            created_at = user.CreatedAt.ToString("O"),
            balance = (double)user.Balance,
            score = user.Score
        };
        // Single statement with last_insert_rowid() - same as Sqlx
        return await _connection.ExecuteScalarAsync<long>(
            "INSERT INTO users (name, email, age, is_active, created_at, balance, score) VALUES (@name, @email, @age, @is_active, @created_at, @balance, @score); SELECT last_insert_rowid()",
            insertUser);
    }
}

public class DapperInsertUser
{
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int age { get; set; }
    public int is_active { get; set; }
    public string created_at { get; set; } = string.Empty;
    public double balance { get; set; }
    public int score { get; set; }
}
