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
/// Phase 4 Batch 7: 性能极限测试
/// 新增8个极限场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Performance")]
[TestCategory("Phase4")]
public class TDD_PerformanceLimit_Phase4
{
    [TestMethod]
    [Description("Query 10K records should be fast")]
    public async Task Performance_Query10K_ShouldBeFast()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");

        // Insert 10K records
        for (int i = 1; i <= 10000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO perf_test VALUES ({i}, {i * 10}, 'Item{i}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var sw = Stopwatch.StartNew();
        var results = await repo.GetAllAsync();
        sw.Stop();

        // Assert
        Assert.AreEqual(10000, results.Count);
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Query took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [TestMethod]
    [Description("Batch insert 1K records should work")]
    public async Task Performance_BatchInsert1K_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");

        var repo = new PerformanceRepository(connection);

        // Act - Insert 1K records
        for (int i = 1; i <= 1000; i++)
        {
            await repo.InsertAsync(i, i * 10, $"Item{i}");
        }

        // Assert
        var count = await repo.CountAsync();
        Assert.AreEqual(1000, count);
    }

    [TestMethod]
    [Description("Query with complex WHERE should be fast")]
    public async Task Performance_ComplexWhere_ShouldBeFast()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");
        ExecuteSql(connection, "CREATE INDEX idx_value ON perf_test(value)");

        for (int i = 1; i <= 5000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO perf_test VALUES ({i}, {i * 10}, 'Item{i}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var sw = Stopwatch.StartNew();
        var results = await repo.GetByComplexWhereAsync(1000, 4000, "Item2%");
        sw.Stop();

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Complex WHERE took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    [Description("JOIN with large dataset should work")]
    public async Task Performance_LargeJoin_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, @"
            CREATE TABLE t1 (id INTEGER PRIMARY KEY, value INTEGER);
            CREATE TABLE t2 (id INTEGER PRIMARY KEY, t1_id INTEGER, name TEXT);
        ");

        // Insert 1K records in each table
        for (int i = 1; i <= 1000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO t1 VALUES ({i}, {i * 10})");
            ExecuteSql(connection, $"INSERT INTO t2 VALUES ({i}, {i}, 'Name{i}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var sw = Stopwatch.StartNew();
        var results = await repo.GetJoinResultsAsync();
        sw.Stop();

        // Assert
        Assert.AreEqual(1000, results.Count);
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"JOIN took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    [Description("Subquery with large dataset should work")]
    public async Task Performance_LargeSubquery_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");

        for (int i = 1; i <= 2000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO perf_test VALUES ({i}, {i * 10}, 'Item{i}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var results = await repo.GetWithSubqueryAsync();

        // Assert
        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    [Description("Aggregate on large dataset should be fast")]
    public async Task Performance_LargeAggregate_ShouldBeFast()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");

        for (int i = 1; i <= 5000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO perf_test VALUES ({i}, {i * 10}, 'Item{i % 10}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var sw = Stopwatch.StartNew();
        var results = await repo.GetAggregateByNameAsync();
        sw.Stop();

        // Assert
        Assert.AreEqual(10, results.Count); // 10 unique names
        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Aggregate took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    [Description("Update large batch should work")]
    public async Task Performance_UpdateLargeBatch_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");

        for (int i = 1; i <= 1000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO perf_test VALUES ({i}, {i * 10}, 'Item{i}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var updated = await repo.UpdateBatchAsync(99999); // Update all where value < 99999

        // Assert
        Assert.AreEqual(1000, updated); // All records updated
    }

    [TestMethod]
    [Description("Delete large batch should work")]
    public async Task Performance_DeleteLargeBatch_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        ExecuteSql(connection, "CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER, name TEXT)");

        for (int i = 1; i <= 2000; i++)
        {
            ExecuteSql(connection, $"INSERT INTO perf_test VALUES ({i}, {i * 10}, 'Item{i}')");
        }

        var repo = new PerformanceRepository(connection);

        // Act
        var deleted = await repo.DeleteByRangeAsync(1, 1000);

        // Assert
        Assert.IsTrue(deleted >= 1000);

        var remaining = await repo.CountAsync();
        Assert.AreEqual(1000, remaining);
    }

    private void ExecuteSql(IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

// Repository interface
public interface IPerformanceRepository
{
    [SqlTemplate("SELECT * FROM perf_test")]
    Task<List<PerfModel>> GetAllAsync();

    [SqlTemplate("SELECT COUNT(*) FROM perf_test")]
    Task<int> CountAsync();

    [SqlTemplate("INSERT INTO perf_test (id, value, name) VALUES (@id, @value, @name)")]
    Task<int> InsertAsync(int id, int value, string name);

    [SqlTemplate("SELECT * FROM perf_test WHERE value >= @minValue AND value <= @maxValue AND name LIKE @pattern")]
    Task<List<PerfModel>> GetByComplexWhereAsync(int minValue, int maxValue, string pattern);

    [SqlTemplate("SELECT t1.id, t1.value, t2.name FROM t1 INNER JOIN t2 ON t1.id = t2.t1_id")]
    Task<List<JoinModel>> GetJoinResultsAsync();

    [SqlTemplate("SELECT * FROM perf_test WHERE value > (SELECT AVG(value) FROM perf_test)")]
    Task<List<PerfModel>> GetWithSubqueryAsync();

    [SqlTemplate("SELECT name, COUNT(*) as count, SUM(value) as total FROM perf_test GROUP BY name")]
    Task<List<AggModel>> GetAggregateByNameAsync();

    [SqlTemplate("UPDATE perf_test SET value = @newValue WHERE value < @newValue")]
    Task<int> UpdateBatchAsync(int newValue);

    [SqlTemplate("DELETE FROM perf_test WHERE id >= @minId AND id <= @maxId")]
    Task<int> DeleteByRangeAsync(int minId, int maxId);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPerformanceRepository))]
public partial class PerformanceRepository(IDbConnection connection) : IPerformanceRepository { }

// Models
public class PerfModel
{
    public int id { get; set; }
    public int value { get; set; }
    public string name { get; set; } = "";
}

public class JoinModel
{
    public int id { get; set; }
    public int value { get; set; }
    public string name { get; set; } = "";
}

public class AggModel
{
    public string name { get; set; } = "";
    public int? count { get; set; }
    public int? total { get; set; }
}

