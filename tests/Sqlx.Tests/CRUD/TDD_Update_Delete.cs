using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CRUD;

/// <summary>
/// TDD: UPDATE和DELETE操作的完整测试覆盖
/// </summary>
[TestClass]
public class TDD_Update_Delete
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Update")]
    public void Update_SingleEntity_ShouldModifyRecord()
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
        
        connection.Execute(
            "INSERT INTO users (name, email, age) VALUES ('Alice', 'alice@test.com', 25)");
        
        var repo = new CrudTestUserRepository(connection);
        
        // Act
        var affected = repo.UpdateUserAsync(1, "Alice Smith", "alice.smith@test.com", 26).Result;
        
        // Assert
        Assert.AreEqual(1, affected);
        
        var user = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice Smith", user.Name);
        Assert.AreEqual("alice.smith@test.com", user.Email);
        Assert.AreEqual(26, user.Age);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Update")]
    public void Update_NonExistentEntity_ShouldReturnZero()
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
        
        var repo = new CrudTestUserRepository(connection);
        
        // Act
        var affected = repo.UpdateUserAsync(999, "Nobody", "nobody@test.com", 0).Result;
        
        // Assert
        Assert.AreEqual(0, affected);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Delete")]
    public void Delete_ExistingEntity_ShouldRemoveRecord()
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
        
        connection.Execute(
            "INSERT INTO users (name, email, age) VALUES ('Bob', 'bob@test.com', 30)");
        
        var repo = new CrudTestUserRepository(connection);
        
        // Act
        var affected = repo.DeleteUserAsync(1).Result;
        
        // Assert
        Assert.AreEqual(1, affected);
        
        var user = repo.GetByIdAsync(1).Result;
        Assert.IsNull(user);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Delete")]
    public void Delete_NonExistentEntity_ShouldReturnZero()
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
        
        var repo = new CrudTestUserRepository(connection);
        
        // Act
        var affected = repo.DeleteUserAsync(999).Result;
        
        // Assert
        Assert.AreEqual(0, affected);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Update")]
    public void Update_WithWhereClause_ShouldModifyMatchingRecords()
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
        
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Alice', 'alice@test.com', 25)");
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Bob', 'bob@test.com', 25)");
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Charlie', 'charlie@test.com', 30)");
        
        var repo = new CrudTestUserRepository(connection);
        
        // Act: Update all users with age=25
        var affected = repo.UpdateAgeByAgeAsync(25, 26).Result;
        
        // Assert
        Assert.AreEqual(2, affected);
        
        var users = repo.GetAllAsync().Result;
        Assert.AreEqual(2, users.FindAll(u => u.Age == 26).Count);
        Assert.AreEqual(1, users.FindAll(u => u.Age == 30).Count);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Delete")]
    public void Delete_WithWhereClause_ShouldRemoveMatchingRecords()
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
        
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Alice', 'alice@test.com', 25)");
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Bob', 'bob@test.com', 25)");
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Charlie', 'charlie@test.com', 30)");
        
        var repo = new CrudTestUserRepository(connection);
        
        // Act: Delete all users with age < 30
        var affected = repo.DeleteByAgeAsync(30).Result;
        
        // Assert
        Assert.AreEqual(2, affected);
        
        var users = repo.GetAllAsync().Result;
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("Charlie", users[0].Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Complete")]
    public void FullCRUD_Lifecycle_ShouldWork()
    {
        // Complete CRUD test
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL
            )");
        
        var repo = new CrudTestUserRepository(connection);
        
        // CREATE
        connection.Execute("INSERT INTO users (name, email, age) VALUES ('Alice', 'alice@test.com', 25)");
        var users = repo.GetAllAsync().Result;
        Assert.AreEqual(1, users.Count);
        
        // READ
        var user = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.Name);
        
        // UPDATE
        var updated = repo.UpdateUserAsync(1, "Alice Smith", "alice.smith@test.com", 26).Result;
        Assert.AreEqual(1, updated);
        user = repo.GetByIdAsync(1).Result;
        Assert.AreEqual("Alice Smith", user!.Name);
        Assert.AreEqual(26, user.Age);
        
        // DELETE
        var deleted = repo.DeleteUserAsync(1).Result;
        Assert.AreEqual(1, deleted);
        user = repo.GetByIdAsync(1).Result;
        Assert.IsNull(user);
        
        connection.Dispose();
    }
}

// Test models
public class CrudTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICrudTestUserRepository))]
public partial class CrudTestUserRepository(IDbConnection connection) : ICrudTestUserRepository { }

public interface ICrudTestUserRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<CrudTestUser?> GetByIdAsync(long id);
    
    [SqlTemplate("SELECT * FROM users")]
    Task<List<CrudTestUser>> GetAllAsync();
    
    [SqlTemplate("UPDATE users SET name = @name, email = @email, age = @age WHERE id = @id")]
    Task<int> UpdateUserAsync(long id, string name, string email, int age);
    
    [SqlTemplate("UPDATE users SET age = @newAge WHERE age = @oldAge")]
    Task<int> UpdateAgeByAgeAsync(int oldAge, int newAge);
    
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteUserAsync(long id);
    
    [SqlTemplate("DELETE FROM users WHERE age < @maxAge")]
    Task<int> DeleteByAgeAsync(int maxAge);
}

// Helper extension
public static class CrudConnectionExtensions
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

