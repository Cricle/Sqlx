using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Sqlx DbBatchExecutor vs Dapper.AOT: Batch insert comparison.
/// Note: SQLite doesn't support DbBatch (CanCreateBatch=false), so this benchmark
/// tests the fallback loop execution path for Sqlx.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class BatchInsertBenchmark
{
    private SqliteConnection _connection = null!;
    private List<BenchmarkUser> _users = null!;
    private List<DapperBatchUser> _dapperUsers = null!;

    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);

        _users = new List<BenchmarkUser>();
        _dapperUsers = new List<DapperBatchUser>();
        for (int i = 0; i < BatchSize; i++)
        {
            var user = DatabaseSetup.CreateTestUser(i);
            _users.Add(user);
            _dapperUsers.Add(new DapperBatchUser
            {
                name = user.Name,
                email = user.Email,
                age = user.Age,
                is_active = user.IsActive ? 1 : 0,
                created_at = user.CreatedAt.ToString("O"),
                balance = (double)user.Balance,
                description = user.Description,
                score = user.Score
            });
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users";
        cmd.ExecuteNonQuery();
    }

    [Benchmark(Baseline = true, Description = "Sqlx.DbBatch")]
    public async Task<int> Sqlx_BatchInsert()
    {
        var sql = "INSERT INTO users (name, email, age, is_active, created_at, balance, description, score) VALUES (@name, @email, @age, @is_active, @created_at, @balance, @description, @score)";
        return await _connection.ExecuteBatchAsync(
            sql,
            _users,
            BenchmarkUserParameterBinder.Default);
    }

    [Benchmark(Description = "Dapper.AOT")]
    public async Task<int> DapperAot_BatchInsert()
    {
        return await _connection.ExecuteAsync(
            "INSERT INTO users (name, email, age, is_active, created_at, balance, description, score) VALUES (@name, @email, @age, @is_active, @created_at, @balance, @description, @score)",
            _dapperUsers);
    }

    [Benchmark(Description = "Sqlx.Loop")]
    public async Task<int> Sqlx_LoopInsert()
    {
        var sql = "INSERT INTO users (name, email, age, is_active, created_at, balance, description, score) VALUES (@name, @email, @age, @is_active, @created_at, @balance, @description, @score)";
        var total = 0;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var user in _users)
        {
            cmd.Parameters.Clear();
            BenchmarkUserParameterBinder.Default.BindEntity(cmd, user);
            total += await cmd.ExecuteNonQueryAsync();
        }

        return total;
    }
}

public class DapperBatchUser
{
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int age { get; set; }
    public int is_active { get; set; }
    public string created_at { get; set; } = string.Empty;
    public double balance { get; set; }
    public string? description { get; set; }
    public int score { get; set; }
}
