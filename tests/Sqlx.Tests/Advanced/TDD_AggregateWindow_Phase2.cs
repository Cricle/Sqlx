using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Advanced;

/// <summary>
/// Phase 2: 聚合和窗口函数增强测试 - 确保90%复杂场景覆盖
/// 新增10个聚合和窗口函数测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Advanced")]
[TestCategory("CoveragePhase2")]
public class TDD_AggregateWindow_Phase2
{
    private IDbConnection? _connection;
    private IAggregateWindowRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE sales (
                id INTEGER PRIMARY KEY,
                product TEXT NOT NULL,
                category TEXT NOT NULL,
                region TEXT NOT NULL,
                amount REAL NOT NULL,
                sale_date TEXT NOT NULL
            )");

        // Insert test data
        _connection.Execute("INSERT INTO sales VALUES (1, 'Product A', 'Electronics', 'North', 1000, '2025-01-01')");
        _connection.Execute("INSERT INTO sales VALUES (2, 'Product B', 'Electronics', 'North', 1500, '2025-01-02')");
        _connection.Execute("INSERT INTO sales VALUES (3, 'Product C', 'Books', 'North', 500, '2025-01-03')");
        _connection.Execute("INSERT INTO sales VALUES (4, 'Product A', 'Electronics', 'South', 800, '2025-01-04')");
        _connection.Execute("INSERT INTO sales VALUES (5, 'Product B', 'Electronics', 'South', 1200, '2025-01-05')");
        _connection.Execute("INSERT INTO sales VALUES (6, 'Product D', 'Books', 'South', 600, '2025-01-06')");
        _connection.Execute("INSERT INTO sales VALUES (7, 'Product E', 'Toys', 'North', 300, '2025-01-07')");
        _connection.Execute("INSERT INTO sales VALUES (8, 'Product F', 'Toys', 'South', 400, '2025-01-08')");

        _repo = new AggregateWindowRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("COUNT(*) aggregate should work")]
    public async Task Aggregate_CountAll_ShouldWork()
    {
        var result = await _repo!.GetTotalSalesCountAsync();
        Assert.AreEqual(8, result);
    }

    [TestMethod]
    [Description("COUNT(DISTINCT) should count unique values")]
    public async Task Aggregate_CountDistinct_ShouldWork()
    {
        var result = await _repo!.GetUniqueCategoryCountAsync();
        Assert.AreEqual(3, result); // Electronics, Books, Toys
    }

    [TestMethod]
    [Description("SUM aggregate should calculate total")]
    public async Task Aggregate_Sum_ShouldWork()
    {
        var result = await _repo!.GetTotalRevenueAsync();
        Assert.AreEqual(6300.0, result, 0.01);
    }

    [TestMethod]
    [Description("AVG aggregate should calculate average")]
    public async Task Aggregate_Avg_ShouldWork()
    {
        var result = await _repo!.GetAverageSaleAmountAsync();
        Assert.AreEqual(787.5, result, 0.01); // 6300 / 8
    }

    [TestMethod]
    [Description("MIN/MAX aggregates should find extremes")]
    public async Task Aggregate_MinMax_ShouldWork()
    {
        var min = await _repo!.GetMinSaleAmountAsync();
        var max = await _repo!.GetMaxSaleAmountAsync();
        Assert.AreEqual(300.0, min, 0.01);
        Assert.AreEqual(1500.0, max, 0.01);
    }

    [TestMethod]
    [Description("GROUP BY with multiple aggregates should work")]
    public async Task Aggregate_GroupByMultiple_ShouldWork()
    {
        var results = await _repo!.GetCategorySummaryAsync();
        Assert.AreEqual(3, results.Count);

        var electronics = results.FirstOrDefault(r => r.Category == "Electronics");
        Assert.IsNotNull(electronics);
        Assert.AreEqual(4, electronics.SaleCount);
        Assert.AreEqual(4500.0, electronics.TotalRevenue, 0.01);
        Assert.AreEqual(1125.0, electronics.AvgSale, 0.01);
    }

    [TestMethod]
    [Description("HAVING clause should filter groups")]
    public async Task Aggregate_Having_ShouldFilter()
    {
        var results = await _repo!.GetHighRevenueCategoriesAsync(2000.0);
        Assert.IsTrue(results.Count >= 1);
        Assert.IsTrue(results.All(r => r.TotalRevenue >= 2000.0));
    }

    [TestMethod]
    [Description("ROW_NUMBER window function should assign sequential numbers")]
    public async Task Window_RowNumber_ShouldWork()
    {
        var results = await _repo!.GetSalesWithRowNumberAsync();
        Assert.AreEqual(8, results.Count);
        // Row numbers should be 1-8
        var rowNumbers = results.Select(r => r.RowNum).OrderBy(n => n).ToList();
        for (int i = 0; i < 8; i++)
        {
            Assert.AreEqual(i + 1, rowNumbers[i]);
        }
    }

    [TestMethod]
    [Description("RANK window function should handle ties")]
    public async Task Window_Rank_ShouldWork()
    {
        var results = await _repo!.GetSalesRankedByAmountAsync();
        Assert.AreEqual(8, results.Count);
        // Highest amount should have rank 1
        var top = results.OrderBy(r => r.Rank).First();
        Assert.AreEqual(1500.0, top.Amount, 0.01);
        Assert.AreEqual(1, top.Rank);
    }

    [TestMethod]
    [Description("Partition by window function should work")]
    public async Task Window_PartitionBy_ShouldWork()
    {
        var results = await _repo!.GetSalesRankedByCategoryAsync();
        Assert.AreEqual(8, results.Count);

        // Each category should have its own ranking
        var electronicsRanks = results.Where(r => r.Category == "Electronics")
            .Select(r => r.Rank).OrderBy(r => r).ToList();
        Assert.IsTrue(electronicsRanks.Contains(1));
    }
}

// Repository interface
public interface IAggregateWindowRepository
{
    [SqlTemplate("SELECT COUNT(*) FROM sales")]
    Task<int> GetTotalSalesCountAsync();

    [SqlTemplate("SELECT COUNT(DISTINCT category) FROM sales")]
    Task<int> GetUniqueCategoryCountAsync();

    [SqlTemplate("SELECT SUM(amount) FROM sales")]
    Task<double> GetTotalRevenueAsync();

    [SqlTemplate("SELECT AVG(amount) FROM sales")]
    Task<double> GetAverageSaleAmountAsync();

    [SqlTemplate("SELECT MIN(amount) FROM sales")]
    Task<double> GetMinSaleAmountAsync();

    [SqlTemplate("SELECT MAX(amount) FROM sales")]
    Task<double> GetMaxSaleAmountAsync();

    [SqlTemplate("SELECT category, COUNT(*) as sale_count, SUM(amount) as total_revenue, AVG(amount) as avg_sale FROM sales GROUP BY category")]
    Task<List<CategorySummary>> GetCategorySummaryAsync();

    [SqlTemplate("SELECT category, SUM(amount) as total_revenue FROM sales GROUP BY category HAVING SUM(amount) >= @minRevenue")]
    Task<List<CategoryRevenue>> GetHighRevenueCategoriesAsync(double minRevenue);

    [SqlTemplate("SELECT product, category, amount, ROW_NUMBER() OVER (ORDER BY amount DESC) as row_num FROM sales")]
    Task<List<SaleWithRowNumber>> GetSalesWithRowNumberAsync();

    [SqlTemplate("SELECT product, category, amount, RANK() OVER (ORDER BY amount DESC) as rank FROM sales")]
    Task<List<SaleWithRank>> GetSalesRankedByAmountAsync();

    [SqlTemplate("SELECT product, category, amount, RANK() OVER (PARTITION BY category ORDER BY amount DESC) as rank FROM sales")]
    Task<List<SaleWithRank>> GetSalesRankedByCategoryAsync();
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAggregateWindowRepository))]
public partial class AggregateWindowRepository(IDbConnection connection) : IAggregateWindowRepository { }

// Models
public class CategorySummary
{
    public string Category { get; set; } = "";
    public int SaleCount { get; set; }
    public double TotalRevenue { get; set; }
    public double AvgSale { get; set; }
}

public class CategoryRevenue
{
    public string Category { get; set; } = "";
    public double TotalRevenue { get; set; }
}

public class SaleWithRowNumber
{
    public string Product { get; set; } = "";
    public string Category { get; set; } = "";
    public double Amount { get; set; }
    public int RowNum { get; set; }
}

public class SaleWithRank
{
    public string Product { get; set; } = "";
    public string Category { get; set; } = "";
    public double Amount { get; set; }
    public int Rank { get; set; }
}

