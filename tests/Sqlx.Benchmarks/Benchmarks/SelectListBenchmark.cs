using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Select list of entities.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectListBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    
    [Params(10, 100, 1000)]
    public int Limit { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx")]
    public async Task<List<BenchmarkUser>> Sqlx_GetAll()
    {
        return await _sqlxRepo.GetAllAsync(Limit, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<List<DapperUser>> DapperAot_GetAll()
    {
        var result = await _connection.QueryAsync<DapperUser>(
            "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit",
            new { limit = Limit });
        return result.ToList();
    }
}
