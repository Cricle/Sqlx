using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using FreeSql;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT vs FreeSql: Select list of entities.
/// All use the same entity type (BenchmarkUser) for fair comparison.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectListBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private IFreeSql _freeSql = null!;
    
    [Params(10, 100, 1000)]
    public int Limit { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        
        _freeSql = new FreeSqlBuilder()
            .UseConnectionFactory(DataType.Sqlite, () => _connection)
            .UseAutoSyncStructure(false)
            .Build();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _freeSql?.Dispose();
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx")]
    public async Task<List<BenchmarkUser>> Sqlx_GetAll()
    {
        return await _sqlxRepo.GetAllAsync(Limit, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<List<BenchmarkUser>> DapperAot_GetAll()
    {
        // Use same entity type as Sqlx for fair comparison
        var result = await _connection.QueryAsync<BenchmarkUser>(
            "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit",
            new { limit = Limit });
        return result.ToList();
    }
    
    [Benchmark(Description = "FreeSql")]
    public async Task<List<FreeSqlUser>> FreeSql_GetAll()
    {
        return await _freeSql.Select<FreeSqlUser>().Take(Limit).ToListAsync();
    }
}
