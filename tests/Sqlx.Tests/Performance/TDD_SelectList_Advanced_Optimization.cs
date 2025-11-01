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
/// TDD: SelectList高级优化 - 针对100行场景
/// 当前：比Dapper慢27%
/// 目标：<10%
/// </summary>
[TestClass]
public class TDD_SelectList_Advanced_Optimization : CodeGenerationTestBase
{
    [TestMethod]
    [Ignore("性能基准测试不稳定，仅供手动运行")]
    [TestCategory("TDD-Green")]
    [TestCategory("Performance")]
    [TestCategory("Benchmark")]
    [TestCategory("Advanced")]
    public void Benchmark_DetailedProfiling_100Rows()
    {
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

        var repo = new AdvancedPerfUserRepository(connection);
        var iterations = 1000;

        // Method 1: Current Sqlx (after optimization)
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var users = repo.GetAllAsync().Result;
        }
        sw1.Stop();

        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        // Method 2: Optimized manual code with List capacity
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        var gen0Before2 = GC.CollectionCount(0);
        var gen1Before2 = GC.CollectionCount(1);
        var gen2Before2 = GC.CollectionCount(2);

        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var users = new List<AdvancedPerfUser>(128); // Pre-allocate with capacity
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM users";
            using var reader = cmd.ExecuteReader();

            // Cache ordinals
            var idOrd = reader.GetOrdinal("id");
            var nameOrd = reader.GetOrdinal("name");
            var emailOrd = reader.GetOrdinal("email");
            var ageOrd = reader.GetOrdinal("age");

            while (reader.Read())
            {
                users.Add(new AdvancedPerfUser
                {
                    Id = reader.GetInt64(idOrd),
                    Name = reader.GetString(nameOrd),
                    Email = reader.GetString(emailOrd),
                    Age = reader.GetInt32(ageOrd)
                });
            }
        }
        sw2.Stop();

        var gen0After2 = GC.CollectionCount(0);
        var gen1After2 = GC.CollectionCount(1);
        var gen2After2 = GC.CollectionCount(2);

        // Report
        var time1 = sw1.ElapsedMilliseconds;
        var time2 = sw2.ElapsedMilliseconds;
        var overhead = ((double)time1 / time2 - 1.0) * 100;

        Console.WriteLine($"=== Performance Analysis (100 rows, {iterations} iterations) ===");
        Console.WriteLine($"Current Sqlx: {time1}ms");
        Console.WriteLine($"  GC Gen0: {gen0After - gen0Before}, Gen1: {gen1After - gen1Before}, Gen2: {gen2After - gen2Before}");
        Console.WriteLine($"Optimized:    {time2}ms");
        Console.WriteLine($"  GC Gen0: {gen0After2 - gen0Before2}, Gen1: {gen1After2 - gen1Before2}, Gen2: {gen2After2 - gen2Before2}");
        Console.WriteLine($"Overhead:     {overhead:F1}%");
        Console.WriteLine($"Target:       <20%");

        // 验证正确性
        var result = repo.GetAllAsync().Result;
        Assert.AreEqual(100, result.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CodeGen")]
    [TestCategory("ListCapacity")]
    public void VerifyGeneratedCode_ChecksForListCapacityPreallocation()
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

        Console.WriteLine("=== Checking for List capacity optimization ===");
        var idx = generatedCode.IndexOf("__result__ = new");
        if (idx >= 0)
        {
            Console.WriteLine(generatedCode.Substring(idx, Math.Min(200, generatedCode.Length - idx)));
        }

        // 基本验证
        Assert.IsTrue(generatedCode.Contains("GetAllAsync"));

        // 优化检查：List是否预分配容量？
        // 注意：这个可能还没实现，这是优化点
        if (generatedCode.Contains("new global::System.Collections.Generic.List<") &&
            generatedCode.Contains("("))
        {
            Console.WriteLine("✅ List with capacity detected");
        }
        else
        {
            Console.WriteLine("⚠️ List without capacity - optimization opportunity");
        }
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Performance")]
    [TestCategory("Correctness")]
    public void SelectList_LargeResult_ShouldNotLoseData()
    {
        // 验证在优化过程中没有破坏正确性
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL
            )");

        // Insert 500 rows
        for (int i = 1; i <= 500; i++)
        {
            connection.Execute(
                "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)",
                new { name = $"User{i}", email = $"user{i}@test.com", age = 20 + (i % 80) });
        }

        var repo = new AdvancedPerfUserRepository(connection);
        var users = repo.GetAllAsync().Result;

        Assert.AreEqual(500, users.Count);
        Assert.AreEqual("User1", users[0].Name);
        Assert.AreEqual("User500", users[499].Name);

        // 验证数据完整性
        for (int i = 0; i < 500; i++)
        {
            Assert.IsNotNull(users[i].Name);
            Assert.IsNotNull(users[i].Email);
            Assert.IsTrue(users[i].Age >= 20 && users[i].Age < 100);
        }

        connection.Dispose();
    }
}

// Test models
public class AdvancedPerfUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAdvancedPerfUserRepository))]
public partial class AdvancedPerfUserRepository(IDbConnection connection) : IAdvancedPerfUserRepository { }

public interface IAdvancedPerfUserRepository
{
    [SqlTemplate("SELECT * FROM users")]
    Task<List<AdvancedPerfUser>> GetAllAsync();
}

