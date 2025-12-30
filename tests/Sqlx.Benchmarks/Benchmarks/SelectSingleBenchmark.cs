using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Database;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[Config(typeof(Config))]
public class SelectSingleBenchmark
{
    private IDbConnection _connection = null!;
    private IUserRepository _sqlxRepo = null!;
    
    private class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(StatisticColumn.P95);
            WithOption(ConfigOptions.DisableOptimizationsValidator, true);
        }
    }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        DatabaseSetup.InitializeDatabase(_connection);
        
        _sqlxRepo = new UserRepository(_connection);
        
        // Warmup
        Dapper_SelectSingle();
        Sqlx_SelectSingle();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Dapper")]
    public User? Dapper_SelectSingle()
    {
        return _connection.QueryFirstOrDefault<User>(
            "SELECT * FROM users WHERE id = @id",
            new { id = 500 });
    }
    
    [Benchmark(Description = "Sqlx")]
    public User? Sqlx_SelectSingle()
    {
        return _sqlxRepo.GetByIdAsync(500).GetAwaiter().GetResult();
    }
}

