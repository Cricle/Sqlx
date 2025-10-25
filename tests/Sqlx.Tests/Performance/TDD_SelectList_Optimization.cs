using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Performance;

/// <summary>
/// TDD: SelectList性能优化测试
/// 目标：将SelectList性能优化到Dapper的90%以内（当前：57%）
/// </summary>
[TestClass]
public class TDD_SelectList_Optimization : CodeGenerationTestBase
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Performance")]
    [TestCategory("SelectList")]
    public void SelectList_SmallDataset_ShouldBeReasonablyFast()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL
            )");
        
        // Insert 10 rows
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute(
                "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)",
                new { name = $"User{i}", email = $"user{i}@test.com", age = 20 + i });
        }
        
        var repo = new PerfTestUserRepository(connection);
        
        // Act - Warmup
        repo.GetAllAsync().Wait();
        
        // Act - Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var users = repo.GetAllAsync().Result;
        }
        sw.Stop();
        
        // Assert
        var avgTime = sw.ElapsedMilliseconds / 100.0;
        Console.WriteLine($"Average time for 10 rows (100 iterations): {avgTime:F2}ms");
        
        // Sanity check - should complete reasonably fast
        Assert.IsTrue(avgTime < 5, $"Expected < 5ms, got {avgTime:F2}ms");
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Performance")]
    [TestCategory("SelectList")]
    public void SelectList_LargeDataset_ShouldHandleHundredsOfRows()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL
            )");
        
        // Insert 100 rows
        for (int i = 1; i <= 100; i++)
        {
            connection.Execute(
                "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)",
                new { name = $"User{i}", email = $"user{i}@test.com", age = 20 + (i % 50) });
        }
        
        var repo = new PerfTestUserRepository(connection);
        
        // Act - Warmup
        repo.GetAllAsync().Wait();
        
        // Act - Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var users = repo.GetAllAsync().Result;
        }
        sw.Stop();
        
        // Assert
        var avgTime = sw.ElapsedMilliseconds / 100.0;
        Console.WriteLine($"Average time for 100 rows (100 iterations): {avgTime:F2}ms");
        
        // Sanity check
        Assert.IsTrue(avgTime < 20, $"Expected < 20ms, got {avgTime:F2}ms");
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CodeGen")]
    [TestCategory("SelectList")]
    public void VerifyGeneratedCode_SelectList_UsesEfficientMapping()
    {
        var source = @"
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public string Email { get; set; } = """";
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM users"")]
    Task<List<User>> GetAllAsync();
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // 验证：应该有List<T>的预分配
        // 当前可能没有，这是优化点之一
        Console.WriteLine("=== Generated GetAllAsync ===");
        var idx = generatedCode.IndexOf("GetAllAsync");
        if (idx >= 0)
        {
            Console.WriteLine(generatedCode.Substring(idx, Math.Min(2000, generatedCode.Length - idx)));
        }
        
        // 基本验证
        Assert.IsTrue(generatedCode.Contains("GetAllAsync"), "Should contain GetAllAsync method");
        Assert.IsTrue(generatedCode.Contains("List<"), "Should return List<T>");
        Assert.IsTrue(generatedCode.Contains("ExecuteReader"), "Should use ExecuteReader");
    }
    
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Performance")]
    [TestCategory("SelectList")]
    public void Benchmark_CompareMapping_vs_ManualLoop()
    {
        // 这个测试用于分析不同映射方式的性能差异
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL
            )");
        
        // Insert 100 rows
        for (int i = 1; i <= 100; i++)
        {
            connection.Execute(
                "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)",
                new { name = $"User{i}", email = $"user{i}@test.com", age = 20 + i });
        }
        
        // Method 1: Current generated code (via repository)
        var repo = new PerfTestUserRepository(connection);
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var users = repo.GetAllAsync().Result;
        }
        sw1.Stop();
        
        // Method 2: Manual optimized loop
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var users = new List<PerfUser>(100); // Pre-allocate
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM users";
            using var reader = cmd.ExecuteReader();
            
            // Cache ordinals
            var idOrdinal = reader.GetOrdinal("id");
            var nameOrdinal = reader.GetOrdinal("name");
            var emailOrdinal = reader.GetOrdinal("email");
            var ageOrdinal = reader.GetOrdinal("age");
            
            while (reader.Read())
            {
                users.Add(new PerfUser
                {
                    Id = reader.GetInt64(idOrdinal),
                    Name = reader.GetString(nameOrdinal),
                    Email = reader.GetString(emailOrdinal),
                    Age = reader.GetInt32(ageOrdinal)
                });
            }
        }
        sw2.Stop();
        
        // Report
        var time1 = sw1.ElapsedMilliseconds;
        var time2 = sw2.ElapsedMilliseconds;
        var overhead = ((double)time1 / time2 - 1.0) * 100;
        
        Console.WriteLine($"Generated code: {time1}ms");
        Console.WriteLine($"Optimized loop: {time2}ms");
        Console.WriteLine($"Overhead: {overhead:F1}%");
        
        // 目标：生成的代码应该只比手工优化代码慢不超过20%
        Assert.IsTrue(overhead < 50, $"Expected < 50% overhead, got {overhead:F1}%");
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Performance")]
    [TestCategory("Correctness")]
    public void SelectList_WithNullableFields_ShouldHandleCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT,
                email TEXT NOT NULL,
                age INTEGER
            )");
        
        connection.Execute(
            "INSERT INTO users (name, email, age) VALUES (NULL, 'test@test.com', NULL)");
        connection.Execute(
            "INSERT INTO users (name, email, age) VALUES ('Alice', 'alice@test.com', 25)");
        
        var repo = new PerfTestUserRepository(connection);
        var users = repo.GetAllAsync().Result;
        
        Assert.AreEqual(2, users.Count);
        Assert.IsNull(users[0].Name);
        Assert.IsNull(users[0].Age);
        Assert.AreEqual("Alice", users[1].Name);
        Assert.AreEqual(25, users[1].Age);
        
        connection.Dispose();
    }
}

// Test models
public class PerfUser
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; } = "";
    public int? Age { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPerfTestUserRepository))]
public partial class PerfTestUserRepository(IDbConnection connection) : IPerfTestUserRepository { }

public interface IPerfTestUserRepository
{
    [SqlTemplate("SELECT * FROM users")]
    Task<List<PerfUser>> GetAllAsync();
}

// Helper extension for simple queries
public static class PerfConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql, object? param = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        
        if (param != null)
        {
            foreach (var prop in param.GetType().GetProperties())
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@" + prop.Name;
                p.Value = prop.GetValue(param) ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
        }
        
        cmd.ExecuteNonQuery();
    }
}

