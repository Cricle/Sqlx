using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Boundary;

/// <summary>
/// Phase 3: 大数据和性能测试 - 确保80%边界场景覆盖
/// 新增10个大数据和性能测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Boundary")]
[TestCategory("CoveragePhase3")]
public class TDD_LargeDataPerf_Phase3
{
    private IDbConnection? _connection;
    private ILargeDataPerfRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE large_data (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                value REAL NOT NULL,
                category TEXT NOT NULL
            )");

        _repo = new LargeDataPerfRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("Query with pagination on large dataset should work")]
    public async Task Query_PaginationLarge_ShouldWork()
    {
        // Insert 5000 records
        for (int i = 1; i <= 5000; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 10}')");
        }

        var page1 = await _repo!.GetPageAsync(100, 0);
        var page2 = await _repo.GetPageAsync(100, 100);
        var lastPage = await _repo.GetPageAsync(100, 4900);

        Assert.AreEqual(100, page1.Count);
        Assert.AreEqual(100, page2.Count);
        Assert.AreEqual(100, lastPage.Count);
        Assert.AreNotEqual(page1[0].Id, page2[0].Id);
    }

    [TestMethod]
    [Description("Query with large OFFSET should work")]
    public async Task Query_LargeOffset_ShouldWork()
    {
        // Insert 2000 records
        for (int i = 1; i <= 2000; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 5}')");
        }

        var results = await _repo!.GetPageAsync(10, 1990);
        Assert.AreEqual(10, results.Count);
        Assert.IsTrue(results[0].Id >= 1991);
    }

    [TestMethod]
    [Description("Aggregate on large dataset should work")]
    public async Task Aggregate_LargeDataset_ShouldWork()
    {
        // Insert 3000 records
        for (int i = 1; i <= 3000; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 10}')");
        }

        var count = await _repo!.GetCountAsync();
        var sum = await _repo.GetSumAsync();
        var avg = await _repo.GetAvgAsync();

        Assert.AreEqual(3000, count);
        Assert.AreEqual(4501500.0, sum, 1.0); // Sum of 1 to 3000
        Assert.AreEqual(1500.5, avg, 1.0);
    }

    [TestMethod]
    [Description("Query with complex WHERE on large dataset should work")]
    public async Task Query_ComplexWhereLarge_ShouldWork()
    {
        // Insert 2000 records
        for (int i = 1; i <= 2000; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 10}')");
        }

        var results = await _repo!.GetComplexFilterAsync(500, 1500, "Cat5");
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(r => r.Value >= 500 && r.Value <= 1500 && r.Category == "Cat5"));
    }

    [TestMethod]
    [Description("JOIN on large datasets should work")]
    public async Task Join_LargeDatasets_ShouldWork()
    {
        _connection!.Execute(@"CREATE TABLE categories (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");

        // Insert data
        for (int i = 0; i < 10; i++)
        {
            _connection.Execute($"INSERT INTO categories VALUES ({i}, 'Category{i}')");
        }

        for (int i = 1; i <= 1000; i++)
        {
            _connection.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 10}')");
        }

        var results = await _repo!.GetJoinedDataAsync();
        Assert.IsTrue(results.Count >= 100); // Should have joined results
    }

    [TestMethod]
    [Description("GROUP BY on large dataset should work")]
    public async Task GroupBy_LargeDataset_ShouldWork()
    {
        // Insert 5000 records across 20 categories
        for (int i = 1; i <= 5000; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 20}')");
        }

        var results = await _repo!.GetGroupedAsync();
        Assert.AreEqual(20, results.Count);
        Assert.IsTrue(results.All(r => r.Count == 250)); // 5000 / 20 = 250
    }

    [TestMethod]
    [Description("ORDER BY on large dataset should work")]
    public async Task OrderBy_LargeDataset_ShouldWork()
    {
        // Insert 1000 records in random order
        var random = new Random(42);
        var ids = Enumerable.Range(1, 1000).OrderBy(_ => random.Next()).ToList();
        foreach (var id in ids)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({id}, 'Item{id}', {id}, 'Cat{id % 10}')");
        }

        var results = await _repo!.GetOrderedAsync();
        Assert.AreEqual(1000, results.Count);

        // Verify ordering
        for (int i = 0; i < results.Count - 1; i++)
        {
            Assert.IsTrue(results[i].Value <= results[i + 1].Value,
                $"Not ordered at index {i}: {results[i].Value} > {results[i + 1].Value}");
        }
    }

    [TestMethod]
    [Description("Multiple queries in sequence should work")]
    public async Task MultipleQueries_Sequential_ShouldWork()
    {
        // Insert test data
        for (int i = 1; i <= 500; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 5}')");
        }

        // Execute multiple queries
        var result1 = await _repo!.GetByIdAsync(1);
        var result2 = await _repo.GetByCategoryAsync("Cat1");
        var result3 = await _repo.GetPageAsync(10, 0);
        var result4 = await _repo.GetCountAsync();

        Assert.IsNotNull(result1);
        Assert.IsTrue(result2.Count > 0);
        Assert.AreEqual(10, result3.Count);
        Assert.AreEqual(500, result4);
    }

    [TestMethod]
    [Description("Connection reuse should work correctly")]
    public async Task ConnectionReuse_ShouldWork()
    {
        // Insert test data
        for (int i = 1; i <= 100; i++)
        {
            _connection!.Execute($"INSERT INTO large_data VALUES ({i}, 'Item{i}', {i}, 'Cat{i % 5}')");
        }

        // Reuse connection for multiple operations
        for (int i = 0; i < 10; i++)
        {
            var results = await _repo!.GetPageAsync(10, i * 10);
            Assert.AreEqual(10, results.Count);
        }

        // Verify final state
        var all = await _repo!.GetAllAsync();
        Assert.AreEqual(100, all.Count);
    }
}

// Repository interface
public interface ILargeDataPerfRepository
{
    [SqlTemplate("SELECT * FROM large_data")]
    Task<List<LargeData>> GetAllAsync();

    [SqlTemplate("SELECT * FROM large_data WHERE id = @id")]
    Task<LargeData?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM large_data LIMIT @limit OFFSET @offset")]
    Task<List<LargeData>> GetPageAsync(int limit, int offset);

    [SqlTemplate("SELECT COUNT(*) FROM large_data")]
    Task<int> GetCountAsync();

    [SqlTemplate("SELECT SUM(value) FROM large_data")]
    Task<double> GetSumAsync();

    [SqlTemplate("SELECT AVG(value) FROM large_data")]
    Task<double> GetAvgAsync();

    [SqlTemplate("SELECT * FROM large_data WHERE value >= @min AND value <= @max AND category = @category")]
    Task<List<LargeData>> GetComplexFilterAsync(double min, double max, string category);

    [SqlTemplate("SELECT ld.* FROM large_data ld INNER JOIN categories c ON ld.category = 'Cat' || c.id LIMIT 100")]
    Task<List<LargeData>> GetJoinedDataAsync();

    [SqlTemplate("SELECT category, COUNT(*) as count FROM large_data GROUP BY category")]
    Task<List<CategoryCount>> GetGroupedAsync();

    [SqlTemplate("SELECT * FROM large_data ORDER BY value ASC")]
    Task<List<LargeData>> GetOrderedAsync();

    [SqlTemplate("SELECT * FROM large_data WHERE category = @category")]
    Task<List<LargeData>> GetByCategoryAsync(string category);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ILargeDataPerfRepository))]
public partial class LargeDataPerfRepository(IDbConnection connection) : ILargeDataPerfRepository { }

// Models
public class LargeData
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public string Category { get; set; } = "";
}

public class CategoryCount
{
    public string Category { get; set; } = "";
    public int Count { get; set; }
}

