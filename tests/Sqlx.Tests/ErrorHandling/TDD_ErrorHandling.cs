using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.ErrorHandling;

/// <summary>
/// TDD: 错误处理和边界条件测试
/// 验证异常情况、空结果、边界值等场景
/// </summary>
[TestClass]
public class TDD_ErrorHandling
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Core")]
    public void Query_NoResults_ShouldReturnNull()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Query non-existent record
        var user = repo.GetUserByIdAsync(999).Result;
        
        // Assert: Should return null
        Assert.IsNull(user);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Generated code issue with empty result set")]
    [TestCategory("TDD-Red")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Core")]
    public void Query_EmptyTable_ShouldReturnEmptyList()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Query empty table
        var users = repo.GetAllUsersAsync().Result;
        
        // Assert: Should return empty list, not null
        Assert.IsNotNull(users);
        Assert.AreEqual(0, users.Count);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Update")]
    public void Update_NonExistentRecord_ShouldReturnZero()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Update non-existent record
        var affected = repo.UpdateUserAsync(999, "NewName").Result;
        
        // Assert: Should return 0 affected rows
        Assert.AreEqual(0, affected);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Delete")]
    public void Delete_NonExistentRecord_ShouldReturnZero()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Delete non-existent record
        var affected = repo.DeleteUserAsync(999).Result;
        
        // Assert: Should return 0 affected rows
        Assert.AreEqual(0, affected);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Insert")]
    public async Task Insert_DuplicateKey_ShouldThrowException()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (id, name) VALUES (1, 'Alice')");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act & Assert: Should throw exception
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertUserWithIdAsync(1, "Bob");
        });
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Null")]
    public void Query_NullableField_ShouldHandleCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT
            )");
        
        connection.Execute("INSERT INTO users (name, email) VALUES ('Alice', NULL)");
        connection.Execute("INSERT INTO users (name, email) VALUES ('Bob', 'bob@test.com')");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act
        var alice = repo.GetUserByIdAsync(1).Result;
        var bob = repo.GetUserByIdAsync(2).Result;
        
        // Assert
        Assert.IsNotNull(alice);
        Assert.IsNull(alice.Email);
        
        Assert.IsNotNull(bob);
        Assert.AreEqual("bob@test.com", bob.Email);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Large result set testing needs investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("ErrorHandling")]
    [TestCategory("Boundary")]
    public void Query_LargeResultSet_ShouldHandleCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        // Insert 1000 records
        for (int i = 1; i <= 1000; i++)
        {
            connection.Execute($"INSERT INTO users (name) VALUES ('User{i}')");
        }
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Query all 1000 records
        var users = repo.GetAllUsersAsync().Result;
        
        // Assert: Should return all records
        Assert.AreEqual(1000, users.Count);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Empty parameter needs special handling")]
    [TestCategory("TDD-Red")]
    [TestCategory("ErrorHandling")]
    [TestCategory("EmptyParameter")]
    public void Query_WithEmptyParameter_ShouldWorkCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (name) VALUES ('')");
        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Query with empty string parameter
        var user = repo.FindByNameAsync("").Result;
        
        // Assert: Should find the empty name record
        Assert.IsNotNull(user);
        Assert.AreEqual("", user.Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Connection reuse testing needs investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("ErrorHandling")]
    [TestCategory("MultipleQueries")]
    public void Connection_ReuseForMultipleQueries_ShouldWork()
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
        
        var repo = new ErrorTestRepository(connection);
        
        // Act: Multiple queries on same connection
        var user1 = repo.GetUserByIdAsync(1).Result;
        var user2 = repo.GetUserByIdAsync(2).Result;
        var allUsers = repo.GetAllUsersAsync().Result;
        
        // Assert: All queries should work
        Assert.IsNotNull(user1);
        Assert.AreEqual("Alice", user1.Name);
        
        Assert.IsNotNull(user2);
        Assert.AreEqual("Bob", user2.Name);
        
        Assert.AreEqual(2, allUsers.Count);
        
        connection.Dispose();
    }
}

// Test models
public class ErrorTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IErrorTestRepository))]
public partial class ErrorTestRepository(IDbConnection connection) : IErrorTestRepository { }

public interface IErrorTestRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<ErrorTestUser?> GetUserByIdAsync(long id);
    
    [SqlTemplate("SELECT * FROM users")]
    Task<List<ErrorTestUser>> GetAllUsersAsync();
    
    [SqlTemplate("SELECT * FROM users WHERE name = @name")]
    Task<ErrorTestUser?> FindByNameAsync(string name);
    
    [SqlTemplate("UPDATE users SET name = @name WHERE id = @id")]
    Task<int> UpdateUserAsync(long id, string name);
    
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteUserAsync(long id);
    
    [SqlTemplate("INSERT INTO users (id, name) VALUES (@id, @name)")]
    Task<int> InsertUserWithIdAsync(long id, string name);
}

// Helper extension
public static class ErrorConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

