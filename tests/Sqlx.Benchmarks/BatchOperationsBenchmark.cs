// -----------------------------------------------------------------------
// <copyright file="BatchOperationsBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
public class BatchOperationsBenchmark
{
    private SqliteConnection? _connection;
    private List<TestUser> _testData = null!;

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                email TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        // Generate test data
        _testData = Enumerable.Range(1, 1000)
            .Select(i => new TestUser
            {
                Id = i,
                Name = $"User{i}",
                Age = 20 + (i % 50),
                Email = $"user{i}@example.com"
            })
            .ToList();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [Benchmark(Description = "批量插入 - 单条循环")]
    public async Task<int> BatchInsert_SingleLoop()
    {
        int totalAffected = 0;
        foreach (var user in _testData)
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "INSERT INTO users (name, age, email) VALUES (@name, @age, @email)";
            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@age", user.Age);
            cmd.Parameters.AddWithValue("@email", user.Email);
            totalAffected += await cmd.ExecuteNonQueryAsync();
        }

        // Cleanup
        using var cleanupCmd = _connection!.CreateCommand();
        cleanupCmd.CommandText = "DELETE FROM users";
        await cleanupCmd.ExecuteNonQueryAsync();

        return totalAffected;
    }

    [Benchmark(Description = "批量插入 - 批次优化 (100条/批)")]
    public async Task<int> BatchInsert_Batched100()
    {
        return await BatchInsertWithBatchSize(100);
    }

    [Benchmark(Description = "批量插入 - 批次优化 (500条/批)", Baseline = true)]
    public async Task<int> BatchInsert_Batched500()
    {
        return await BatchInsertWithBatchSize(500);
    }

    [Benchmark(Description = "批量插入 - 批次优化 (1000条/批)")]
    public async Task<int> BatchInsert_Batched1000()
    {
        return await BatchInsertWithBatchSize(1000);
    }

    private async Task<int> BatchInsertWithBatchSize(int batchSize)
    {
        int totalAffected = 0;
        int processed = 0;

        while (processed < _testData.Count)
        {
            var batch = _testData.Skip(processed).Take(batchSize).ToList();

            // Build SQL
            var sqlBuilder = new System.Text.StringBuilder("INSERT INTO users (name, age, email) VALUES ");
            using var cmd = _connection!.CreateCommand();

            for (int i = 0; i < batch.Count; i++)
            {
                if (i > 0) sqlBuilder.Append(", ");
                sqlBuilder.Append($"(@name{i}, @age{i}, @email{i})");

                cmd.Parameters.AddWithValue($"@name{i}", batch[i].Name);
                cmd.Parameters.AddWithValue($"@age{i}", batch[i].Age);
                cmd.Parameters.AddWithValue($"@email{i}", batch[i].Email);
            }

            cmd.CommandText = sqlBuilder.ToString();
            totalAffected += await cmd.ExecuteNonQueryAsync();
            processed += batch.Count;
        }

        // Cleanup
        using var cleanupCmd = _connection!.CreateCommand();
        cleanupCmd.CommandText = "DELETE FROM users";
        await cleanupCmd.ExecuteNonQueryAsync();

        return totalAffected;
    }

    [Benchmark(Description = "批量更新 - 单条循环")]
    public async Task<int> BatchUpdate_SingleLoop()
    {
        // Insert test data first
        await BatchInsertWithBatchSize(1000);

        int totalAffected = 0;
        foreach (var user in _testData)
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "UPDATE users SET age = @age WHERE id = @id";
            cmd.Parameters.AddWithValue("@age", user.Age + 1);
            cmd.Parameters.AddWithValue("@id", user.Id);
            totalAffected += await cmd.ExecuteNonQueryAsync();
        }

        // Cleanup
        using var cleanupCmd = _connection!.CreateCommand();
        cleanupCmd.CommandText = "DELETE FROM users";
        await cleanupCmd.ExecuteNonQueryAsync();

        return totalAffected;
    }

    [Benchmark(Description = "批量更新 - 一条SQL (WHERE IN)")]
    public async Task<int> BatchUpdate_WhereIn()
    {
        // Insert test data first
        await BatchInsertWithBatchSize(1000);

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE users SET age = age + 1 WHERE age > 30";
        var totalAffected = await cmd.ExecuteNonQueryAsync();

        // Cleanup
        using var cleanupCmd = _connection!.CreateCommand();
        cleanupCmd.CommandText = "DELETE FROM users";
        await cleanupCmd.ExecuteNonQueryAsync();

        return totalAffected;
    }

    [Benchmark(Description = "批量删除 - 单条循环")]
    public async Task<int> BatchDelete_SingleLoop()
    {
        // Insert test data first
        await BatchInsertWithBatchSize(1000);

        int totalAffected = 0;
        foreach (var user in _testData)
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "DELETE FROM users WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", user.Id);
            totalAffected += await cmd.ExecuteNonQueryAsync();
        }

        return totalAffected;
    }

    [Benchmark(Description = "批量删除 - 一条SQL (WHERE IN)")]
    public async Task<int> BatchDelete_WhereIn()
    {
        // Insert test data first
        await BatchInsertWithBatchSize(1000);

        var ids = string.Join(",", _testData.Select(u => u.Id));
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"DELETE FROM users WHERE id IN ({ids})";
        var totalAffected = await cmd.ExecuteNonQueryAsync();

        return totalAffected;
    }
}

