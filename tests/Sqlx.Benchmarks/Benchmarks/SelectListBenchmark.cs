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
public class SelectListBenchmark
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
    
    [Params(10, 100)]
    public int RowCount;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        DatabaseSetup.InitializeDatabase(_connection);
        
        _sqlxRepo = new UserRepository(_connection);
        
        // Warmup
        Dapper_SelectList();
        Sqlx_SelectList();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Dapper")]
    public List<User> Dapper_SelectList()
    {
        return _connection.Query<User>(
            "SELECT * FROM users WHERE age > @minAge LIMIT @limit",
            new { minAge = 20, limit = RowCount }).ToList();
    }
    
    [Benchmark(Description = "Sqlx")]
    public List<User> Sqlx_SelectList()
    {
        return _sqlxRepo.GetByAgeAsync(20, RowCount).GetAwaiter().GetResult();
    }
}

