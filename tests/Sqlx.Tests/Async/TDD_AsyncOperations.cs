using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Async;

/// <summary>
/// TDD: 异步操作测试
/// 验证Task<T>, Task<List<T>>, 并发等场景
/// </summary>
[TestClass]
public class TDD_AsyncOperations
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Core")]
    public async Task Async_SelectSingle_ShouldReturnCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        
        var repo = new AsyncTestRepository(connection);
        
        // Act
        var user = await repo.GetUserByIdAsync(1);
        
        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Core")]
    public async Task Async_SelectList_ShouldReturnMultipleItems()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        connection.Execute("INSERT INTO users (name) VALUES ('Bob')");
        connection.Execute("INSERT INTO users (name) VALUES ('Charlie')");
        
        var repo = new AsyncTestRepository(connection);
        
        // Act
        var users = await repo.GetAllUsersAsync();
        
        // Assert
        Assert.AreEqual(3, users.Count);
        Assert.IsTrue(users.Any(u => u.Name == "Alice"));
        Assert.IsTrue(users.Any(u => u.Name == "Bob"));
        Assert.IsTrue(users.Any(u => u.Name == "Charlie"));
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Core")]
    public async Task Async_Insert_ShouldReturnAffectedRows()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        var repo = new AsyncTestRepository(connection);
        
        // Act
        var affected = await repo.InsertUserAsync("NewUser");
        
        // Assert
        Assert.AreEqual(1, affected);
        
        var users = await repo.GetAllUsersAsync();
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("NewUser", users[0].Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Core")]
    public async Task Async_Update_ShouldModifyRecord()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (name) VALUES ('OldName')");
        
        var repo = new AsyncTestRepository(connection);
        
        // Act
        var affected = await repo.UpdateUserAsync(1, "NewName");
        
        // Assert
        Assert.AreEqual(1, affected);
        
        var user = await repo.GetUserByIdAsync(1);
        Assert.AreEqual("NewName", user!.Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Core")]
    public async Task Async_Delete_ShouldRemoveRecord()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (name) VALUES ('ToDelete')");
        
        var repo = new AsyncTestRepository(connection);
        
        // Act
        var affected = await repo.DeleteUserAsync(1);
        
        // Assert
        Assert.AreEqual(1, affected);
        
        var user = await repo.GetUserByIdAsync(1);
        Assert.IsNull(user);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Concurrent")]
    public async Task Async_ConcurrentReads_ShouldNotInterfere()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO users (name) VALUES ('User{i}')");
        }
        
        var repo = new AsyncTestRepository(connection);
        
        // Act: 10 concurrent reads
        var tasks = Enumerable.Range(1, 10)
            .Select(i => repo.GetUserByIdAsync(i))
            .ToArray();
        
        var users = await Task.WhenAll(tasks);
        
        // Assert
        Assert.AreEqual(10, users.Length);
        for (int i = 0; i < 10; i++)
        {
            Assert.IsNotNull(users[i]);
            Assert.AreEqual($"User{i + 1}", users[i]!.Name);
        }
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Async")]
    [TestCategory("Chain")]
    public async Task Async_ChainedOperations_ShouldWorkCorrectly()
    {
        // Test sequential async operations
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        var repo = new AsyncTestRepository(connection);
        
        // Act: Insert -> Read -> Update -> Read -> Delete -> Read
        await repo.InsertUserAsync("Initial");
        
        var user1 = await repo.GetUserByIdAsync(1);
        Assert.AreEqual("Initial", user1!.Name);
        
        await repo.UpdateUserAsync(1, "Updated");
        
        var user2 = await repo.GetUserByIdAsync(1);
        Assert.AreEqual("Updated", user2!.Name);
        
        await repo.DeleteUserAsync(1);
        
        var user3 = await repo.GetUserByIdAsync(1);
        Assert.IsNull(user3);
        
        connection.Dispose();
    }
}

// Test models
public class AsyncTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAsyncTestRepository))]
public partial class AsyncTestRepository(IDbConnection connection) : IAsyncTestRepository { }

public interface IAsyncTestRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<AsyncTestUser?> GetUserByIdAsync(long id);
    
    [SqlTemplate("SELECT * FROM users")]
    Task<List<AsyncTestUser>> GetAllUsersAsync();
    
    [SqlTemplate("INSERT INTO users (name) VALUES (@name)")]
    Task<int> InsertUserAsync(string name);
    
    [SqlTemplate("UPDATE users SET name = @name WHERE id = @id")]
    Task<int> UpdateUserAsync(long id, string name);
    
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteUserAsync(long id);
}

// Helper extension
public static class AsyncConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

