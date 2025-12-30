using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CRUD;

/// <summary>
/// TDD: 基础INSERT操作的完整测试覆盖 - 最常用场景
/// Phase 1.2: 12个基础INSERT测试
/// </summary>
[TestClass]
public class TDD_Insert_Basic
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_SingleRecord_ShouldAddToDatabase()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act
        var affected = repo.InsertAsync("Gaming Laptop", 1500, 10, "High-end laptop").Result;

        // Assert
        Assert.AreEqual(1, affected);

        // Verify insertion
        var products = repo.GetAllAsync().Result;
        Assert.AreEqual(1, products.Count);
        Assert.AreEqual("Gaming Laptop", products[0].Name);
        Assert.AreEqual(1500, products[0].Price);
        Assert.AreEqual(10, products[0].Stock);
        Assert.AreEqual("High-end laptop", products[0].Description);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_MultipleRecordsSequential_ShouldAddAll()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 连续插入多条记录
        repo.InsertAsync("Product 1", 100, 10, "Desc 1").Wait();
        repo.InsertAsync("Product 2", 200, 20, "Desc 2").Wait();
        repo.InsertAsync("Product 3", 300, 30, "Desc 3").Wait();

        // Assert
        var products = repo.GetAllAsync().Result;
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual("Product 1", products[0].Name);
        Assert.AreEqual("Product 2", products[1].Name);
        Assert.AreEqual("Product 3", products[2].Name);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithNullableField_ShouldWork()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入description为NULL的产品
        var affected = repo.InsertWithNullDescAsync("No Description Product", 50, 5).Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual("No Description Product", product.Name);
        Assert.IsNull(product.Description);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithDefaultValue_ShouldUseDefault()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL DEFAULT 0,
                stock INTEGER NOT NULL DEFAULT 0,
                description TEXT DEFAULT 'No description'
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 只插入name，让其他字段使用默认值
        connection.Execute("INSERT INTO products (name) VALUES ('Default Product')");

        // Assert
        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual("Default Product", product.Name);
        Assert.AreEqual(0, product.Price);
        Assert.AreEqual(0, product.Stock);
        Assert.AreEqual("No description", product.Description);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithMaxValues_ShouldWork()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入极大值
        var affected = repo.InsertAsync("Max Value Product", 999999.99, int.MaxValue, "Max values").Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(999999.99, product.Price, 0.01);
        Assert.AreEqual(int.MaxValue, product.Stock);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithMinValues_ShouldWork()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入极小值
        var affected = repo.InsertAsync("Min Value Product", 0.01, 1, "Min values").Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(0.01, product.Price, 0.001);
        Assert.AreEqual(1, product.Stock);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithZeroValues_ShouldWork()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入价格和库存为0
        var affected = repo.InsertAsync("Free Item", 0, 0, "Free giveaway").Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(0, product.Price);
        Assert.AreEqual(0, product.Stock);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithNegativeValues_ShouldWork()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入负值（某些业务场景可能需要）
        var affected = repo.InsertAsync("Discount", -10.50, -5, "Adjustment").Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(-10.50, product.Price, 0.01);
        Assert.AreEqual(-5, product.Stock);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithSpecialCharacters_ShouldEscape()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入包含特殊字符的数据
        var name = "O'Reilly's \"Best\" Product";
        var desc = "It's great & awesome!";
        var affected = repo.InsertAsync(name, 99.99, 10, desc).Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(name, product.Name);
        Assert.AreEqual(desc, product.Description);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_WithUnicodeCharacters_ShouldWork()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act - 插入中文、日文、表情符号
        var name = "产品名称 商品 🎉";
        var desc = "これは素晴らしい製品です！😊";
        var affected = repo.InsertAsync(name, 100, 20, desc).Result;

        // Assert
        Assert.AreEqual(1, affected);

        var product = repo.GetByIdAsync(1).Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(name, product.Name);
        Assert.AreEqual(desc, product.Description);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_VerifyAffectedRows_ShouldReturnOne()
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
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act
        var affected1 = repo.InsertAsync("Product 1", 100, 10, "Desc").Result;
        var affected2 = repo.InsertAsync("Product 2", 200, 20, "Desc").Result;

        // Assert - 每次INSERT都应该返回1
        Assert.AreEqual(1, affected1);
        Assert.AreEqual(1, affected2);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Insert")]
    [TestCategory("Phase1")]
    public void Insert_DuplicatePrimaryKey_ShouldThrowException()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                description TEXT
            )");

        var repo = new BasicInsertProductRepository(connection);

        // Act & Assert - 手动插入重复ID应该抛出异常
        connection.Execute("INSERT INTO products (id, name, price, stock) VALUES (1, 'Product 1', 100, 10)");

        Assert.ThrowsException<SqliteException>(() =>
        {
            connection.Execute("INSERT INTO products (id, name, price, stock) VALUES (1, 'Product 2', 200, 20)");
        });

        connection.Dispose();
    }
}

#region Test Models and Repositories

public class BasicInsertProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
}

public interface IBasicInsertProductRepository
{
    // INSERT operations
    [SqlTemplate("INSERT INTO products (name, price, stock, description) VALUES (@name, @price, @stock, @description)")]
    Task<int> InsertAsync(string name, double price, int stock, string? description);

    [SqlTemplate("INSERT INTO products (name, price, stock, description) VALUES (@name, @price, @stock, NULL)")]
    Task<int> InsertWithNullDescAsync(string name, double price, int stock);

    // SELECT for verification
    [SqlTemplate("SELECT id, name, price, stock, description FROM products WHERE id = @id")]
    Task<BasicInsertProduct?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, name, price, stock, description FROM products")]
    Task<List<BasicInsertProduct>> GetAllAsync();
}

[RepositoryFor<IBasicInsertProductRepository>]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class BasicInsertProductRepository : IBasicInsertProductRepository
{
    private readonly IDbConnection _connection;

    public BasicInsertProductRepository(IDbConnection connection)
    {
        _connection = connection;
    }
}

#endregion


