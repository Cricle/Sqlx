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
/// TDD: 分页和排序测试
/// 验证LIMIT/OFFSET、ORDER BY等常见业务查询场景
/// </summary>
[TestClass]
public class TDD_PaginationAndOrdering
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Core")]
    public void Pagination_FirstPage_ShouldReturnCorrectRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        for (int i = 1; i <= 20; i++)
        {
            connection.Execute($"INSERT INTO products (name, price) VALUES ('Product{i}', {i * 10})");
        }
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Get first page (10 items)
        var products = repo.GetProductsPageAsync(10, 0).Result;
        
        // Assert
        Assert.AreEqual(10, products.Count);
        Assert.AreEqual("Product1", products[0].Name);
        Assert.AreEqual("Product10", products[9].Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Core")]
    public void Pagination_SecondPage_ShouldReturnCorrectRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        for (int i = 1; i <= 20; i++)
        {
            connection.Execute($"INSERT INTO products (name, price) VALUES ('Product{i}', {i * 10})");
        }
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Get second page (items 11-20)
        var products = repo.GetProductsPageAsync(10, 10).Result;
        
        // Assert
        Assert.AreEqual(10, products.Count);
        Assert.AreEqual("Product11", products[0].Name);
        Assert.AreEqual("Product20", products[9].Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Ordering")]
    [TestCategory("Core")]
    public void Ordering_Ascending_ShouldReturnOrderedRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (name, price) VALUES ('C Product', 300)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('A Product', 100)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('B Product', 200)");
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Get products ordered by name ascending
        var products = repo.GetProductsOrderedByNameAsync().Result;
        
        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual("A Product", products[0].Name);
        Assert.AreEqual("B Product", products[1].Name);
        Assert.AreEqual("C Product", products[2].Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Ordering")]
    [TestCategory("Core")]
    public void Ordering_Descending_ShouldReturnOrderedRecords()
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
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product2', 300)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Product3', 200)");
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Get products ordered by price descending
        var products = repo.GetProductsOrderedByPriceDescAsync().Result;
        
        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual(300, products[0].Price);
        Assert.AreEqual(200, products[1].Price);
        Assert.AreEqual(100, products[2].Price);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Ordering")]
    public void PaginationWithOrdering_ShouldWorkTogether()
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
            connection.Execute($"INSERT INTO products (name, price) VALUES ('Product{i}', {(16 - i) * 10})");
        }
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Get second page of products ordered by price
        var products = repo.GetProductsPageOrderedByPriceAsync(5, 5).Result;
        
        // Assert: Prices are (16-i)*10, so sorted: 10,20,30,40,50,60,70,80,90,100,110,120,130,140,150
        // Second page (offset 5, limit 5) should be: 60,70,80,90,100
        Assert.AreEqual(5, products.Count);
        Assert.AreEqual(60, products[0].Price);
        Assert.AreEqual(100, products[4].Price);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Edge")]
    public void Pagination_LastPagePartial_ShouldReturnRemainingRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        // Insert 23 records (last page will have only 3 records)
        for (int i = 1; i <= 23; i++)
        {
            connection.Execute($"INSERT INTO products (name, price) VALUES ('Product{i}', {i * 10})");
        }
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Get third page (offset 20, limit 10)
        var products = repo.GetProductsPageAsync(10, 20).Result;
        
        // Assert: Should only get 3 records
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual("Product21", products[0].Name);
        Assert.AreEqual("Product23", products[2].Name);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Edge")]
    public void Pagination_EmptyPage_ShouldReturnEmptyList()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");
        
        // Insert only 10 records
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO products (name, price) VALUES ('Product{i}', {i * 10})");
        }
        
        var repo = new PaginationTestRepository(connection);
        
        // Act: Try to get page beyond available data
        var products = repo.GetProductsPageAsync(10, 20).Result;
        
        // Assert: Should return empty list
        Assert.IsNotNull(products);
        Assert.AreEqual(0, products.Count);
        
        connection.Dispose();
    }
}

// Test models
public class PaginationProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Price { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPaginationTestRepository))]
public partial class PaginationTestRepository(IDbConnection connection) : IPaginationTestRepository { }

public interface IPaginationTestRepository
{
    [SqlTemplate("SELECT * FROM products LIMIT @limit OFFSET @offset")]
    Task<List<PaginationProduct>> GetProductsPageAsync(int limit, int offset);
    
    [SqlTemplate("SELECT * FROM products ORDER BY name ASC")]
    Task<List<PaginationProduct>> GetProductsOrderedByNameAsync();
    
    [SqlTemplate("SELECT * FROM products ORDER BY price DESC")]
    Task<List<PaginationProduct>> GetProductsOrderedByPriceDescAsync();
    
    [SqlTemplate("SELECT * FROM products ORDER BY price ASC LIMIT @limit OFFSET @offset")]
    Task<List<PaginationProduct>> GetProductsPageOrderedByPriceAsync(int limit, int offset);
}

// Helper extension
public static class PaginationConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

