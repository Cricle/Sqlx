using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for selecting a single entity by ID.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectSingleBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private long _testId;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
        _testId = 5000; // Middle of the dataset
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx GetById")]
    public async Task<BenchmarkUser?> Sqlx_GetById()
    {
        return await _sqlxRepo.GetByIdAsync(_testId);
    }
    
    [Benchmark(Description = "Dapper QueryFirstOrDefault")]
    public async Task<BenchmarkUser?> Dapper_GetById()
    {
        return await _connection.QueryFirstOrDefaultAsync<BenchmarkUser>(
            "SELECT id AS Id, name AS Name, email AS Email, age AS Age, is_active AS IsActive, " +
            "created_at AS CreatedAt, updated_at AS UpdatedAt, balance AS Balance, " +
            "description AS Description, score AS Score FROM users WHERE id = @id",
            new { id = _testId });
    }
    
    [Benchmark(Description = "ADO.NET Manual")]
    public async Task<BenchmarkUser?> AdoNet_GetById()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = _testId;
        cmd.Parameters.Add(param);
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new BenchmarkUser
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                Age = reader.GetInt32(3),
                IsActive = reader.GetInt64(4) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                UpdatedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                Balance = (decimal)reader.GetDouble(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                Score = reader.GetInt32(9)
            };
        }
        return null;
    }
}
