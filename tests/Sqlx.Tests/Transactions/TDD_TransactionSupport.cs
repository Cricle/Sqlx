using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Transactions;

/// <summary>
/// TDD: 事务支持测试
/// 验证事务提交、回滚、嵌套等场景
/// </summary>
[TestClass]
public class TDD_TransactionSupport
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("Core")]
    public void Transaction_Commit_ShouldPersistChanges()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        var repo = new TransactionTestRepository(connection);

        // Act: Insert within transaction and commit
        using (var transaction = connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            repo.InsertUserAsync("Alice").Wait();
            repo.InsertUserAsync("Bob").Wait();
            transaction.Commit();
        }
        repo.Transaction = null; // Clear transaction after commit

        // Assert: Data should be persisted
        var users = repo.GetAllUsersAsync().Result;
        Assert.AreEqual(2, users.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("Core")]
    public void Transaction_Rollback_ShouldDiscardChanges()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        var repo = new TransactionTestRepository(connection);

        // Act: Insert within transaction and rollback
        using (var transaction = connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            repo.InsertUserAsync("Alice").Wait();
            repo.InsertUserAsync("Bob").Wait();
            transaction.Rollback();
        }
        repo.Transaction = null; // Clear transaction after rollback

        // Assert: Data should NOT be persisted
        var users = repo.GetAllUsersAsync().Result;
        Assert.AreEqual(0, users.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("Core")]
    public void Transaction_PartialCommit_ShouldWorkCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        var repo = new TransactionTestRepository(connection);

        // Act: Insert some records, commit, then insert more and rollback
        using (var transaction1 = connection.BeginTransaction())
        {
            repo.Transaction = transaction1;
            repo.InsertUserAsync("Alice").Wait();
            transaction1.Commit();
        }

        using (var transaction2 = connection.BeginTransaction())
        {
            repo.Transaction = transaction2;
            repo.InsertUserAsync("Bob").Wait();
            transaction2.Rollback();
        }
        repo.Transaction = null; // Clear transaction

        // Assert: Only first insert should be persisted
        var users = repo.GetAllUsersAsync().Result;
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("Alice", users[0].Name);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("Core")]
    public void Transaction_ExceptionDuringTransaction_ShouldAllowRollback()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        var repo = new TransactionTestRepository(connection);

        // Act: Simulate exception during transaction
        try
        {
            using (var transaction = connection.BeginTransaction())
            {
                repo.Transaction = transaction;
                repo.InsertUserAsync("Alice").Wait();
                throw new InvalidOperationException("Simulated error");
#pragma warning disable CS0162
                transaction.Commit();
#pragma warning restore CS0162
            }
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
        repo.Transaction = null; // Clear transaction

        // Assert: Data should NOT be persisted (implicit rollback)
        var users = repo.GetAllUsersAsync().Result;
        Assert.AreEqual(0, users.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("CRUD")]
    public void Transaction_MultipleOperations_ShouldBeAtomic()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                balance INTEGER NOT NULL DEFAULT 0
            )");

        connection.Execute("INSERT INTO users (name, balance) VALUES ('Alice', 100)");
        connection.Execute("INSERT INTO users (name, balance) VALUES ('Bob', 100)");

        var repo = new TransactionTestRepository(connection);

        // Act: Transfer money between accounts (atomic operation)
        using (var transaction = connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            // Alice gives 50 to Bob
            repo.UpdateBalanceAsync(1, 50).Wait();  // Alice: 100 - 50 = 50
            repo.UpdateBalanceAsync(2, 150).Wait(); // Bob: 100 + 50 = 150
            transaction.Commit();
        }
        repo.Transaction = null; // Clear transaction

        // Assert: Both updates should be applied
        var alice = repo.GetUserWithBalanceAsync(1).Result;
        var bob = repo.GetUserWithBalanceAsync(2).Result;

        Assert.AreEqual(50, alice!.Balance);
        Assert.AreEqual(150, bob!.Balance);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("Batch")]
    public void Transaction_BatchInsert_ShouldBeAtomic()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        var repo = new TransactionTestRepository(connection);

        // Act: Batch insert within transaction
        using (var transaction = connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            for (int i = 1; i <= 10; i++)
            {
                repo.InsertUserAsync($"User{i}").Wait();
            }
            transaction.Commit();
        }
        repo.Transaction = null; // Clear transaction

        // Assert: All 10 records should be inserted
        var users = repo.GetAllUsersAsync().Result;
        Assert.AreEqual(10, users.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Transaction")]
    [TestCategory("Delete")]
    public void Transaction_DeleteWithRollback_ShouldRestoreData()
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

        var repo = new TransactionTestRepository(connection);

        // Act: Delete within transaction and rollback
        using (var transaction = connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            repo.DeleteUserAsync(1).Wait();
            transaction.Rollback();
        }
        repo.Transaction = null; // Clear transaction

        // Assert: Record should still exist
        var user = repo.GetUserByIdAsync(1).Result;
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.Name);

        connection.Dispose();
    }
}

// Test models
public class TransactionTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Balance { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITransactionTestRepository))]
public partial class TransactionTestRepository(IDbConnection connection) : ITransactionTestRepository { }

public interface ITransactionTestRepository
{
    [SqlTemplate("SELECT id, name, 0 as balance FROM users")]
    Task<List<TransactionTestUser>> GetAllUsersAsync();

    [SqlTemplate("SELECT id, name, 0 as balance FROM users WHERE id = @id")]
    Task<TransactionTestUser?> GetUserByIdAsync(long id);

    [SqlTemplate("SELECT id, name, balance FROM users WHERE id = @id")]
    Task<TransactionTestUser?> GetUserWithBalanceAsync(long id);

    [SqlTemplate("INSERT INTO users (name) VALUES (@name)")]
    Task<int> InsertUserAsync(string name);

    [SqlTemplate("UPDATE users SET balance = @balance WHERE id = @id")]
    Task<int> UpdateBalanceAsync(long id, int balance);

    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteUserAsync(long id);
}

// Helper extension
public static class TransactionConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

