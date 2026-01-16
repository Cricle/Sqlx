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
/// Uses sync methods for fair comparison (no async overhead).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectSingleBenchmark
{
    private const string SelectByIdSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id";
    
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
    public BenchmarkUser? Sqlx_GetById()
    {
        return _sqlxRepo.GetById(_testId);
    }
    
    [Benchmark(Description = "Dapper.AOT")]
    public BenchmarkUser? DapperAot_GetById()
    {
        // Use same entity type as Sqlx for fair comparison
        return _connection.QueryFirstOrDefault<BenchmarkUser>(SelectByIdSql, new { id = _testId });
    }
    
    [Benchmark(Description = "FreeSql")]
    public FreeSqlUser? FreeSql_GetById()
    {
        return _freeSql.Select<FreeSqlUser>().Where(u => u.Id == _testId).First();
    }
}
