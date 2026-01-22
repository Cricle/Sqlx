using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using System.Collections.Generic;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark comparing array ordinals vs struct ordinals performance.
/// Tests the raw ResultReader performance without repository overhead.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StructOrdinalsBenchmark
{
    private const string SelectSql = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
    
    private SqliteConnection _connection = null!;
    
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
    
    [Benchmark(Baseline = true, Description = "Array Ordinals")]
    public List<BenchmarkUser> ArrayOrdinals()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectSql;
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = Limit;
        cmd.Parameters.Add(param);
        
        using var reader = cmd.ExecuteReader();
        var resultReader = BenchmarkUserResultReader.Default;
        var ordinals = resultReader.GetOrdinals(reader);
        
        var list = new List<BenchmarkUser>(Limit);
        while (reader.Read())
        {
            list.Add(resultReader.Read(reader, ordinals));
        }
        return list;
    }
    
    [Benchmark(Description = "Struct Ordinals")]
    public List<BenchmarkUser> StructOrdinals()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectSql;
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = Limit;
        cmd.Parameters.Add(param);
        
        using var reader = cmd.ExecuteReader();
        var resultReader = BenchmarkUserResultReader.Default;
        var ordinals = resultReader.GetOrdinalsStruct(reader);
        
        var list = new List<BenchmarkUser>(Limit);
        while (reader.Read())
        {
            // Call the struct ordinals Read method directly
            list.Add(resultReader.Read(reader, in ordinals));
        }
        return list;
    }
    
    [Benchmark(Description = "Struct Ordinals (No Inline)")]
    public List<BenchmarkUser> StructOrdinalsNoInline()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectSql;
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = Limit;
        cmd.Parameters.Add(param);
        
        using var reader = cmd.ExecuteReader();
        var resultReader = BenchmarkUserResultReader.Default;
        var ordinals = resultReader.GetOrdinalsStruct(reader);
        
        var list = new List<BenchmarkUser>(Limit);
        while (reader.Read())
        {
            // Store in variable to prevent inlining
            var ordinalsRef = ordinals;
            list.Add(resultReader.Read(reader, in ordinalsRef));
        }
        return list;
    }
}
