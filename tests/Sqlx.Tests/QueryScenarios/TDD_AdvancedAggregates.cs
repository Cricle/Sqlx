using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.QueryScenarios;

/// <summary>
/// TDD: 高级聚合查询测试
/// Phase 2.3: 复杂GROUP BY、多聚合函数组合
/// </summary>
[TestClass]
public class TDD_AdvancedAggregates
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Phase2")]
    public void Aggregate_GroupByMultipleColumns_ShouldWork()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE sales (
                id INTEGER PRIMARY KEY,
                region TEXT,
                category TEXT,
                amount REAL
            )");

        connection.Execute(@"
            INSERT INTO sales VALUES
            (1, 'North', 'Electronics', 100),
            (2, 'North', 'Electronics', 150),
            (3, 'North', 'Books', 50),
            (4, 'South', 'Electronics', 200)");

        var repo = new AdvancedAggregateRepository(connection);

        // Act
        var results = repo.GetSalesByRegionAndCategoryAsync().Result;

        // Assert
        Assert.AreEqual(3, results.Count); // North-Electronics, North-Books, South-Electronics

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Phase2")]
    public void Aggregate_CountAndSumTogether_ShouldWork()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                customer_id INTEGER,
                amount REAL
            )");

        connection.Execute("INSERT INTO orders VALUES (1, 1, 100), (2, 1, 200), (3, 2, 50)");

        var repo = new AdvancedAggregateRepository(connection);

        // Act
        var results = repo.GetCustomerStatsAsync().Result;

        // Assert
        Assert.AreEqual(2, results.Count);
        var customer1 = results.First(r => r.CustomerId == 1);
        Assert.AreEqual(2, customer1.OrderCount);
        Assert.AreEqual(300, customer1.TotalAmount, 0.01);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Phase2")]
    public void Aggregate_DistinctCount_ShouldCountUnique()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE visits (id INTEGER PRIMARY KEY, user_id INTEGER)");
        connection.Execute("INSERT INTO visits VALUES (1, 1), (2, 1), (3, 2), (4, 3), (5, 2)");

        var repo = new AdvancedAggregateRepository(connection);

        // Act
        var count = repo.GetUniqueUserCountAsync().Result;

        // Assert
        Assert.AreEqual(3, count); // 3 unique users

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Phase2")]
    public void Aggregate_GroupByWithOrderBy_ShouldSortGroups()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, category TEXT, price REAL)");
        connection.Execute("INSERT INTO products VALUES (1, 'A', 100), (2, 'B', 200), (3, 'A', 150), (4, 'C', 50)");

        var repo = new AdvancedAggregateRepository(connection);

        // Act
        var results = repo.GetCategoriesByTotalPriceAsync().Result;

        // Assert: 应该按总价降序排序
        Assert.AreEqual("A", results[0].Category); // Total: 250
        Assert.AreEqual("B", results[1].Category); // Total: 200
        Assert.AreEqual("C", results[2].Category); // Total: 50

        connection.Dispose();
    }
}

// Models
public class SalesSummary
{
    public string Region { get; set; } = "";
    public string Category { get; set; } = "";
    public double TotalAmount { get; set; }
}

public class CustomerStats
{
    public int CustomerId { get; set; }
    public int OrderCount { get; set; }
    public double TotalAmount { get; set; }
}

public class CategorySummary
{
    public string Category { get; set; } = "";
    public double TotalPrice { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAdvancedAggregateRepository))]
public partial class AdvancedAggregateRepository(IDbConnection connection) : IAdvancedAggregateRepository { }

public interface IAdvancedAggregateRepository
{
    [SqlTemplate("SELECT region, category, SUM(amount) as total_amount FROM sales GROUP BY region, category")]
    Task<List<SalesSummary>> GetSalesByRegionAndCategoryAsync();

    [SqlTemplate("SELECT customer_id, COUNT(*) as order_count, SUM(amount) as total_amount FROM orders GROUP BY customer_id")]
    Task<List<CustomerStats>> GetCustomerStatsAsync();

    [SqlTemplate("SELECT COUNT(DISTINCT user_id) as count FROM visits")]
    Task<int> GetUniqueUserCountAsync();

    [SqlTemplate("SELECT category, SUM(price) as total_price FROM products GROUP BY category ORDER BY total_price DESC")]
    Task<List<CategorySummary>> GetCategoriesByTotalPriceAsync();
}


