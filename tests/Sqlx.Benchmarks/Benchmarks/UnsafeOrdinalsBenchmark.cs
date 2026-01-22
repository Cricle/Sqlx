using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using System.Collections.Generic;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark comparing safe vs unsafe ordinals access.
/// Tests if using unsafe pointers can eliminate array bounds checking overhead.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class UnsafeOrdinalsBenchmark
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
    
    [Benchmark(Baseline = true, Description = "Safe Array (Current)")]
    public List<BenchmarkUser> SafeArray()
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
    
    [Benchmark(Description = "Unsafe Pointer")]
    public unsafe List<BenchmarkUser> UnsafePointer()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectSql;
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = Limit;
        cmd.Parameters.Add(param);
        
        using var reader = cmd.ExecuteReader();
        var ordinals = BenchmarkUserResultReader.Default.GetOrdinals(reader);
        
        var list = new List<BenchmarkUser>(Limit);
        
        // Pin the array and get a pointer
        fixed (int* pOrdinals = ordinals)
        {
            while (reader.Read())
            {
                var result = new BenchmarkUser();
                result.Id = reader.GetInt64(pOrdinals[0]);
                result.Name = reader.GetString(pOrdinals[1]);
                result.Email = reader.GetString(pOrdinals[2]);
                result.Age = reader.GetInt32(pOrdinals[3]);
                result.IsActive = reader.GetBoolean(pOrdinals[4]);
                result.CreatedAt = reader.GetDateTime(pOrdinals[5]);
                
                var ord6 = pOrdinals[6];
                result.UpdatedAt = reader.IsDBNull(ord6) ? default : reader.GetDateTime(ord6);
                
                result.Balance = reader.GetDecimal(pOrdinals[7]);
                
                var ord8 = pOrdinals[8];
                result.Description = reader.IsDBNull(ord8) ? default : reader.GetString(ord8);
                
                result.Score = reader.GetInt32(pOrdinals[9]);
                
                list.Add(result);
            }
        }
        
        return list;
    }
    
    [Benchmark(Description = "Unsafe Pointer + Local Vars")]
    public unsafe List<BenchmarkUser> UnsafePointerWithLocalVars()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectSql;
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = Limit;
        cmd.Parameters.Add(param);
        
        using var reader = cmd.ExecuteReader();
        var ordinals = BenchmarkUserResultReader.Default.GetOrdinals(reader);
        
        var list = new List<BenchmarkUser>(Limit);
        
        // Pin the array and extract to local variables
        fixed (int* pOrdinals = ordinals)
        {
            // Extract all ordinals to local variables
            var ord0 = pOrdinals[0];
            var ord1 = pOrdinals[1];
            var ord2 = pOrdinals[2];
            var ord3 = pOrdinals[3];
            var ord4 = pOrdinals[4];
            var ord5 = pOrdinals[5];
            var ord6 = pOrdinals[6];
            var ord7 = pOrdinals[7];
            var ord8 = pOrdinals[8];
            var ord9 = pOrdinals[9];
            
            while (reader.Read())
            {
                var result = new BenchmarkUser();
                result.Id = reader.GetInt64(ord0);
                result.Name = reader.GetString(ord1);
                result.Email = reader.GetString(ord2);
                result.Age = reader.GetInt32(ord3);
                result.IsActive = reader.GetBoolean(ord4);
                result.CreatedAt = reader.GetDateTime(ord5);
                result.UpdatedAt = reader.IsDBNull(ord6) ? default : reader.GetDateTime(ord6);
                result.Balance = reader.GetDecimal(ord7);
                result.Description = reader.IsDBNull(ord8) ? default : reader.GetString(ord8);
                result.Score = reader.GetInt32(ord9);
                
                list.Add(result);
            }
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
            list.Add(resultReader.Read(reader, in ordinals));
        }
        return list;
    }
}
