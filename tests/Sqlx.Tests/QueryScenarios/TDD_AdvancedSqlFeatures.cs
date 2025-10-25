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
/// TDD: SQL高级功能测试
/// 验证DISTINCT, GROUP BY, HAVING, IN, LIKE等功能
/// </summary>
[TestClass]
public class TDD_AdvancedSqlFeatures
{
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("Core")]
    public void Sql_Distinct_ShouldRemoveDuplicates()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                city TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO orders (city) VALUES ('New York')");
        connection.Execute("INSERT INTO orders (city) VALUES ('London')");
        connection.Execute("INSERT INTO orders (city) VALUES ('New York')");
        connection.Execute("INSERT INTO orders (city) VALUES ('Paris')");
        connection.Execute("INSERT INTO orders (city) VALUES ('London')");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act
        var cities = repo.GetDistinctCitiesAsync().Result;
        
        // Assert: Should get 3 unique cities
        Assert.AreEqual(3, cities.Count);
        Assert.IsTrue(cities.Any(c => c.Name == "New York"));
        Assert.IsTrue(cities.Any(c => c.Name == "London"));
        Assert.IsTrue(cities.Any(c => c.Name == "Paris"));
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("Core")]
    public void Sql_GroupBy_ShouldGroupRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                city TEXT NOT NULL,
                amount INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('New York', 100)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('New York', 200)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('London', 150)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('London', 250)");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act
        var results = repo.GetOrderSummaryByCityAsync().Result;
        
        // Assert
        Assert.AreEqual(2, results.Count);
        
        var ny = results.First(r => r.City == "New York");
        Assert.AreEqual(2, ny.OrderCount);
        Assert.AreEqual(300, ny.TotalAmount);
        
        var ld = results.First(r => r.City == "London");
        Assert.AreEqual(2, ld.OrderCount);
        Assert.AreEqual(400, ld.TotalAmount);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("Having")]
    public void Sql_GroupByHaving_ShouldFilterGroups()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                city TEXT NOT NULL,
                amount INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('New York', 100)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('New York', 200)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('London', 50)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('Paris', 300)");
        connection.Execute("INSERT INTO orders (city, amount) VALUES ('Paris', 400)");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act: Get cities with total amount > 250
        var results = repo.GetHighValueCitiesAsync(250).Result;
        
        // Assert: Should get New York (300) and Paris (700)
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.City == "New York"));
        Assert.IsTrue(results.Any(r => r.City == "Paris"));
        Assert.IsFalse(results.Any(r => r.City == "London"));
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("In")]
    public void Sql_InClause_ShouldFilterByList()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                category TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, category) VALUES ('Widget', 'Tools')");
        connection.Execute("INSERT INTO products (name, category) VALUES ('Gadget', 'Electronics')");
        connection.Execute("INSERT INTO products (name, category) VALUES ('Hammer', 'Tools')");
        connection.Execute("INSERT INTO products (name, category) VALUES ('Phone', 'Electronics')");
        connection.Execute("INSERT INTO products (name, category) VALUES ('Book', 'Media')");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act: Get products in Tools and Electronics categories
        var results = repo.GetProductsInCategoriesAsync().Result;
        
        // Assert: Should get 4 products (excluding Book)
        Assert.AreEqual(4, results.Count);
        Assert.IsFalse(results.Any(r => r.Name == "Book"));
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("Like")]
    public void Sql_LikeClause_ShouldMatchPattern()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name) VALUES ('SuperWidget')");
        connection.Execute("INSERT INTO products (name) VALUES ('MegaWidget')");
        connection.Execute("INSERT INTO products (name) VALUES ('Gadget')");
        connection.Execute("INSERT INTO products (name) VALUES ('Widget Pro')");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act: Get products with name containing 'Widget'
        var results = repo.GetProductsContainingAsync("Widget").Result;
        
        // Assert: Should get 3 products
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.Any(r => r.Name == "SuperWidget"));
        Assert.IsTrue(results.Any(r => r.Name == "MegaWidget"));
        Assert.IsTrue(results.Any(r => r.Name == "Widget Pro"));
        Assert.IsFalse(results.Any(r => r.Name == "Gadget"));
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("Between")]
    public void Sql_BetweenClause_ShouldFilterRange()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product1', 50)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product2', 100)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product3', 150)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product4', 200)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product5', 250)");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act: Get products with price between 100 and 200
        var results = repo.GetProductsInPriceRangeAsync(100, 200).Result;
        
        // Assert: Should get 3 products
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.All(r => r.Price >= 100 && r.Price <= 200));
        
        connection.Dispose();
    }
    
    [TestMethod]
    [Ignore("TODO: Advanced SQL features require additional testing/investigation")]
    [TestCategory("TDD-Red")]
    [TestCategory("SQL")]
    [TestCategory("Case")]
    public void Sql_CaseWhen_ShouldWorkCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product1', 50)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product2', 150)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product3', 250)");
        
        var repo = new AdvancedSqlTestRepository(connection);
        
        // Act: Get products with price category
        var results = repo.GetProductsWithPriceCategoryAsync().Result;
        
        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Low", results.First(r => r.Price == 50).PriceCategory);
        Assert.AreEqual("Medium", results.First(r => r.Price == 150).PriceCategory);
        Assert.AreEqual("High", results.First(r => r.Price == 250).PriceCategory);
        
        connection.Dispose();
    }
}

// Test models
public class AdvancedSqlCity
{
    public string Name { get; set; } = "";
}

public class AdvancedSqlOrderSummary
{
    public string City { get; set; } = "";
    public int OrderCount { get; set; }
    public int TotalAmount { get; set; }
}

public class AdvancedSqlProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public int Price { get; set; }
    public string PriceCategory { get; set; } = "";
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAdvancedSqlTestRepository))]
public partial class AdvancedSqlTestRepository(IDbConnection connection) : IAdvancedSqlTestRepository { }

public interface IAdvancedSqlTestRepository
{
    [SqlTemplate("SELECT DISTINCT city as Name FROM orders")]
    Task<List<AdvancedSqlCity>> GetDistinctCitiesAsync();
    
    [SqlTemplate(@"
        SELECT city as City, COUNT(*) as OrderCount, SUM(amount) as TotalAmount
        FROM orders
        GROUP BY city
    ")]
    Task<List<AdvancedSqlOrderSummary>> GetOrderSummaryByCityAsync();
    
    [SqlTemplate(@"
        SELECT city as City, SUM(amount) as TotalAmount
        FROM orders
        GROUP BY city
        HAVING SUM(amount) > @minAmount
    ")]
    Task<List<AdvancedSqlOrderSummary>> GetHighValueCitiesAsync(int minAmount);
    
    [SqlTemplate("SELECT id, name, category FROM products WHERE category IN ('Tools', 'Electronics')")]
    Task<List<AdvancedSqlProduct>> GetProductsInCategoriesAsync();
    
    [SqlTemplate("SELECT id, name, 0 as price, '' as PriceCategory FROM products WHERE name LIKE '%' || @pattern || '%'")]
    Task<List<AdvancedSqlProduct>> GetProductsContainingAsync(string pattern);
    
    [SqlTemplate("SELECT id, name, price, '' as PriceCategory FROM products WHERE price BETWEEN @minPrice AND @maxPrice")]
    Task<List<AdvancedSqlProduct>> GetProductsInPriceRangeAsync(int minPrice, int maxPrice);
    
    [SqlTemplate(@"
        SELECT id, name, price,
               CASE 
                   WHEN price < 100 THEN 'Low'
                   WHEN price < 200 THEN 'Medium'
                   ELSE 'High'
               END as PriceCategory
        FROM products
    ")]
    Task<List<AdvancedSqlProduct>> GetProductsWithPriceCategoryAsync();
}


