using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for COUNT queries.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CountBenchmark
{
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
    
    [Benchmark(Baseline = true, Description = "Sqlx Count")]
    public async Task<long> Sqlx_Count()
    {
        return await _sqlxRepo.CountAsync();
    }
    
    [Benchmark(Description = "Dapper ExecuteScalar")]
    public async Task<long> Dapper_Count()
    {
        return await _connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");
    }
    
    [Benchmark(Description = "ADO.NET Manual")]
    public async Task<long> AdoNet_Count()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users";
        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }
}
