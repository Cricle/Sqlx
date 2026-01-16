using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Raw comparison: Sqlx ResultReader vs Dapper with same entity type.
/// This isolates the ResultReader performance from other factors.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RawSelectListBenchmark
{
    private SqliteConnection _connection = null!;
    private const string Sql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
    
    [Params(100, 1000)]
    public int Limit { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx.ResultReader")]
    public async Task<List<BenchmarkUser>> Sqlx_ResultReader()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = Sql;
        var p = cmd.CreateParameter();
        p.ParameterName = "@limit";
        p.Value = Limit;
        cmd.Parameters.Add(p);
        
        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<BenchmarkUser>(Limit);
        var ordinals = BenchmarkUserResultReader.Default.GetOrdinals(reader);
        while (reader.Read())
        {
            list.Add(BenchmarkUserResultReader.Default.Read(reader, ordinals));
        }
        return list;
    }
    
    [Benchmark(Description = "Dapper.AOT (same types)")]
    public async Task<List<BenchmarkUser>> Dapper_SameTypes()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(Sql, new { limit = Limit });
        return result.ToList();
    }
}
