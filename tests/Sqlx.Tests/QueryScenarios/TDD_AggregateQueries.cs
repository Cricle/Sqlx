using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.QueryScenarios;

/// <summary>
/// TDD: 聚合查询测试
/// 验证COUNT, SUM, AVG, MIN, MAX等聚合函数
/// </summary>
[TestClass]
public class TDD_AggregateQueries
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Core")]
    public void Aggregate_Count_ShouldReturnTotalRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        for (int i = 1; i <= 15; i++)
        {
            connection.Execute($"INSERT INTO products (name, price) VALUES ('Product{i}', {i * 10})");
        }
        
        var repo = new AggregateTestRepository(connection);
        
        // Act
        var count = repo.GetProductCountAsync().Result;
        
        // Assert
        Assert.AreEqual(15, count);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Core")]
    public void Aggregate_CountWithCondition_ShouldReturnFilteredCount()
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
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product4', 350)");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act: Count products with price > 100
        var count = repo.GetExpensiveProductCountAsync(100).Result;
        
        // Assert
        Assert.AreEqual(3, count);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Core")]
    public void Aggregate_Sum_ShouldReturnTotalValue()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                amount INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO orders (amount) VALUES (100)");
        connection.Execute("INSERT INTO orders (amount) VALUES (200)");
        connection.Execute("INSERT INTO orders (amount) VALUES (300)");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act
        var totalAmount = repo.GetTotalOrderAmountAsync().Result;
        
        // Assert
        Assert.AreEqual(600, totalAmount);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Core")]
    public void Aggregate_Average_ShouldReturnCorrectValue()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product1', 100)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product2', 200)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product3', 300)");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act
        var avgPrice = repo.GetAveragePriceAsync().Result;
        
        // Assert
        Assert.AreEqual(200.0, avgPrice, 0.01);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Core")]
    public void Aggregate_Min_ShouldReturnMinimumValue()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product1', 250)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product2', 100)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product3', 500)");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act
        var minPrice = repo.GetMinPriceAsync().Result;
        
        // Assert
        Assert.AreEqual(100, minPrice);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Core")]
    public void Aggregate_Max_ShouldReturnMaximumValue()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product1', 250)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product2', 100)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product3', 500)");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act
        var maxPrice = repo.GetMaxPriceAsync().Result;
        
        // Assert
        Assert.AreEqual(500, maxPrice);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("EmptyTable")]
    public void Aggregate_CountOnEmptyTable_ShouldReturnZero()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act
        var count = repo.GetProductCountAsync().Result;
        
        // Assert
        Assert.AreEqual(0, count);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Aggregate")]
    [TestCategory("Nullable")]
    public void Aggregate_SumWithNullable_ShouldHandleCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                amount INTEGER
            )");
        
        connection.Execute("INSERT INTO orders (amount) VALUES (100)");
        connection.Execute("INSERT INTO orders (amount) VALUES (NULL)");
        connection.Execute("INSERT INTO orders (amount) VALUES (200)");
        
        var repo = new AggregateTestRepository(connection);
        
        // Act: SQLite SUM ignores NULL values
        var totalAmount = repo.GetTotalOrderAmountAsync().Result;
        
        // Assert
        Assert.AreEqual(300, totalAmount);
        
        connection.Dispose();
    }
}

// Test models (reuse some from previous tests if needed)

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAggregateTestRepository))]
public partial class AggregateTestRepository(IDbConnection connection) : IAggregateTestRepository { }

public interface IAggregateTestRepository
{
    [SqlTemplate("SELECT COUNT(*) FROM products")]
    Task<int> GetProductCountAsync();
    
    [SqlTemplate("SELECT COUNT(*) FROM products WHERE price > @minPrice")]
    Task<int> GetExpensiveProductCountAsync(int minPrice);
    
    [SqlTemplate("SELECT SUM(amount) FROM orders")]
    Task<int> GetTotalOrderAmountAsync();
    
    [SqlTemplate("SELECT AVG(price) FROM products")]
    Task<double> GetAveragePriceAsync();
    
    [SqlTemplate("SELECT MIN(price) FROM products")]
    Task<int> GetMinPriceAsync();
    
    [SqlTemplate("SELECT MAX(price) FROM products")]
    Task<int> GetMaxPriceAsync();
}


