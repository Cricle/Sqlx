using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CRUD;

/// <summary>
/// TDD: 基础SELECT操作的完整测试覆盖 - 最常用场景
/// Phase 1.1: 15个基础SELECT测试
/// </summary>
[TestClass]
public class TDD_Select_Basic
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_ByPrimaryKey_ShouldReturnSingleRecord()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(
            "INSERT INTO products (name, price, stock, category) VALUES ('Laptop', 999.99, 10, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act
        var product = repo.GetByIdAsync(1).Result;

        // Assert
        Assert.IsNotNull(product);
        Assert.AreEqual(1, product.Id);
        Assert.AreEqual("Laptop", product.Name);
        Assert.AreEqual(999.99, product.Price, 0.01);
        Assert.AreEqual(10, product.Stock);
        Assert.AreEqual("Electronics", product.Category);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_ByPrimaryKey_NotFound_ShouldReturnNull()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        var repo = new BasicSelectProductRepository(connection);

        // Act
        var product = repo.GetByIdAsync(999).Result;

        // Assert
        Assert.IsNull(product);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_All_ShouldReturnAllRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Laptop', 999.99, 10, 'Electronics'),
            ('Mouse', 29.99, 50, 'Electronics'),
            ('Desk', 299.99, 5, 'Furniture')");

        var repo = new BasicSelectProductRepository(connection);

        // Act
        var products = repo.GetAllAsync().Result;

        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual("Laptop", products[0].Name);
        Assert.AreEqual("Mouse", products[1].Name);
        Assert.AreEqual("Desk", products[2].Name);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_All_EmptyTable_ShouldReturnEmptyList()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        var repo = new BasicSelectProductRepository(connection);

        // Act
        var products = repo.GetAllAsync().Result;

        // Assert
        Assert.IsNotNull(products);
        Assert.AreEqual(0, products.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereSingleCondition_ShouldReturnMatchingRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Laptop', 999.99, 10, 'Electronics'),
            ('Mouse', 29.99, 50, 'Electronics'),
            ('Keyboard', 79.99, 5, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找库存少于20的产品
        var products = repo.GetLowStockAsync(20).Result;

        // Assert
        Assert.AreEqual(2, products.Count); // Laptop和Keyboard
        Assert.IsTrue(products.All(p => p.Stock < 20));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereMultipleConditions_AND_ShouldReturnMatching()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Gaming Laptop', 1500, 5, 'Electronics'),
            ('Budget Laptop', 500, 15, 'Electronics'),
            ('Mouse', 29.99, 50, 'Electronics'),
            ('Premium Desk', 599.99, 3, 'Furniture')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找价格>100 AND 库存<10的产品
        var products = repo.GetExpensiveLowStockAsync(100, 10).Result;

        // Assert
        Assert.AreEqual(2, products.Count); // Gaming Laptop和Premium Desk
        Assert.IsTrue(products.All(p => p.Price > 100 && p.Stock < 10));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereMultipleConditions_OR_ShouldReturnMatching()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Expensive Item', 2000, 50, 'Electronics'),
            ('Cheap Item', 10, 5, 'Electronics'),
            ('Normal Item', 100, 20, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找价格>1000 OR 库存<10的产品
        var products = repo.GetExpensiveOrLowStockAsync(1000, 10).Result;

        // Assert
        Assert.AreEqual(2, products.Count); // Expensive Item和Cheap Item
        Assert.IsTrue(products.All(p => p.Price > 1000 || p.Stock < 10));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereLike_ShouldReturnMatchingRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Gaming Laptop', 1500, 5, 'Electronics'),
            ('Business Laptop', 1200, 8, 'Electronics'),
            ('Wireless Mouse', 29.99, 50, 'Electronics'),
            ('USB Cable', 9.99, 100, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找名称包含'Laptop'的产品
        var products = repo.SearchByNameAsync("Laptop").Result;

        // Assert
        Assert.AreEqual(2, products.Count);
        Assert.IsTrue(products.All(p => p.Name.Contains("Laptop")));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereBetween_ShouldReturnRecordsInRange()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Cheap Item', 10, 100, 'Electronics'),
            ('Mid Item 1', 50, 50, 'Electronics'),
            ('Mid Item 2', 75, 30, 'Electronics'),
            ('Mid Item 3', 100, 20, 'Electronics'),
            ('Expensive Item', 500, 5, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找价格在50-100之间的产品
        var products = repo.GetPriceRangeAsync(50, 100).Result;

        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.IsTrue(products.All(p => p.Price >= 50 && p.Price <= 100));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereIn_ShouldReturnMatchingRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Laptop', 999, 10, 'Electronics'),
            ('Desk', 299, 5, 'Furniture'),
            ('Chair', 199, 15, 'Furniture'),
            ('Mouse', 29, 50, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找类别在Electronics或Furniture中的产品
        var products = repo.GetByCategoriesAsync().Result;

        // Assert
        Assert.AreEqual(4, products.Count);
        Assert.IsTrue(products.All(p => p.Category == "Electronics" || p.Category == "Furniture"));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereIsNull_ShouldReturnNullRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Item 1', 100, 10, 'Electronics'),
            ('Item 2', 200, 20, NULL),
            ('Item 3', 300, 30, NULL)");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找category为NULL的产品
        var products = repo.GetNoCategoryAsync().Result;

        // Assert
        Assert.AreEqual(2, products.Count);
        Assert.IsTrue(products.All(p => p.Category == null));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_WhereIsNotNull_ShouldReturnNonNullRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Item 1', 100, 10, 'Electronics'),
            ('Item 2', 200, 20, NULL),
            ('Item 3', 300, 30, 'Furniture')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 查找category不为NULL的产品
        var products = repo.GetWithCategoryAsync().Result;

        // Assert
        Assert.AreEqual(2, products.Count);
        Assert.IsTrue(products.All(p => p.Category != null));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_OrderByAsc_ShouldReturnSortedRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Item C', 300, 30, 'Electronics'),
            ('Item A', 100, 10, 'Electronics'),
            ('Item B', 200, 20, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 按价格升序排序
        var products = repo.GetAllOrderByPriceAscAsync().Result;

        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual(100, products[0].Price);
        Assert.AreEqual(200, products[1].Price);
        Assert.AreEqual(300, products[2].Price);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_OrderByDesc_ShouldReturnSortedRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Item C', 300, 30, 'Electronics'),
            ('Item A', 100, 10, 'Electronics'),
            ('Item B', 200, 20, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 按价格降序排序
        var products = repo.GetAllOrderByPriceDescAsync().Result;

        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual(300, products[0].Price);
        Assert.AreEqual(200, products[1].Price);
        Assert.AreEqual(100, products[2].Price);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Select")]
    [TestCategory("Phase1")]
    public void Select_OrderByMultipleColumns_ShouldWork()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT
            )");

        connection.Execute(@"
            INSERT INTO products (name, price, stock, category) VALUES
            ('Item 1', 100, 30, 'Electronics'),
            ('Item 2', 100, 10, 'Electronics'),
            ('Item 3', 200, 20, 'Electronics'),
            ('Item 4', 100, 20, 'Electronics')");

        var repo = new BasicSelectProductRepository(connection);

        // Act - 按价格升序，然后按库存升序
        var products = repo.GetAllOrderByPriceAndStockAsync().Result;

        // Assert
        Assert.AreEqual(4, products.Count);
        // 价格100的三个产品，按库存排序: 10, 20, 30
        Assert.AreEqual(100, products[0].Price);
        Assert.AreEqual(10, products[0].Stock);
        Assert.AreEqual(100, products[1].Price);
        Assert.AreEqual(20, products[1].Stock);
        Assert.AreEqual(100, products[2].Price);
        Assert.AreEqual(30, products[2].Stock);
        // 价格200的产品在最后
        Assert.AreEqual(200, products[3].Price);

        connection.Dispose();
    }
}

#region Test Models and Repositories

public class BasicSelectProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
}

public interface IBasicSelectProductRepository
{
    // 基础SELECT
    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE id = @id")]
    Task<BasicSelectProduct?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, name, price, stock, category FROM products")]
    Task<List<BasicSelectProduct>> GetAllAsync();

    // WHERE条件
    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE stock < @maxStock")]
    Task<List<BasicSelectProduct>> GetLowStockAsync(int maxStock);

    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE price > @minPrice AND stock < @maxStock")]
    Task<List<BasicSelectProduct>> GetExpensiveLowStockAsync(double minPrice, int maxStock);

    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE price > @minPrice OR stock < @maxStock")]
    Task<List<BasicSelectProduct>> GetExpensiveOrLowStockAsync(double minPrice, int maxStock);

    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE name LIKE '%' || @keyword || '%'")]
    Task<List<BasicSelectProduct>> SearchByNameAsync(string keyword);

    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE price BETWEEN @minPrice AND @maxPrice")]
    Task<List<BasicSelectProduct>> GetPriceRangeAsync(double minPrice, double maxPrice);

    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE category IN ('Electronics', 'Furniture')")]
    Task<List<BasicSelectProduct>> GetByCategoriesAsync();

    // NULL判断
    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE category IS NULL")]
    Task<List<BasicSelectProduct>> GetNoCategoryAsync();

    [SqlTemplate("SELECT id, name, price, stock, category FROM products WHERE category IS NOT NULL")]
    Task<List<BasicSelectProduct>> GetWithCategoryAsync();

    // ORDER BY
    [SqlTemplate("SELECT id, name, price, stock, category FROM products ORDER BY price ASC")]
    Task<List<BasicSelectProduct>> GetAllOrderByPriceAscAsync();

    [SqlTemplate("SELECT id, name, price, stock, category FROM products ORDER BY price DESC")]
    Task<List<BasicSelectProduct>> GetAllOrderByPriceDescAsync();

    [SqlTemplate("SELECT id, name, price, stock, category FROM products ORDER BY price ASC, stock ASC")]
    Task<List<BasicSelectProduct>> GetAllOrderByPriceAndStockAsync();
}

[RepositoryFor<IBasicSelectProductRepository>]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class BasicSelectProductRepository : IBasicSelectProductRepository
{
    private readonly IDbConnection _connection;

    public BasicSelectProductRepository(IDbConnection connection)
    {
        _connection = connection;
    }
}

#endregion


