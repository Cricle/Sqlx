using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using FreeSql;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT vs FreeSql: Count query.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CountBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private IFreeSql _freeSql = null!;
    
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
    public async Task<long> Sqlx_Count()
    {
        return await _sqlxRepo.CountAsync(default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<long> DapperAot_Count()
    {
        return await _connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");
    }
    
    [Benchmark(Description = "FreeSql")]
    public async Task<long> FreeSql_Count()
    {
        return await _freeSql.Select<FreeSqlUser>().CountAsync();
    }
}
