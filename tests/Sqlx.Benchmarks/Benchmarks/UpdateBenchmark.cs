using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for updating entities.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class UpdateBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private BenchmarkUser _testUser = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        
        // Get a user to update
        _testUser = _sqlxRepo.GetByIdAsync(5000).GetAwaiter().GetResult()!;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx Update")]
    public async Task<int> Sqlx_Update()
    {
        _testUser.Score++;
        _testUser.UpdatedAt = DateTime.UtcNow;
        return await _sqlxRepo.UpdateAsync(_testUser);
    }
    
    [Benchmark(Description = "Dapper Execute")]
    public async Task<int> Dapper_Update()
    {
        _testUser.Score++;
        _testUser.UpdatedAt = DateTime.UtcNow;
        return await _connection.ExecuteAsync(
            "UPDATE users SET name = @Name, email = @Email, age = @Age, is_active = @IsActive, " +
            "created_at = @CreatedAt, updated_at = @UpdatedAt, balance = @Balance, " +
            "description = @Description, score = @Score WHERE id = @Id",
            new
            {
                _testUser.Id,
                _testUser.Name,
                _testUser.Email,
                _testUser.Age,
                IsActive = _testUser.IsActive ? 1 : 0,
                CreatedAt = _testUser.CreatedAt.ToString("O"),
                UpdatedAt = _testUser.UpdatedAt?.ToString("O"),
                Balance = (double)_testUser.Balance,
                _testUser.Description,
                _testUser.Score
            });
    }
    
    [Benchmark(Description = "ADO.NET Manual")]
    public async Task<int> AdoNet_Update()
    {
        _testUser.Score++;
        _testUser.UpdatedAt = DateTime.UtcNow;
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE users SET name = @name, email = @email, age = @age, is_active = @isActive, " +
                         "created_at = @createdAt, updated_at = @updatedAt, balance = @balance, " +
                         "description = @description, score = @score WHERE id = @id";
        
        AddParameter(cmd, "@id", _testUser.Id);
        AddParameter(cmd, "@name", _testUser.Name);
        AddParameter(cmd, "@email", _testUser.Email);
        AddParameter(cmd, "@age", _testUser.Age);
        AddParameter(cmd, "@isActive", _testUser.IsActive ? 1 : 0);
        AddParameter(cmd, "@createdAt", _testUser.CreatedAt.ToString("O"));
        AddParameter(cmd, "@updatedAt", _testUser.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
        AddParameter(cmd, "@balance", (double)_testUser.Balance);
        AddParameter(cmd, "@description", _testUser.Description ?? (object)DBNull.Value);
        AddParameter(cmd, "@score", _testUser.Score);
        
        return await cmd.ExecuteNonQueryAsync();
    }
    
    private static void AddParameter(SqliteCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        cmd.Parameters.Add(param);
    }
}
