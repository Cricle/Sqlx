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
public class BatchInsertBenchmark
{
    private IDbConnection _connection = null!;
    private IUserRepository _sqlxRepo = null!;
    private List<User> _users = null!;

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

        _users = Enumerable.Range(1, RowCount)
            .Select(i => new User
            {
                Name = $"BatchUser{i}",
                Email = $"batch{i}@test.com",
                Age = 25 + i % 30,
                IsActive = true
            })
            .ToList();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clean up inserted data before each iteration
        DatabaseSetup.CleanupDatabase(_connection);
    }

    [Benchmark(Baseline = true, Description = "Dapper (Individual)")]
    public int Dapper_BatchInsert_Individual()
    {
        using var transaction = _connection.BeginTransaction();
        var affected = 0;

        foreach (var user in _users)
        {
            affected += _connection.Execute(
                "INSERT INTO users (name, email, age, is_active) VALUES (@Name, @Email, @Age, @IsActive)",
                user,
                transaction);
        }

        transaction.Commit();
        return affected;
    }

    [Benchmark(Description = "Sqlx (Batch)")]
    public int Sqlx_BatchInsert()
    {
        try
        {
            // Ensure connection is open
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
            
            return _sqlxRepo.BatchInsertAsync(_users).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }
}

