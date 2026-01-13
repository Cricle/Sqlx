using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Select single entity by ID.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectSingleBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private long _testId;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _testId = 5000;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx")]
    public async Task<BenchmarkUser?> Sqlx_GetById()
    {
        return await _sqlxRepo.GetByIdAsync(_testId);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<DapperUser?> DapperAot_GetById()
    {
        return await _connection.QueryFirstOrDefaultAsync<DapperUser>(
            "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id",
            new { id = _testId });
    }
}

/// <summary>
/// Dapper.AOT entity (column names match database).
/// </summary>
public class DapperUser
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
