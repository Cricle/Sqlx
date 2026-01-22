using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Focused benchmark for large dataset performance analysis.
/// Tests Sqlx with different optimizations to identify bottlenecks.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LargeDataSetBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private const string Sql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
    
    [Params(1000, 5000)]
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
    
    [Benchmark(Baseline = true, Description = "Sqlx.Repository")]
    public List<BenchmarkUser> Sqlx_Repository()
    {
        return _sqlxRepo.GetAll(Limit);
    }
    
    [Benchmark(Description = "Sqlx.Manual")]
    public List<BenchmarkUser> Sqlx_Manual()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = Sql;
        var p = cmd.CreateParameter();
        p.ParameterName = "@limit";
        p.Value = Limit;
        cmd.Parameters.Add(p);
        
        using var reader = cmd.ExecuteReader();
        var list = new List<BenchmarkUser>(Limit);
        var ordinals = BenchmarkUserResultReader.Default.GetOrdinals(reader);
        while (reader.Read())
        {
            list.Add(BenchmarkUserResultReader.Default.Read(reader, ordinals));
        }
        return list;
    }
}
