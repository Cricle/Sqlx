using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Update entity.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class UpdateBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private BenchmarkUser _testUser = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _testUser = _sqlxRepo.GetByIdAsync(5000).GetAwaiter().GetResult()!;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx")]
    public async Task<int> Sqlx_Update()
    {
        _testUser.Score++;
        _testUser.UpdatedAt = DateTime.UtcNow;
        return await _sqlxRepo.UpdateAsync(_testUser);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<int> DapperAot_Update()
    {
        _testUser.Score++;
        _testUser.UpdatedAt = DateTime.UtcNow;
        var updateUser = new DapperUpdateUser
        {
            id = _testUser.Id,
            name = _testUser.Name,
            email = _testUser.Email,
            age = _testUser.Age,
            is_active = _testUser.IsActive ? 1 : 0,
            created_at = _testUser.CreatedAt.ToString("O"),
            updated_at = _testUser.UpdatedAt?.ToString("O"),
            balance = (double)_testUser.Balance,
            description = _testUser.Description,
            score = _testUser.Score
        };
        return await _connection.ExecuteAsync(
            "UPDATE users SET name = @name, email = @email, age = @age, is_active = @is_active, " +
            "created_at = @created_at, updated_at = @updated_at, balance = @balance, " +
            "description = @description, score = @score WHERE id = @id",
            updateUser);
    }
}

/// <summary>
/// Dapper.AOT update entity.
/// </summary>
public class DapperUpdateUser
{
    public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int age { get; set; }
    public int is_active { get; set; }
    public string created_at { get; set; } = string.Empty;
    public string? updated_at { get; set; }
    public double balance { get; set; }
    public string? description { get; set; }
    public int score { get; set; }
}
