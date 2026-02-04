// <copyright file="ResultReaderOrdinalsVsSwitchBenchmark.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Sqlx.Annotations;
using System.Data;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark comparing ResultReader performance:
/// 1. Current approach: Get ordinals once, then use ordinals array
/// 2. Alternative: Use switch case on column names directly (no ordinals)
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ResultReaderOrdinalsVsSwitchBenchmark
{
    private SqliteConnection _connection = null!;
    private IDataReader _reader = null!;
    private int[] _ordinals = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE benchmark_users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                score REAL NOT NULL
            )";
        cmd.ExecuteNonQuery();

        // Insert test data
        cmd.CommandText = @"
            INSERT INTO benchmark_users (id, name, email, age, is_active, created_at, updated_at, score)
            VALUES 
                (1, 'User1', 'user1@test.com', 25, 1, '2024-01-01', '2024-01-01', 85.5),
                (2, 'User2', 'user2@test.com', 30, 1, '2024-01-02', '2024-01-02', 90.0),
                (3, 'User3', 'user3@test.com', 35, 0, '2024-01-03', '2024-01-03', 75.5)";
        cmd.ExecuteNonQuery();

        // Prepare reader
        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, score FROM benchmark_users";
        _reader = selectCmd.ExecuteReader();

        // Pre-compute ordinals
        _ordinals = new int[]
        {
            _reader.GetOrdinal("id"),
            _reader.GetOrdinal("name"),
            _reader.GetOrdinal("email"),
            _reader.GetOrdinal("age"),
            _reader.GetOrdinal("is_active"),
            _reader.GetOrdinal("created_at"),
            _reader.GetOrdinal("updated_at"),
            _reader.GetOrdinal("score")
        };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _reader?.Dispose();
        _connection?.Dispose();
    }

    [IterationSetup]
    public void ResetReader()
    {
        // Reset reader to beginning
        _reader.Dispose();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active, created_at, updated_at, score FROM benchmark_users";
        _reader = cmd.ExecuteReader();
    }

    /// <summary>
    /// Current approach: Use pre-computed ordinals array
    /// </summary>
    [Benchmark(Baseline = true)]
    public List<OrdinalsTestUser> WithOrdinals()
    {
        var results = new List<OrdinalsTestUser>();
        while (_reader.Read())
        {
            results.Add(OrdinalsTestUserResultReader.Default.Read(_reader, _ordinals));
        }
        return results;
    }

    /// <summary>
    /// Alternative: Use switch case on column names (no ordinals array)
    /// </summary>
    [Benchmark]
    public List<OrdinalsTestUser> WithSwitchCase()
    {
        var results = new List<OrdinalsTestUser>();
        while (_reader.Read())
        {
            results.Add(ReadWithSwitchCase(_reader));
        }
        return results;
    }

    /// <summary>
    /// Alternative: Call GetOrdinal for each property (worst case)
    /// </summary>
    [Benchmark]
    public List<OrdinalsTestUser> WithGetOrdinalPerProperty()
    {
        var results = new List<OrdinalsTestUser>();
        while (_reader.Read())
        {
            results.Add(OrdinalsTestUserResultReader.Default.Read(_reader));
        }
        return results;
    }

    /// <summary>
    /// Alternative: Use switch case on column name hashcode
    /// </summary>
    [Benchmark]
    public List<OrdinalsTestUser> WithHashCodeSwitch()
    {
        var results = new List<OrdinalsTestUser>();
        while (_reader.Read())
        {
            results.Add(ReadWithHashCodeSwitch(_reader));
        }
        return results;
    }

    /// <summary>
    /// Manual implementation using switch case on column names
    /// </summary>
    private static OrdinalsTestUser ReadWithSwitchCase(IDataReader reader)
    {
        long id = 0;
        string name = string.Empty;
        string email = string.Empty;
        int age = 0;
        bool isActive = false;
        string createdAt = string.Empty;
        string updatedAt = string.Empty;
        double score = 0;

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            switch (columnName)
            {
                case "id":
                    id = reader.GetInt64(i);
                    break;
                case "name":
                    name = reader.GetString(i);
                    break;
                case "email":
                    email = reader.GetString(i);
                    break;
                case "age":
                    age = reader.GetInt32(i);
                    break;
                case "is_active":
                    isActive = reader.GetInt64(i) != 0;
                    break;
                case "created_at":
                    createdAt = reader.GetString(i);
                    break;
                case "updated_at":
                    updatedAt = reader.GetString(i);
                    break;
                case "score":
                    score = reader.GetDouble(i);
                    break;
            }
        }

        return new OrdinalsTestUser
        {
            Id = id,
            Name = name,
            Email = email,
            Age = age,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Score = score
        };
    }

    /// <summary>
    /// Manual implementation using switch case on column name hashcode
    /// </summary>
    private static OrdinalsTestUser ReadWithHashCodeSwitch(IDataReader reader)
    {
        long id = 0;
        string name = string.Empty;
        string email = string.Empty;
        int age = 0;
        bool isActive = false;
        string createdAt = string.Empty;
        string updatedAt = string.Empty;
        double score = 0;

        // Pre-computed hashcodes for column names (as uint to avoid sign issues)
        const uint IdHash = 3355; // "id".GetHashCode()
        const uint NameHash = 2369371622; // "name".GetHashCode()
        const uint EmailHash = 1554823892; // "email".GetHashCode()
        const uint AgeHash = 1099313834; // "age".GetHashCode()
        const uint IsActiveHash = 1352309551; // "is_active".GetHashCode()
        const uint CreatedAtHash = 1006397715; // "created_at".GetHashCode()
        const uint UpdatedAtHash = 1006397716; // "updated_at".GetHashCode()
        const uint ScoreHash = 1544803905; // "score".GetHashCode()

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var hash = (uint)columnName.GetHashCode();
            
            switch (hash)
            {
                case IdHash when columnName == "id":
                    id = reader.GetInt64(i);
                    break;
                case NameHash when columnName == "name":
                    name = reader.GetString(i);
                    break;
                case EmailHash when columnName == "email":
                    email = reader.GetString(i);
                    break;
                case AgeHash when columnName == "age":
                    age = reader.GetInt32(i);
                    break;
                case IsActiveHash when columnName == "is_active":
                    isActive = reader.GetInt64(i) != 0;
                    break;
                case CreatedAtHash when columnName == "created_at":
                    createdAt = reader.GetString(i);
                    break;
                case UpdatedAtHash when columnName == "updated_at":
                    updatedAt = reader.GetString(i);
                    break;
                case ScoreHash when columnName == "score":
                    score = reader.GetDouble(i);
                    break;
            }
        }

        return new OrdinalsTestUser
        {
            Id = id,
            Name = name,
            Email = email,
            Age = age,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Score = score
        };
    }
}

[Sqlx]
[TableName("benchmark_users")]
public class OrdinalsTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public double Score { get; set; }
}
