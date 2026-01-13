using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for inserting entities.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class InsertBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private int _counter;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _counter = 0;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        // Clear table before each iteration
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM users";
        cmd.ExecuteNonQuery();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx Insert")]
    public async Task<long> Sqlx_Insert()
    {
        var user = DatabaseSetup.CreateTestUser(_counter++);
        return await _sqlxRepo.InsertAndGetIdAsync(user);
    }
    
    [Benchmark(Description = "Dapper Execute + LastInsertRowId")]
    public async Task<long> Dapper_Insert()
    {
        var user = DatabaseSetup.CreateTestUser(_counter++);
        await _connection.ExecuteAsync(
            "INSERT INTO users (name, email, age, is_active, created_at, updated_at, balance, description, score) " +
            "VALUES (@Name, @Email, @Age, @IsActive, @CreatedAt, @UpdatedAt, @Balance, @Description, @Score)",
            new
            {
                user.Name,
                user.Email,
                user.Age,
                IsActive = user.IsActive ? 1 : 0,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt?.ToString("O"),
                Balance = (double)user.Balance,
                user.Description,
                user.Score
            });
        return await _connection.ExecuteScalarAsync<long>("SELECT last_insert_rowid()");
    }
    
    [Benchmark(Description = "ADO.NET Manual")]
    public async Task<long> AdoNet_Insert()
    {
        var user = DatabaseSetup.CreateTestUser(_counter++);
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO users (name, email, age, is_active, created_at, updated_at, balance, description, score) " +
                         "VALUES (@name, @email, @age, @isActive, @createdAt, @updatedAt, @balance, @description, @score); " +
                         "SELECT last_insert_rowid()";
        
        AddParameter(cmd, "@name", user.Name);
        AddParameter(cmd, "@email", user.Email);
        AddParameter(cmd, "@age", user.Age);
        AddParameter(cmd, "@isActive", user.IsActive ? 1 : 0);
        AddParameter(cmd, "@createdAt", user.CreatedAt.ToString("O"));
        AddParameter(cmd, "@updatedAt", user.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
        AddParameter(cmd, "@balance", (double)user.Balance);
        AddParameter(cmd, "@description", user.Description ?? (object)DBNull.Value);
        AddParameter(cmd, "@score", user.Score);
        
        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }
    
    private static void AddParameter(SqliteCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        cmd.Parameters.Add(param);
    }
}
