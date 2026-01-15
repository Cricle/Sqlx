using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sqlx.Tests;

/// <summary>
/// Concurrency, thread-safety, and performance tests.
/// </summary>
[TestClass]
public class ConcurrencyAndPerformanceTests
{
    #region Thread Safety Tests

    [TestMethod]
    public void SqlQuery_ConcurrentCreation_ThreadSafe()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var query = SqlQuery.ForSqlite<QueryUser>();
            Assert.IsNotNull(query);
            var sql = query.ToSql();
            Assert.IsTrue(sql.Contains("SELECT *"));
        })).ToArray();

        Task.WaitAll(tasks);
    }

    [TestMethod]
    public void SqlQuery_ConcurrentToSql_ThreadSafe()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name);

        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var sql = query.ToSql();
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("ORDER BY"));
        })).ToArray();

        Task.WaitAll(tasks);
    }

    [TestMethod]
    public void SqlTemplate_ConcurrentPrepare_ThreadSafe()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", System.Data.DbType.Int64, false),
            new ColumnMeta("name", "Name", System.Data.DbType.String, false)
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);

        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
            Assert.IsNotNull(template);
            Assert.IsTrue(template.Sql.Contains("SELECT"));
        })).ToArray();

        Task.WaitAll(tasks);
    }

    [TestMethod]
    public void ResultReader_ConcurrentRead_ThreadSafe()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            using var dbReader = new TestDbDataReader(entities);
            var results = reader.ToList(dbReader);
            Assert.AreEqual(100, results.Count);
        })).ToArray();

        Task.WaitAll(tasks);
    }

    #endregion

    #region Parallel Execution Tests

    [TestMethod]
    public void SqlQuery_ParallelExecution_NoDataRaces()
    {
        var results = new ConcurrentBag<string>();

        Parallel.For(0, 100, i =>
        {
            var query = SqlQuery.ForSqlite<QueryUser>()
                .Where(u => u.Id == i)
                .OrderBy(u => u.Name);

            var sql = query.ToSql();
            results.Add(sql);
        });

        Assert.AreEqual(100, results.Count);
        Assert.IsTrue(results.All(sql => sql.Contains("WHERE")));
    }

    [TestMethod]
    public void SqlTemplate_ParallelRender_NoDataRaces()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", System.Data.DbType.Int64, false),
            new ColumnMeta("name", "Name", System.Data.DbType.String, false)
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);

        var results = new ConcurrentBag<string>();

        Parallel.For(0, 100, i =>
        {
            var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = i });
            results.Add(rendered);
        });

        Assert.AreEqual(100, results.Count);
    }

    #endregion

    #region Performance Benchmarks

    [TestMethod]
    public void Performance_ToSql_FastExecution()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .Where(u => u.Age > 18)
            .OrderBy(u => u.Name)
            .Take(10);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var sql = query.ToSql();
        }
        sw.Stop();

        // Should complete 10,000 iterations in less than 1 second
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void Performance_TemplatePrepare_FastExecution()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", System.Data.DbType.Int64, false),
            new ColumnMeta("name", "Name", System.Data.DbType.String, false),
            new ColumnMeta("age", "Age", System.Data.DbType.Int32, false)
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        }
        sw.Stop();

        // Should complete 10,000 iterations in less than 1 second
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void Performance_ResultReaderToList_FastExecution()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            using var dbReader = new TestDbDataReader(entities);
            var results = reader.ToList(dbReader);
        }
        sw.Stop();

        // Should complete 100 iterations of 1000 rows in less than 1 second
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Memory Pressure Tests

    [TestMethod]
    public void Memory_LargeQueryChain_NoMemoryLeak()
    {
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < 1000; i++)
        {
            var query = SqlQuery.ForSqlite<QueryUser>()
                .Where(u => u.IsActive)
                .Where(u => u.Age > 18)
                .Where(u => u.Email != null)
                .OrderBy(u => u.Name)
                .ThenBy(u => u.Age)
                .Skip(10)
                .Take(20);

            var sql = query.ToSql();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Memory increase should be minimal (less than 10MB)
        Assert.IsTrue(memoryIncrease < 10 * 1024 * 1024, $"Memory increased by {memoryIncrease / 1024 / 1024}MB");
    }

    [TestMethod]
    public void Memory_LargeResultSet_EfficientMemoryUsage()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = i % 2 == 0,
                CreatedAt = DateTime.Now
            })
            .ToArray();

        var initialMemory = GC.GetTotalMemory(true);

        using (var dbReader = new TestDbDataReader(entities))
        {
            var results = reader.ToList(dbReader);
            Assert.AreEqual(100000, results.Count);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Memory increase should be reasonable for 100k entities
        Assert.IsTrue(memoryIncrease < 100 * 1024 * 1024, $"Memory increased by {memoryIncrease / 1024 / 1024}MB");
    }

    #endregion

    #region Stress Tests

    [TestMethod]
    public void Stress_ContinuousQueryGeneration_Stable()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var queryCount = 0;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var query = SqlQuery.ForSqlite<QueryUser>()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .Take(10);

                var sql = query.ToSql();
                Assert.IsTrue(sql.Contains("SELECT"));
                queryCount++;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Should generate thousands of queries in 5 seconds
        Assert.IsTrue(queryCount > 1000, $"Only generated {queryCount} queries");
    }

    // Note: Database integration tests removed as they require WithReader() API

    #endregion

    #region Resource Cleanup Tests

    [TestMethod]
    public void Cleanup_MultipleConnections_ProperDisposal()
    {
        var connections = new List<SqliteConnection>();

        try
        {
            for (int i = 0; i < 100; i++)
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                connections.Add(connection);

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE test (id INTEGER)";
                    cmd.ExecuteNonQuery();
                }
            }

            Assert.AreEqual(100, connections.Count);
        }
        finally
        {
            foreach (var conn in connections)
            {
                conn.Close();
                conn.Dispose();
            }
        }

        // If we get here without exceptions, cleanup was successful
        Assert.IsTrue(true);
    }

    #endregion
}
