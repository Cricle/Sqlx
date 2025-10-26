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
/// Phase 1: UPDATE操作增强测试 - 确保100%常用场景覆盖
/// 新增15个UPDATE场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("CRUD")]
[TestCategory("CoveragePhase1")]
public class TDD_Update_Enhanced
{
    private IDbConnection? _connection;
    private IUpdateEnhancedRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                category TEXT,
                created_at TEXT
            )");

        _repo = new UpdateEnhancedRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("UPDATE with NULL value should work correctly")]
    public async Task Update_WithNullValue_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product A', 100.0, 10, 'Electronics', '2025-01-01')");

        // Act - Set category to NULL
        var affected = await _repo!.UpdateCategoryAsync(1, null);

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.IsNull(product!.Category);
    }

    [TestMethod]
    [Description("UPDATE with BETWEEN clause should update range")]
    public async Task Update_WithBetween_ShouldUpdateRange()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'P1', 50.0, 10, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (2, 'P2', 100.0, 20, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (3, 'P3', 150.0, 30, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (4, 'P4', 200.0, 40, 'A', '2025-01-01')");

        // Act - Apply 10% discount to products priced 100-150
        var affected = await _repo!.UpdatePriceByRangeAsync(100.0, 150.0, 0.9);

        // Assert
        Assert.AreEqual(2, affected);
        var products = await _repo.GetAllAsync();
        Assert.AreEqual(90.0, products.First(p => p.Id == 2).Price, 0.01);
        Assert.AreEqual(135.0, products.First(p => p.Id == 3).Price, 0.01);
        Assert.AreEqual(50.0, products.First(p => p.Id == 1).Price, 0.01); // Unchanged
    }

    [TestMethod]
    [Description("UPDATE with IN clause should update multiple records")]
    public async Task Update_WithIn_ShouldUpdateMultiple()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'P1', 100.0, 10, 'A', '2025-01-01')");
        _connection!.Execute("INSERT INTO products VALUES (2, 'P2', 200.0, 20, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (3, 'P3', 300.0, 30, 'A', '2025-01-01')");

        // Act - Update specific IDs (1 and 3)
        var affected = await _repo!.UpdateCategoryByIdsAsync("Featured");

        // Assert
        Assert.AreEqual(2, affected);
        var products = await _repo.GetAllAsync();
        Assert.AreEqual("Featured", products.First(p => p.Id == 1).Category);
        Assert.AreEqual("Featured", products.First(p => p.Id == 3).Category);
        Assert.AreEqual("A", products.First(p => p.Id == 2).Category);
    }

    [TestMethod]
    [Description("UPDATE with LIKE clause should match patterns")]
    public async Task Update_WithLike_ShouldMatchPattern()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Premium Product A', 100.0, 10, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (2, 'Product B', 200.0, 20, 'B', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (3, 'Premium Product C', 300.0, 30, 'C', '2025-01-01')");

        // Act - Update all premium products
        var affected = await _repo!.UpdateCategoryByNamePatternAsync("Premium%", "Premium");

        // Assert
        Assert.AreEqual(2, affected);
        var products = await _repo.GetAllAsync();
        Assert.AreEqual(2, products.Count(p => p.Category == "Premium"));
    }

    [TestMethod]
    [Description("UPDATE decimal precision should be maintained")]
    public async Task Update_DecimalPrecision_ShouldBeMaintained()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 99.99, 10, 'A', '2025-01-01')");

        // Act
        var affected = await _repo!.UpdatePriceAsync(1, 123.456789);

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual(123.456789, product!.Price, 0.000001);
    }

    [TestMethod]
    [Description("UPDATE with expression (price * 1.1) should calculate correctly")]
    public async Task Update_WithExpression_ShouldCalculate()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 100.0, 10, 'A', '2025-01-01')");

        // Act - Increase price by 10%
        var affected = await _repo!.IncreasePriceAsync(1, 1.1);

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual(110.0, product!.Price, 0.01);
    }

    [TestMethod]
    [Description("UPDATE multiple fields should update all")]
    public async Task Update_MultipleFields_ShouldUpdateAll()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Old', 100.0, 10, 'A', '2025-01-01')");

        // Act
        var affected = await _repo!.UpdateProductAsync(1, "New", 200.0, 20);

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual("New", product!.Name);
        Assert.AreEqual(200.0, product.Price, 0.01);
        Assert.AreEqual(20, product.Stock);
    }

    [TestMethod]
    [Description("UPDATE with WHERE comparing numbers should work")]
    public async Task Update_WhereNumericComparison_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'P1', 50.0, 5, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (2, 'P2', 100.0, 10, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (3, 'P3', 150.0, 15, 'A', '2025-01-01')");

        // Act - Update products with stock < 12
        var affected = await _repo!.UpdateCategoryByStockAsync(12, "LowStock");

        // Assert
        Assert.AreEqual(2, affected);
        var products = await _repo.GetAllAsync();
        Assert.AreEqual(2, products.Count(p => p.Category == "LowStock"));
    }

    [TestMethod]
    [Description("UPDATE affecting 0 rows should return 0")]
    public async Task Update_NoMatches_ShouldReturn0()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 100.0, 10, 'A', '2025-01-01')");

        // Act - Try to update non-existent ID
        var affected = await _repo!.UpdateCategoryAsync(999, "NewCategory");

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [Description("UPDATE all records (no WHERE) should update entire table")]
    public async Task Update_AllRecords_ShouldUpdateTable()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'P1', 100.0, 10, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (2, 'P2', 200.0, 20, 'B', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (3, 'P3', 300.0, 30, 'C', '2025-01-01')");

        // Act - Set all categories to "Standard"
        var affected = await _repo!.UpdateAllCategoriesAsync("Standard");

        // Assert
        Assert.AreEqual(3, affected);
        var products = await _repo.GetAllAsync();
        Assert.IsTrue(products.All(p => p.Category == "Standard"));
    }

    [TestMethod]
    [Description("UPDATE with greater than operator should work")]
    public async Task Update_WithGreaterThan_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'P1', 100.0, 10, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (2, 'P2', 200.0, 20, 'A', '2025-01-01')");
        _connection.Execute("INSERT INTO products VALUES (3, 'P3', 300.0, 30, 'A', '2025-01-01')");

        // Act - Update products with price > 150
        var affected = await _repo!.UpdateCategoryByPriceAsync(150.0, "Premium");

        // Assert
        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    [Description("UPDATE incrementing a counter should work")]
    public async Task Update_IncrementCounter_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 100.0, 10, 'A', '2025-01-01')");

        // Act
        await _repo!.IncrementStockAsync(1, 5);
        await _repo.IncrementStockAsync(1, 3);

        // Assert
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual(18, product!.Stock); // 10 + 5 + 3
    }

    [TestMethod]
    [Description("UPDATE with empty string should work")]
    public async Task Update_WithEmptyString_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 100.0, 10, 'OldCategory', '2025-01-01')");

        // Act
        var affected = await _repo!.UpdateCategoryAsync(1, "");

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual("", product!.Category);
    }

    [TestMethod]
    [Description("UPDATE with negative numbers should work")]
    public async Task Update_WithNegativeNumbers_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 100.0, 10, 'A', '2025-01-01')");

        // Act - Set stock to negative (for backorders)
        var affected = await _repo!.UpdateStockAsync(1, -5);

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual(-5, product!.Stock);
    }

    [TestMethod]
    [Description("UPDATE with very large numbers should work")]
    public async Task Update_WithLargeNumbers_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO products VALUES (1, 'Product', 100.0, 10, 'A', '2025-01-01')");

        // Act
        var affected = await _repo!.UpdatePriceAsync(1, 999999999.99);

        // Assert
        Assert.AreEqual(1, affected);
        var product = await _repo.GetByIdAsync(1);
        Assert.AreEqual(999999999.99, product!.Price, 0.01);
    }
}

// Repository interface
public interface IUpdateEnhancedRepository
{
    [SqlTemplate("SELECT * FROM products WHERE id = @id")]
    Task<EnhancedProduct?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM products")]
    Task<List<EnhancedProduct>> GetAllAsync();

    [SqlTemplate("UPDATE products SET category = @category WHERE id = @id")]
    Task<int> UpdateCategoryAsync(long id, string? category);

    [SqlTemplate("UPDATE products SET price = price * @multiplier WHERE price BETWEEN @minPrice AND @maxPrice")]
    Task<int> UpdatePriceByRangeAsync(double minPrice, double maxPrice, double multiplier);

    [SqlTemplate("UPDATE products SET category = @category WHERE id IN (1, 3)")]
    Task<int> UpdateCategoryByIdsAsync(string category);

    [SqlTemplate("UPDATE products SET category = @category WHERE name LIKE @pattern")]
    Task<int> UpdateCategoryByNamePatternAsync(string pattern, string category);

    [SqlTemplate("UPDATE products SET price = @price WHERE id = @id")]
    Task<int> UpdatePriceAsync(long id, double price);

    [SqlTemplate("UPDATE products SET price = price * @multiplier WHERE id = @id")]
    Task<int> IncreasePriceAsync(long id, double multiplier);

    [SqlTemplate("UPDATE products SET name = @name, price = @price, stock = @stock WHERE id = @id")]
    Task<int> UpdateProductAsync(long id, string name, double price, int stock);

    [SqlTemplate("UPDATE products SET category = @category WHERE stock < @maxStock")]
    Task<int> UpdateCategoryByStockAsync(int maxStock, string category);

    [SqlTemplate("UPDATE products SET category = @category")]
    Task<int> UpdateAllCategoriesAsync(string category);

    [SqlTemplate("UPDATE products SET category = @category WHERE price > @minPrice")]
    Task<int> UpdateCategoryByPriceAsync(double minPrice, string category);

    [SqlTemplate("UPDATE products SET stock = stock + @amount WHERE id = @id")]
    Task<int> IncrementStockAsync(long id, int amount);

    [SqlTemplate("UPDATE products SET stock = @stock WHERE id = @id")]
    Task<int> UpdateStockAsync(long id, int stock);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUpdateEnhancedRepository))]
public partial class UpdateEnhancedRepository(IDbConnection connection) : IUpdateEnhancedRepository { }

// Model
public class EnhancedProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public string? CreatedAt { get; set; }
}

