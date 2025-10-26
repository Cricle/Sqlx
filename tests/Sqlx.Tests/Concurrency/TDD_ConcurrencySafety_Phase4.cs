using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Concurrency;

/// <summary>
/// Phase 4 Batch 6: 并发线程安全测试
/// 新增8个并发场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Concurrency")]
[TestCategory("Phase4")]
public class TDD_ConcurrencySafety_Phase4
{
    [TestMethod]
    [Description("Multiple parallel reads should work")]
    public async Task Concurrent_ParallelReads_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");
        for (int i = 1; i <= 100; i++)
        {
            ExecuteSql(connection, $"INSERT INTO test VALUES ({i}, {i * 10})");
        }

        var repo = new ConcurrencyRepository(connection);

        // Act - 10 parallel reads
        var tasks = Enumerable.Range(1, 10).Select(_ => repo.GetAllAsync()).ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.AreEqual(100, result.Count);
        }
    }

    [TestMethod]
    [Description("Multiple parallel writes should work")]
    public async Task Concurrent_ParallelWrites_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");

        var repo = new ConcurrencyRepository(connection);

        // Act - 10 parallel inserts
        var tasks = new List<Task<int>>();
        for (int i = 1; i <= 10; i++)
        {
            int id = i;
            tasks.Add(repo.InsertAsync(id, id * 10));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all should succeed
        Assert.IsTrue(results.All(r => r == 1));

        var allRecords = await repo.GetAllAsync();
        Assert.AreEqual(10, allRecords.Count);
    }

    [TestMethod]
    [Description("Parallel read and write should work")]
    public async Task Concurrent_MixedReadWrite_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");
        for (int i = 1; i <= 50; i++)
        {
            ExecuteSql(connection, $"INSERT INTO test VALUES ({i}, {i * 10})");
        }

        var repo = new ConcurrencyRepository(connection);

        // Act - 5 reads and 5 writes in parallel
        var readTasks = Enumerable.Range(1, 5).Select(_ => repo.GetAllAsync()).ToList();
        var writeTasks = Enumerable.Range(51, 5).Select(i => repo.InsertAsync(i, i * 10)).ToList();

        var allTasks = readTasks.Cast<Task>().Concat(writeTasks).ToList();
        await Task.WhenAll(allTasks);

        // Assert
        var finalRecords = await repo.GetAllAsync();
        Assert.AreEqual(55, finalRecords.Count);
    }

    [TestMethod]
    [Description("Parallel updates to same record should work")]
    public async Task Concurrent_ParallelUpdates_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");
        ExecuteSql(connection, "INSERT INTO test VALUES (1, 0)");

        var repo = new ConcurrencyRepository(connection);

        // Act - 10 parallel updates to same record
        var tasks = Enumerable.Range(1, 10).Select(i => repo.UpdateAsync(1, i)).ToList();
        await Task.WhenAll(tasks);

        // Assert - record exists and has some value
        var record = await repo.GetByIdAsync(1);
        Assert.IsNotNull(record);
        Assert.IsTrue(record.value > 0 && record.value <= 10);
    }

    [TestMethod]
    [Description("Task.WhenAll with multiple queries should work")]
    public async Task Concurrent_TaskWhenAll_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");
        for (int i = 1; i <= 20; i++)
        {
            ExecuteSql(connection, $"INSERT INTO test VALUES ({i}, {i * 10})");
        }

        var repo = new ConcurrencyRepository(connection);

        // Act - Query different ranges in parallel
        var task1 = repo.GetByRangeAsync(1, 5);
        var task2 = repo.GetByRangeAsync(6, 10);
        var task3 = repo.GetByRangeAsync(11, 15);
        var task4 = repo.GetByRangeAsync(16, 20);

        await Task.WhenAll(task1, task2, task3, task4);

        // Assert
        Assert.AreEqual(5, task1.Result.Count);
        Assert.AreEqual(5, task2.Result.Count);
        Assert.AreEqual(5, task3.Result.Count);
        Assert.AreEqual(5, task4.Result.Count);
    }

    [TestMethod]
    [Description("Sequential operations after parallel should work")]
    public async Task Concurrent_SequentialAfterParallel_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");

        var repo = new ConcurrencyRepository(connection);

        // Act - Parallel inserts
        var insertTasks = Enumerable.Range(1, 10).Select(i => repo.InsertAsync(i, i * 10)).ToList();
        await Task.WhenAll(insertTasks);

        // Act - Sequential read
        var allRecords = await repo.GetAllAsync();

        // Act - Sequential update
        await repo.UpdateAsync(1, 999);

        // Assert
        Assert.AreEqual(10, allRecords.Count);
        var updated = await repo.GetByIdAsync(1);
        Assert.AreEqual(999, updated!.value);
    }

    [TestMethod]
    [Description("High concurrency (50 parallel operations) should work")]
    public async Task Concurrent_HighConcurrency_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");

        var repo = new ConcurrencyRepository(connection);

        // Act - 50 parallel inserts
        var tasks = Enumerable.Range(1, 50).Select(i => repo.InsertAsync(i, i * 10)).ToList();
        await Task.WhenAll(tasks);

        // Assert
        var allRecords = await repo.GetAllAsync();
        Assert.AreEqual(50, allRecords.Count);
    }

    [TestMethod]
    [Description("Concurrent deletes should work")]
    public async Task Concurrent_ParallelDeletes_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE test (id INTEGER PRIMARY KEY, value INTEGER)");
        for (int i = 1; i <= 20; i++)
        {
            ExecuteSql(connection, $"INSERT INTO test VALUES ({i}, {i * 10})");
        }

        var repo = new ConcurrencyRepository(connection);

        // Act - Delete 10 records in parallel
        var deleteTasks = Enumerable.Range(1, 10).Select(i => repo.DeleteAsync(i)).ToList();
        await Task.WhenAll(deleteTasks);

        // Assert - 10 records should remain
        var remaining = await repo.GetAllAsync();
        Assert.AreEqual(10, remaining.Count);
    }

    private void ExecuteSql(IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

// Repository interface
public interface IConcurrencyRepository
{
    [SqlTemplate("SELECT * FROM test")]
    Task<List<ConcurrencyModel>> GetAllAsync();

    [SqlTemplate("SELECT * FROM test WHERE id = @id")]
    Task<ConcurrencyModel?> GetByIdAsync(int id);

    [SqlTemplate("SELECT * FROM test WHERE id >= @minId AND id <= @maxId")]
    Task<List<ConcurrencyModel>> GetByRangeAsync(int minId, int maxId);

    [SqlTemplate("INSERT INTO test (id, value) VALUES (@id, @value)")]
    Task<int> InsertAsync(int id, int value);

    [SqlTemplate("UPDATE test SET value = @value WHERE id = @id")]
    Task<int> UpdateAsync(int id, int value);

    [SqlTemplate("DELETE FROM test WHERE id = @id")]
    Task<int> DeleteAsync(int id);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IConcurrencyRepository))]
public partial class ConcurrencyRepository(IDbConnection connection) : IConcurrencyRepository { }

// Model
public class ConcurrencyModel
{
    public int id { get; set; }
    public int value { get; set; }
}

