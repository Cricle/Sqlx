using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Delete entity.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DeleteBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private long _nextId;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _nextId = 1;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        // Seed fresh data for each iteration
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users";
        cmd.ExecuteNonQuery();
        DatabaseSetup.SeedData(_connection, 1000);
        _nextId = 1;
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx")]
    public async Task<int> Sqlx_Delete()
    {
        return await _sqlxRepo.DeleteAsync(_nextId++, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<int> DapperAot_Delete()
    {
        return await _connection.ExecuteAsync(
            "DELETE FROM users WHERE id = @id",
            new { id = _nextId++ });
    }
}
