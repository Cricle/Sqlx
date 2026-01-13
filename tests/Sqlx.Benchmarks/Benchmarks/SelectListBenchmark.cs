using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for selecting multiple entities.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SelectListBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    
    [Params(10, 100, 1000)]
    public int Limit { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);
        _sqlxRepo = new BenchmarkUserRepository(_connection);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true, Description = "Sqlx GetAll")]
    public async Task<List<BenchmarkUser>> Sqlx_GetAll()
    {
        return await _sqlxRepo.GetAllAsync(Limit);
    }
    
    [Benchmark(Description = "Dapper Query")]
    public async Task<List<BenchmarkUser>> Dapper_GetAll()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(
            "SELECT id AS Id, name AS Name, email AS Email, age AS Age, is_active AS IsActive, " +
            "created_at AS CreatedAt, updated_at AS UpdatedAt, balance AS Balance, " +
            "description AS Description, score AS Score FROM users LIMIT @limit",
            new { limit = Limit });
        return result.ToList();
    }
    
    [Benchmark(Description = "ADO.NET Manual")]
    public async Task<List<BenchmarkUser>> AdoNet_GetAll()
    {
        var result = new List<BenchmarkUser>(Limit);
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @limit";
        var param = cmd.CreateParameter();
        param.ParameterName = "@limit";
        param.Value = Limit;
        cmd.Parameters.Add(param);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new BenchmarkUser
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
            });
        }
        return result;
    }
}
