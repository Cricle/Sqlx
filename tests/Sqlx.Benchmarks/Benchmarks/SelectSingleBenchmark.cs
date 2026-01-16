using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using FreeSql;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx vs Dapper.AOT vs FreeSql: Select single entity by ID.
/// All use the same entity type (BenchmarkUser) for fair comparison.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectSingleBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private IFreeSql _freeSql = null!;
    private long _testId;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _testId = 5000;
        
        // FreeSql uses same in-memory database via connection string
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
    public async Task<BenchmarkUser?> Sqlx_GetById()
    {
        return await _sqlxRepo.GetByIdAsync(_testId, default);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public async Task<BenchmarkUser?> DapperAot_GetById()
    {
        // Use same entity type as Sqlx for fair comparison
        return await _connection.QueryFirstOrDefaultAsync<BenchmarkUser>(
            "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id",
            new { id = _testId });
    }
    
    [Benchmark(Description = "FreeSql")]
    public async Task<FreeSqlUser?> FreeSql_GetById()
    {
        return await _freeSql.Select<FreeSqlUser>().Where(u => u.Id == _testId).FirstAsync();
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
