using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Data.Sqlite;
using Sqlx;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing SqlTemplate ADO.NET extensions vs manual ADO.NET code.
/// Tests CreateCommand, ExecuteScalar, ExecuteNonQuery, and ExecuteReader performance.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 3, iterationCount: 5)]
public class SqlTemplateAdoNetBenchmark
{
    private DbConnection _connection = null!;
    private SqlTemplate _selectCountTemplate;
    private SqlTemplate _selectUserTemplate;
    private SqlTemplate _insertUserTemplate;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        // Create test table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Age INTEGER NOT NULL,
                Email TEXT
            )";
        cmd.ExecuteNonQuery();
        
        // Insert test data
        for (int i = 1; i <= 1000; i++)
        {
            using var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Users (Id, Name, Age, Email) VALUES (@id, @name, @age, @email)";
            insertCmd.Parameters.Add(new SqliteParameter("@id", i));
            insertCmd.Parameters.Add(new SqliteParameter("@name", $"User{i}"));
            insertCmd.Parameters.Add(new SqliteParameter("@age", 20 + (i % 50)));
            insertCmd.Parameters.Add(new SqliteParameter("@email", $"user{i}@example.com"));
            insertCmd.ExecuteNonQuery();
        }
        
        // Initialize templates
        _selectCountTemplate = new SqlTemplate(
            "SELECT COUNT(*) FROM Users WHERE Age >= @minAge",
            new Dictionary<string, object?> { ["@minAge"] = 25 });
        
        _selectUserTemplate = new SqlTemplate(
            "SELECT Name FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 500 });
        
        _insertUserTemplate = new SqlTemplate(
            "INSERT INTO Users (Id, Name, Age, Email) VALUES (@id, @name, @age, @email)",
            new Dictionary<string, object?>
            {
                ["@id"] = 0,
                ["@name"] = "",
                ["@age"] = 0,
                ["@email"] = ""
            });
        
        // Warmup
        Manual_CreateCommand();
        SqlTemplate_CreateCommand();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }
    
    // ========== CreateCommand Benchmarks ==========
    
    [Benchmark(Baseline = true, Description = "Manual CreateCommand")]
    public DbCommand Manual_CreateCommand()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Age >= @minAge";
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@minAge";
        param.Value = 25;
        cmd.Parameters.Add(param);
        
        var result = cmd;
        result.Dispose();
        return result;
    }
    
    [Benchmark(Description = "SqlTemplate CreateCommand")]
    public DbCommand SqlTemplate_CreateCommand()
    {
        var result = _selectCountTemplate.CreateCommand(_connection);
        result.Dispose();
        return result;
    }
    
    [Benchmark(Description = "SqlTemplate CreateCommand + Override")]
    public DbCommand SqlTemplate_CreateCommand_WithOverride()
    {
        var overrides = new Dictionary<string, object?> { ["@minAge"] = 30 };
        var result = _selectCountTemplate.CreateCommand(_connection, parameterOverrides: overrides);
        result.Dispose();
        return result;
    }
    
    // ========== ExecuteScalar Benchmarks ==========
    
    [Benchmark(Description = "Manual ExecuteScalar")]
    public int Manual_ExecuteScalar()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Age >= @minAge";
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@minAge";
        param.Value = 25;
        cmd.Parameters.Add(param);
        
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }
    
    [Benchmark(Description = "SqlTemplate ExecuteScalar<int>")]
    public int SqlTemplate_ExecuteScalar()
    {
        return _selectCountTemplate.ExecuteScalar<int>(_connection);
    }
    
    [Benchmark(Description = "SqlTemplate ExecuteScalar<int> + Override")]
    public int SqlTemplate_ExecuteScalar_WithOverride()
    {
        var overrides = new Dictionary<string, object?> { ["@minAge"] = 30 };
        return _selectCountTemplate.ExecuteScalar<int>(_connection, parameterOverrides: overrides);
    }
    
    [Benchmark(Description = "Manual ExecuteScalar String")]
    public string? Manual_ExecuteScalar_String()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Name FROM Users WHERE Id = @id";
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = 500;
        cmd.Parameters.Add(param);
        
        var result = cmd.ExecuteScalar();
        return result as string;
    }
    
    [Benchmark(Description = "SqlTemplate ExecuteScalar<string>")]
    public string? SqlTemplate_ExecuteScalar_String()
    {
        return _selectUserTemplate.ExecuteScalar<string>(_connection);
    }
}
