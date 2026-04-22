// <copyright file="EfCoreComparisonBenchmark.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sqlx.Benchmarks.Models;
using Sqlx.Benchmarks.Repositories;

namespace Sqlx.Benchmarks.Benchmarks;

// ── EF Core DbContext ─────────────────────────────────────────────────────────

/// <summary>EF Core entity mapped to the same 'users' table.</summary>
[System.ComponentModel.DataAnnotations.Schema.Table("users")]
public class EfUser
{
    [System.ComponentModel.DataAnnotations.Key]
    [System.ComponentModel.DataAnnotations.Schema.Column("id")]
    public long Id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("name")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Schema.Column("email")]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Schema.Column("age")]
    public int Age { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("is_active")]
    public bool IsActive { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("balance")]
    public decimal Balance { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("description")]
    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("score")]
    public int Score { get; set; }
}

public class BenchmarkDbContext : DbContext
{
    private readonly string _connectionString;

    public BenchmarkDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<EfUser> Users => Set<EfUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(_connectionString).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}

// ── Benchmark ─────────────────────────────────────────────────────────────────

/// <summary>
/// Benchmark comparing Sqlx vs Dapper.AOT vs EF Core for common data access patterns.
/// All use SQLite in-memory database with 10,000 rows.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EfCoreComparisonBenchmark
{
    private const string SelectByIdSql =
        "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id";

    private const string SelectListSql =
        "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE age >= @minAge LIMIT @limit";

    private SqliteConnection _connection = null!;
    private BenchmarkUserRepository _sqlxRepo = null!;
    private BenchmarkDbContext _efContext = null!;
    private long _testId;
    private string _connectionString = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use a file-based in-memory DB so EF Core can share it
        _connectionString = "Data Source=benchmark_efcore;Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10000);

        _sqlxRepo = new BenchmarkUserRepository(_connection, SqlDefine.SQLite);
        _testId = 5000;

        _efContext = new BenchmarkDbContext(_connectionString);
        // Warm up EF Core
        _ = _efContext.Users.FirstOrDefault(u => u.Id == 1);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _efContext?.Dispose();
        _connection?.Dispose();
    }

    // ── Single row by ID ──────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Sqlx: GetById")]
    public BenchmarkUser? Sqlx_GetById() => _sqlxRepo.GetById(_testId);

    [Benchmark(Description = "Dapper.AOT: GetById")]
    public BenchmarkUser? DapperAot_GetById() =>
        _connection.QueryFirstOrDefault<BenchmarkUser>(SelectByIdSql, new { id = _testId });

    [Benchmark(Description = "EF Core: Find")]
    public EfUser? EfCore_Find() =>
        _efContext.Users.FirstOrDefault(u => u.Id == _testId);

    [Benchmark(Description = "EF Core: FromSql")]
    public EfUser? EfCore_FromSql() =>
        _efContext.Users
            .FromSqlRaw("SELECT * FROM users WHERE id = {0}", _testId)
            .FirstOrDefault();

    // ── List query (10 rows) ──────────────────────────────────────────────────

    [Benchmark(Description = "Sqlx: GetList(10)")]
    public List<BenchmarkUser> Sqlx_GetList10() =>
        _sqlxRepo.GetByMinAge(25, 10);

    [Benchmark(Description = "Dapper.AOT: GetList(10)")]
    public List<BenchmarkUser> DapperAot_GetList10() =>
        _connection.Query<BenchmarkUser>(SelectListSql, new { minAge = 25, limit = 10 }).AsList();

    [Benchmark(Description = "EF Core: GetList(10)")]
    public List<EfUser> EfCore_GetList10() =>
        _efContext.Users.Where(u => u.Age >= 25).Take(10).ToList();

    // ── List query (100 rows) ─────────────────────────────────────────────────

    [Benchmark(Description = "Sqlx: GetList(100)")]
    public List<BenchmarkUser> Sqlx_GetList100() =>
        _sqlxRepo.GetByMinAge(18, 100);

    [Benchmark(Description = "Dapper.AOT: GetList(100)")]
    public List<BenchmarkUser> DapperAot_GetList100() =>
        _connection.Query<BenchmarkUser>(SelectListSql, new { minAge = 18, limit = 100 }).AsList();

    [Benchmark(Description = "EF Core: GetList(100)")]
    public List<EfUser> EfCore_GetList100() =>
        _efContext.Users.Where(u => u.Age >= 18).Take(100).ToList();

    // ── Count ─────────────────────────────────────────────────────────────────

    [Benchmark(Description = "Sqlx: Count")]
    public long Sqlx_Count() => _sqlxRepo.Count();

    [Benchmark(Description = "Dapper.AOT: Count")]
    public long DapperAot_Count() =>
        _connection.ExecuteScalar<long>("SELECT COUNT(*) FROM users");

    [Benchmark(Description = "EF Core: Count")]
    public int EfCore_Count() => _efContext.Users.Count();
}
