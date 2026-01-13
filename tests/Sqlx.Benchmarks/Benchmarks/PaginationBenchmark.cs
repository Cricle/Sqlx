using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for pagination queries.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PaginationBenchmark
{
    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    
    [Params(20, 50, 100)]
    public int PageSize { get; set; }
    
    [Params(0, 100, 500)]
    public int Offset { get; set; }
    
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
    
    [Benchmark(Baseline = true, Description = "Sqlx GetPaged")]
    public async Task<List<BenchmarkUser>> Sqlx_GetPaged()
    {
        return await _sqlxRepo.GetPagedAsync(PageSize, Offset);
    }
    
    [Benchmark(Description = "Dapper Query")]
    public async Task<List<BenchmarkUser>> Dapper_GetPaged()
    {
        var result = await _connection.QueryAsync<BenchmarkUser>(
            "SELECT id AS Id, name AS Name, email AS Email, age AS Age, is_active AS IsActive, " +
            "created_at AS CreatedAt, updated_at AS UpdatedAt, balance AS Balance, " +
            "description AS Description, score AS Score FROM users LIMIT @pageSize OFFSET @offset",
            new { pageSize = PageSize, offset = Offset });
        return result.ToList();
    }
    
    [Benchmark(Description = "ADO.NET Manual")]
    public async Task<List<BenchmarkUser>> AdoNet_GetPaged()
    {
        var result = new List<BenchmarkUser>(PageSize);
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users LIMIT @pageSize OFFSET @offset";
        
        AddParameter(cmd, "@pageSize", PageSize);
        AddParameter(cmd, "@offset", Offset);
        
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
    
    private static void AddParameter(SqliteCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        cmd.Parameters.Add(param);
    }
}
