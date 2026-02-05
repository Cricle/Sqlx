using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// ResultReader Performance Benchmark: Generic vs Optimized ResultReader.
/// Tests the performance improvement from optimized ResultReader strategies.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ResultReaderBenchmark
{
    private const string SelectAllSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
    private const string SelectByIdSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id";
    private const string SelectByMinAgeSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE age >= @minAge LIMIT @limit";
    
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    
    [Params(100, 1000)]
    public int RowCount { get; set; }
    
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
    
    // ==================== Strategy A: Direct Index Access ====================
    
    [Benchmark(Description = "Sqlx Optimized (Strategy A) - GetById")]
    public async Task<BenchmarkUser?> Sqlx_Optimized_GetById()
    {
        // Uses optimized ResultReader with direct index access (Strategy A)
        // SQL: SELECT {{columns}} FROM {{table}} WHERE id = @id
        return await _sqlxRepo.GetByIdAsync(5000, default);
    }
    
    [Benchmark(Description = "Dapper - GetById")]
    public async Task<BenchmarkUser?> Dapper_GetById()
    {
        return await _connection.QueryFirstOrDefaultAsync<BenchmarkUser>(SelectByIdSql, new { id = 5000L });
    }
    
    [Benchmark(Description = "Sqlx Optimized (Strategy A) - GetByMinAge")]
    public async Task<List<BenchmarkUser>> Sqlx_Optimized_GetByMinAge()
    {
        // Uses optimized ResultReader with direct index access (Strategy A)
        // SQL: SELECT {{columns}} FROM {{table}} WHERE age >= @minAge LIMIT @limit
        return await _sqlxRepo.GetByMinAgeAsync(25, RowCount, default);
    }
    
    [Benchmark(Description = "Dapper - GetByMinAge")]
    public async Task<List<BenchmarkUser>> Dapper_GetByMinAge()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(SelectByMinAgeSql, new { minAge = 25, limit = RowCount });
        return result.ToList();
    }
    
    // ==================== Strategy B: Cached Ordinals ====================
    
    [Benchmark(Description = "Sqlx Optimized (Strategy B) - GetFirstWhere")]
    public async Task<BenchmarkUser?> Sqlx_Optimized_GetFirstWhere()
    {
        // Uses optimized ResultReader with cached ordinals (Strategy B)
        // SQL: SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} LIMIT 1
        return await _sqlxRepo.GetFirstWhereAsync(u => u.Age > 30, default);
    }
    
    [Benchmark(Description = "Sqlx Optimized (Strategy B) - GetWhere")]
    public async Task<List<BenchmarkUser>> Sqlx_Optimized_GetWhere()
    {
        // Uses optimized ResultReader with cached ordinals (Strategy B)
        // SQL: SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} LIMIT @limit
        return await _sqlxRepo.GetWhereAsync(u => u.Age > 30, RowCount, default);
    }
    
    [Benchmark(Description = "Sqlx Optimized (Strategy B) - GetPaged")]
    public async Task<List<BenchmarkUser>> Sqlx_Optimized_GetPaged()
    {
        // Uses optimized ResultReader with cached ordinals (Strategy B)
        // SQL: SELECT {{columns}} FROM {{table}} LIMIT @pageSize OFFSET @offset
        return await _sqlxRepo.GetPagedAsync(RowCount, 0, default);
    }
    
    // ==================== Baseline: Generic ResultReader ====================
    
    [Benchmark(Baseline = true, Description = "Dapper (Baseline)")]
    public async Task<List<BenchmarkUser>> Dapper_GetAll()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(SelectAllSql, new { limit = RowCount });
        return result.ToList();
    }
}
