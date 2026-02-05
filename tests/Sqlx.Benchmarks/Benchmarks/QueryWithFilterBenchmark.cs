using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Query with WHERE filter.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class QueryWithFilterBenchmark
{
    private const string FilterSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE age >= @minAge";
    
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    
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
    public async Task<List<BenchmarkUser>> Sqlx_GetByMinAge()
    {
        return await _sqlxRepo.GetByMinAgeAsync(50, 1000, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<List<BenchmarkUser>> DapperAot_GetByMinAge()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(FilterSql, new { minAge = 50 });
        return result.ToList();
    }
}
