using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT: Pagination query.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PaginationBenchmark
{
    private const string PaginationSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @pageSize OFFSET @offset";
    
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    
    [Params(20, 50, 100)]
    public int PageSize { get; set; }
    
    [Params(0, 100, 500)]
    public int Offset { get; set; }
    
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
    public async Task<List<BenchmarkUser>> Sqlx_GetPaged()
    {
        return await _sqlxRepo.GetPagedAsync(PageSize, Offset, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<List<BenchmarkUser>> DapperAot_GetPaged()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(PaginationSql, new { pageSize = PageSize, offset = Offset });
        return result.ToList();
    }
}
