using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark comparing static ordinals vs dynamic ordinals performance.
/// Static ordinals skip GetOrdinal() calls at runtime.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StaticOrdinalsBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserResultReader _reader = null!;
    private int[] _staticOrdinals = null!;
    
    [Params(10, 100, 1000)]
    public int RowCount { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _reader = BenchmarkUserResultReader.Default;
        
        // Pre-compute static ordinals (simulating what generator does for {{columns}})
        // Order matches BenchmarkUserEntityProvider.Columns
        _staticOrdinals = Enumerable.Range(0, BenchmarkUserEntityProvider.Default.Columns.Count).ToArray();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Static Ordinals")]
    public async Task<List<BenchmarkUser>> WithStaticOrdinals()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = RowCount;
        cmd.Parameters.Add(param);
        
        using var reader = await cmd.ExecuteReaderAsync();
        return await _reader.ToListAsync(reader, _staticOrdinals);
    }
    
    [Benchmark(Description = "Dynamic Ordinals")]
    public async Task<List<BenchmarkUser>> WithDynamicOrdinals()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = RowCount;
        cmd.Parameters.Add(param);
        
        using var reader = await cmd.ExecuteReaderAsync();
        return await _reader.ToListAsync(reader);
    }
}

/// <summary>
/// Benchmark for FirstOrDefault with static vs dynamic ordinals.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StaticOrdinalsFirstOrDefaultBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserResultReader _reader = null!;
    private int[] _staticOrdinals = null!;
    private long _testId;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _reader = BenchmarkUserResultReader.Default;
        _staticOrdinals = Enumerable.Range(0, BenchmarkUserEntityProvider.Default.Columns.Count).ToArray();
        _testId = 5000;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Static Ordinals")]
    public async Task<BenchmarkUser?> WithStaticOrdinals()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = _testId;
        cmd.Parameters.Add(param);
        
        using var reader = await cmd.ExecuteReaderAsync();
        return await _reader.FirstOrDefaultAsync(reader, _staticOrdinals);
    }
    
    [Benchmark(Description = "Dynamic Ordinals")]
    public async Task<BenchmarkUser?> WithDynamicOrdinals()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = _testId;
        cmd.Parameters.Add(param);
        
        using var reader = await cmd.ExecuteReaderAsync();
        return await _reader.FirstOrDefaultAsync(reader);
    }
}
