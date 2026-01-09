// -----------------------------------------------------------------------
// <copyright file="PredefinedInterfacesBenchmarks.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Benchmarks;

/// <summary>
/// Performance benchmarks comparing predefined interfaces vs hand-written methods.
/// Validates that predefined interfaces have equivalent performance (within 5% difference).
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Config(typeof(BenchmarkConfig))]
public class PredefinedInterfacesBenchmarks
{
    private IDbConnection _connection = null!;
    private BenchmarkUserCrudRepository _predefinedRepo = null!;
    private BenchmarkUserCustomRepository _customRepo = null!;

    private class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
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
        InitializeDatabase();

        _predefinedRepo = new BenchmarkUserCrudRepository(_connection);
        _customRepo = new BenchmarkUserCustomRepository(_connection);

        // Warmup
        _ = _predefinedRepo.GetByIdAsync(500).GetAwaiter().GetResult();
        _ = _customRepo.GetByIdAsync(500).GetAwaiter().GetResult();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS benchmark_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT,
                age INTEGER NOT NULL,
                balance REAL NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        // Seed 1000 users for realistic benchmarking
        for (int i = 1; i <= 1000; i++)
        {
            using var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO benchmark_users (name, email, age, balance, is_active, created_at) VALUES ('User{i}', 'user{i}@test.com', {20 + (i % 50)}, {100.0 + i}, 1, datetime('now'))";
            insertCmd.ExecuteNonQuery();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    // ==================== GetById Benchmarks ====================

    [Benchmark(Baseline = true, Description = "Predefined: GetByIdAsync")]
    public BenchmarkUser? Predefined_GetById()
    {
        return _predefinedRepo.GetByIdAsync(500).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Custom: GetByIdAsync")]
    public BenchmarkUser? Custom_GetById()
    {
        return _customRepo.GetByIdAsync(500).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Dapper: GetById")]
    public BenchmarkUser? Dapper_GetById()
    {
        return _connection.QueryFirstOrDefault<BenchmarkUser>(
            "SELECT id, name, email, age, balance, is_active AS IsActive, created_at AS CreatedAt FROM benchmark_users WHERE id = @id",
            new { id = 500 });
    }

    // ==================== Count Benchmarks ====================

    [Benchmark(Description = "Predefined: CountAsync")]
    public long Predefined_Count()
    {
        return _predefinedRepo.CountAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Custom: CountAsync")]
    public long Custom_Count()
    {
        return _customRepo.CountAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Dapper: Count")]
    public long Dapper_Count()
    {
        return _connection.ExecuteScalar<long>("SELECT COUNT(*) FROM benchmark_users");
    }

    // ==================== GetAll Benchmarks ====================

    [Benchmark(Description = "Predefined: GetAllAsync")]
    public List<BenchmarkUser> Predefined_GetAll()
    {
        return _predefinedRepo.GetAllAsync(100).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Custom: GetAllAsync")]
    public List<BenchmarkUser> Custom_GetAll()
    {
        return _customRepo.GetAllAsync(100).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Dapper: GetAll")]
    public List<BenchmarkUser> Dapper_GetAll()
    {
        return _connection.Query<BenchmarkUser>(
            "SELECT id, name, email, age, balance, is_active AS IsActive, created_at AS CreatedAt FROM benchmark_users LIMIT 100").ToList();
    }

    // ==================== Exists Benchmarks ====================

    [Benchmark(Description = "Predefined: ExistsAsync")]
    public bool Predefined_Exists()
    {
        return _predefinedRepo.ExistsAsync(500).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Custom: ExistsAsync")]
    public bool Custom_Exists()
    {
        return _customRepo.ExistsAsync(500).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Dapper: Exists")]
    public bool Dapper_Exists()
    {
        return _connection.ExecuteScalar<int>(
            "SELECT CASE WHEN EXISTS(SELECT 1 FROM benchmark_users WHERE id = @id) THEN 1 ELSE 0 END",
            new { id = 500 }) == 1;
    }
}

// ==================== Entity ====================

[TableName("benchmark_users")]
public class BenchmarkUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ==================== Predefined Interface Repository ====================

/// <summary>
/// Repository using predefined ICrudRepository interface.
/// All methods are generated by the source generator.
/// </summary>
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICrudRepository<BenchmarkUser, long>))]
public partial class BenchmarkUserCrudRepository(IDbConnection connection)
    : ICrudRepository<BenchmarkUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// ==================== Custom Interface Repository ====================

/// <summary>
/// Repository using custom interface with hand-written SQL templates.
/// This represents the traditional approach before predefined interfaces.
/// </summary>
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBenchmarkUserCustomRepository))]
public partial class BenchmarkUserCustomRepository(IDbConnection connection)
    : IBenchmarkUserCustomRepository
{
    protected readonly IDbConnection connection = connection;
}

/// <summary>
/// Custom interface with explicit SQL templates (hand-written approach).
/// </summary>
public interface IBenchmarkUserCustomRepository
{
    [SqlTemplate("SELECT id, name, email, age, balance, is_active, created_at FROM benchmark_users WHERE id = @id")]
    Task<BenchmarkUser?> GetByIdAsync(long id);

    [SqlTemplate("SELECT COUNT(*) FROM benchmark_users")]
    Task<long> CountAsync();

    [SqlTemplate("SELECT id, name, email, age, balance, is_active, created_at FROM benchmark_users LIMIT @limit")]
    Task<List<BenchmarkUser>> GetAllAsync(int limit);

    [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM benchmark_users WHERE id = @id) THEN 1 ELSE 0 END")]
    Task<bool> ExistsAsync(long id);

    [SqlTemplate("INSERT INTO benchmark_users (name, email, age, balance, is_active, created_at) VALUES (@Name, @Email, @Age, @Balance, @IsActive, @CreatedAt)")]
    Task<int> InsertAsync(BenchmarkUser entity);

    [SqlTemplate("UPDATE benchmark_users SET name = @Name, email = @Email, age = @Age, balance = @Balance, is_active = @IsActive WHERE id = @Id")]
    Task<int> UpdateAsync(BenchmarkUser entity);

    [SqlTemplate("DELETE FROM benchmark_users WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}
