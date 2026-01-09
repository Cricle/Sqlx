using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

/// <summary>
/// TDD: 验证BatchInsert在指定列名的情况下正确工作（模拟Benchmark场景）
/// </summary>
[TestClass]
public class TDD_BatchInsert_ColumnParsing : CodeGenerationTestBase
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("BatchInsert")]
    [TestCategory("ColumnParsing")]
    public void BatchInsert_WithExplicitColumns_ExcludingIdAndCreatedAt_ShouldWork()
    {
        // First, verify code generation for this specific setup
        var source = @"
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

public class TestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public string Email { get; set; } = """";
    public int Age { get; set; }
    public bool IsActive { get; set; } = true;
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        Console.WriteLine("=== Generated BatchInsertAsync (Parameter binding section) ===");
        var idx = generatedCode.IndexOf("__itemIndex__ = 0;");
        if (idx >= 0)
        {
            Console.WriteLine(generatedCode.Substring(idx, Math.Min(3000, generatedCode.Length - idx)));
        }
        Console.WriteLine("=== END ===");

        // Arrange: 模拟Benchmark的表结构（包含auto-increment id和default timestamp）
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER DEFAULT 1,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP
            )");

        var repo = new TestUserRepository(connection);

        var users = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25, IsActive = true },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30, IsActive = false },
            new() { Name = "Charlie", Email = "charlie@test.com", Age = 35, IsActive = true }
        };

        // Act
        int affected;
        try
        {
            affected = repo.BatchInsertAsync(users).Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CAUGHT EXCEPTION: {ex.Message}");
            Console.WriteLine($"Inner: {ex.InnerException?.Message}");
            throw;
        }

        // Assert
        Assert.AreEqual(3, affected, "应该插入3行");

        // 验证数据正确插入
        var allUsers = repo.GetAllAsync().Result;
        Assert.AreEqual(3, allUsers.Count);
        Assert.AreEqual("Alice", allUsers[0].Name);
        Assert.AreEqual("Bob", allUsers[1].Name);
        Assert.AreEqual("Charlie", allUsers[2].Name);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("BatchInsert")]
    [TestCategory("ColumnParsing")]
    public void BatchInsert_WithExplicitColumns_LargerBatch_ShouldWork()
    {
        // Arrange: 测试较大批次（10个，模拟Benchmark RowCount=10）
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER DEFAULT 1,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP
            )");

        var repo = new TestUserRepository(connection);

        var users = Enumerable.Range(1, 10)
            .Select(i => new TestUser
            {
                Name = $"User{i}",
                Email = $"user{i}@test.com",
                Age = 20 + i,
                IsActive = i % 2 == 0
            })
            .ToList();

        // Act
        var affected = repo.BatchInsertAsync(users).Result;

        // Assert
        Assert.AreEqual(10, affected, "应该插入10行");

        var allUsers = repo.GetAllAsync().Result;
        Assert.AreEqual(10, allUsers.Count);
        Assert.AreEqual("User1", allUsers[0].Name);
        Assert.AreEqual("User10", allUsers[9].Name);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("BatchInsert")]
    [TestCategory("ColumnParsing")]
    public void BatchInsert_WithExplicitColumns_100Items_ShouldWork()
    {
        // Arrange: 测试100个项目（模拟Benchmark RowCount=100）
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER DEFAULT 1,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP
            )");

        var repo = new TestUserRepository(connection);

        var users = Enumerable.Range(1, 100)
            .Select(i => new TestUser
            {
                Name = $"User{i}",
                Email = $"user{i}@test.com",
                Age = 20 + (i % 50),
                IsActive = i % 3 != 0
            })
            .ToList();

        // Act
        var affected = repo.BatchInsertAsync(users).Result;

        // Assert
        Assert.AreEqual(100, affected, "应该插入100行");

        var count = connection.QueryScalar<int>("SELECT COUNT(*) FROM users");
        Assert.AreEqual(100, count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("BatchInsert")]
    [TestCategory("CodeGen")]
    public void VerifyGeneratedCode_ParsesColumnsCorrectly()
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
    public bool IsActive { get; set; } = true;
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO users (name, email, age, is_active) VALUES {{batch_values @users}}"")]
    [BatchOperation(MaxBatchSize = 100)]
    Task<int> BatchInsertAsync(IEnumerable<User> users);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);

        // 验证：VALUES子句包含4个参数（name, email, age, is_active）
        Assert.IsTrue(generatedCode.Contains("(@name{__itemIndex__}, @email{__itemIndex__}, @age{__itemIndex__}, @is_active{__itemIndex__})"),
            "VALUES子句应该包含4个参数");

        // 验证：为每个属性创建参数
        Assert.IsTrue(generatedCode.Contains("__p__.ParameterName = $\"@name{__itemIndex__}\""), "应该创建name参数");
        Assert.IsTrue(generatedCode.Contains("__p__.ParameterName = $\"@email{__itemIndex__}\""), "应该创建email参数");
        Assert.IsTrue(generatedCode.Contains("__p__.ParameterName = $\"@age{__itemIndex__}\""), "应该创建age参数");
        Assert.IsTrue(generatedCode.Contains("__p__.ParameterName = $\"@is_active{__itemIndex__}\""), "应该创建is_active参数");

        // 验证：不包含Id参数（因为SQL中没有指定）
        Assert.IsFalse(generatedCode.Contains("@id{__itemIndex__}"), "不应该包含id参数");
    }
}

// Test models are defined in TestUserModels.cs

// Helper extension for simple queries
public static class ConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public static T QueryScalar<T>(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        return (T)Convert.ChangeType(result, typeof(T));
    }
}

