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
/// Phase 1: WHERE条件增强测试 - 确保100%常用场景覆盖
/// 新增20个WHERE场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("CRUD")]
[TestCategory("CoveragePhase1")]
public class TDD_Where_Enhanced
{
    private IDbConnection? _connection;
    private IWhereEnhancedRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE items (
                id INTEGER PRIMARY KEY,
                name TEXT,
                price REAL,
                quantity INTEGER,
                category TEXT,
                is_active INTEGER,
                created_date TEXT
            )");

        // Insert test data
        _connection.Execute("INSERT INTO items VALUES (1, 'Item A', 100.0, 10, 'Electronics', 1, '2025-01-01')");
        _connection.Execute("INSERT INTO items VALUES (2, 'Item B', 200.0, 20, 'Books', 1, '2025-01-02')");
        _connection.Execute("INSERT INTO items VALUES (3, 'Item C', 150.0, 0, 'Electronics', 0, '2025-01-03')");
        _connection.Execute("INSERT INTO items VALUES (4, NULL, 50.0, 5, 'Toys', 1, NULL)");
        _connection.Execute("INSERT INTO items VALUES (5, 'Item E', 300.0, 15, NULL, 1, '2025-01-05')");

        _repo = new WhereEnhancedRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("WHERE with equals operator")]
    public async Task Where_Equals_ShouldWork()
    {
        var items = await _repo!.GetByCategoryAsync("Electronics");
        Assert.AreEqual(2, items.Count);
    }

    [TestMethod]
    [Description("WHERE with not equals operator")]
    public async Task Where_NotEquals_ShouldWork()
    {
        var items = await _repo!.GetNonElectronicsAsync();
        Assert.AreEqual(3, items.Count);
    }

    [TestMethod]
    [Description("WHERE with greater than")]
    public async Task Where_GreaterThan_ShouldWork()
    {
        var items = await _repo!.GetByPriceGreaterAsync(150.0);
        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.All(i => i.Price > 150.0));
    }

    [TestMethod]
    [Description("WHERE with less than")]
    public async Task Where_LessThan_ShouldWork()
    {
        var items = await _repo!.GetByPriceLessAsync(100.0);
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual(50.0, items[0].Price, 0.01);
    }

    [TestMethod]
    [Description("WHERE with greater than or equals")]
    public async Task Where_GreaterThanOrEquals_ShouldWork()
    {
        var items = await _repo!.GetByPriceGteAsync(150.0);
        Assert.AreEqual(3, items.Count);
    }

    [TestMethod]
    [Description("WHERE with less than or equals")]
    public async Task Where_LessThanOrEquals_ShouldWork()
    {
        var items = await _repo!.GetByPriceLteAsync(150.0);
        Assert.AreEqual(3, items.Count);
    }

    [TestMethod]
    [Description("WHERE with IN clause")]
    public async Task Where_In_ShouldWork()
    {
        var items = await _repo!.GetByCategoriesAsync();
        Assert.AreEqual(3, items.Count);
    }

    [TestMethod]
    [Description("WHERE with NOT IN clause")]
    public async Task Where_NotIn_ShouldWork()
    {
        var items = await _repo!.GetExcludingCategoriesAsync();
        Assert.AreEqual(2, items.Count);
    }

    [TestMethod]
    [Description("WHERE with LIKE clause")]
    public async Task Where_Like_ShouldWork()
    {
        var items = await _repo!.GetByNamePatternAsync("Item%");
        Assert.AreEqual(4, items.Count);
    }

    [TestMethod]
    [Description("WHERE with NOT LIKE clause")]
    public async Task Where_NotLike_ShouldWork()
    {
        var items = await _repo!.GetExcludingPatternAsync("Item A%");
        Assert.AreEqual(4, items.Count);
    }

    [TestMethod]
    [Description("WHERE with BETWEEN clause")]
    public async Task Where_Between_ShouldWork()
    {
        var items = await _repo!.GetByPriceRangeAsync(100.0, 200.0);
        Assert.AreEqual(3, items.Count);
        Assert.IsTrue(items.All(i => i.Price >= 100.0 && i.Price <= 200.0));
    }

    [TestMethod]
    [Description("WHERE with IS NULL")]
    public async Task Where_IsNull_ShouldWork()
    {
        var items = await _repo!.GetWithoutCategoryAsync();
        Assert.AreEqual(1, items.Count);
        Assert.IsNull(items[0].Category);
    }

    [TestMethod]
    [Description("WHERE with IS NOT NULL")]
    public async Task Where_IsNotNull_ShouldWork()
    {
        var items = await _repo!.GetWithCategoryAsync();
        Assert.AreEqual(4, items.Count);
        Assert.IsTrue(items.All(i => i.Category != null));
    }

    [TestMethod]
    [Description("WHERE with AND operator")]
    public async Task Where_And_ShouldWork()
    {
        var items = await _repo!.GetActiveElectronicsAsync();
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("Electronics", items[0].Category);
        Assert.IsTrue(items[0].IsActive);
    }

    [TestMethod]
    [Description("WHERE with OR operator")]
    public async Task Where_Or_ShouldWork()
    {
        var items = await _repo!.GetElectronicsOrBooksAsync();
        Assert.AreEqual(3, items.Count);
    }

    [TestMethod]
    [Description("WHERE with complex AND/OR combination")]
    public async Task Where_AndOrCombination_ShouldWork()
    {
        var items = await _repo!.GetComplexConditionAsync();
        Assert.AreEqual(2, items.Count);
    }

    [TestMethod]
    [Description("WHERE with boolean column")]
    public async Task Where_Boolean_ShouldWork()
    {
        var items = await _repo!.GetActiveItemsAsync();
        Assert.AreEqual(4, items.Count);
        Assert.IsTrue(items.All(i => i.IsActive));
    }

    [TestMethod]
    [Description("WHERE with integer comparison")]
    public async Task Where_IntegerComparison_ShouldWork()
    {
        var items = await _repo!.GetLowStockAsync(10);
        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.All(i => i.Quantity < 10));
    }

    [TestMethod]
    [Description("WHERE with string comparison")]
    public async Task Where_StringComparison_ShouldWork()
    {
        var items = await _repo!.GetByExactNameAsync("Item A");
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("Item A", items[0].Name);
    }

    [TestMethod]
    [Description("WHERE with empty string")]
    public async Task Where_EmptyString_ShouldWork()
    {
        _connection!.Execute("INSERT INTO items VALUES (6, '', 10.0, 1, 'Test', 1, '2025-01-01')");
        var items = await _repo!.GetByExactNameAsync("");
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("", items[0].Name);
    }
}

// Repository interface
public interface IWhereEnhancedRepository
{
    [SqlTemplate("SELECT * FROM items WHERE category = @category")]
    Task<List<WhereItem>> GetByCategoryAsync(string category);

    [SqlTemplate("SELECT * FROM items WHERE category != 'Electronics' OR category IS NULL")]
    Task<List<WhereItem>> GetNonElectronicsAsync();

    [SqlTemplate("SELECT * FROM items WHERE price > @minPrice")]
    Task<List<WhereItem>> GetByPriceGreaterAsync(double minPrice);

    [SqlTemplate("SELECT * FROM items WHERE price < @maxPrice")]
    Task<List<WhereItem>> GetByPriceLessAsync(double maxPrice);

    [SqlTemplate("SELECT * FROM items WHERE price >= @minPrice")]
    Task<List<WhereItem>> GetByPriceGteAsync(double minPrice);

    [SqlTemplate("SELECT * FROM items WHERE price <= @maxPrice")]
    Task<List<WhereItem>> GetByPriceLteAsync(double maxPrice);

    [SqlTemplate("SELECT * FROM items WHERE category IN ('Electronics', 'Books')")]
    Task<List<WhereItem>> GetByCategoriesAsync();

    [SqlTemplate("SELECT * FROM items WHERE category NOT IN ('Electronics', 'Books') OR category IS NULL")]
    Task<List<WhereItem>> GetExcludingCategoriesAsync();

    [SqlTemplate("SELECT * FROM items WHERE name LIKE @pattern")]
    Task<List<WhereItem>> GetByNamePatternAsync(string pattern);

    [SqlTemplate("SELECT * FROM items WHERE name NOT LIKE @pattern OR name IS NULL")]
    Task<List<WhereItem>> GetExcludingPatternAsync(string pattern);

    [SqlTemplate("SELECT * FROM items WHERE price BETWEEN @minPrice AND @maxPrice")]
    Task<List<WhereItem>> GetByPriceRangeAsync(double minPrice, double maxPrice);

    [SqlTemplate("SELECT * FROM items WHERE category IS NULL")]
    Task<List<WhereItem>> GetWithoutCategoryAsync();

    [SqlTemplate("SELECT * FROM items WHERE category IS NOT NULL")]
    Task<List<WhereItem>> GetWithCategoryAsync();

    [SqlTemplate("SELECT * FROM items WHERE is_active = 1 AND category = 'Electronics'")]
    Task<List<WhereItem>> GetActiveElectronicsAsync();

    [SqlTemplate("SELECT * FROM items WHERE category = 'Electronics' OR category = 'Books'")]
    Task<List<WhereItem>> GetElectronicsOrBooksAsync();

    [SqlTemplate("SELECT * FROM items WHERE (category = 'Electronics' OR category = 'Books') AND is_active = 1")]
    Task<List<WhereItem>> GetComplexConditionAsync();

    [SqlTemplate("SELECT * FROM items WHERE is_active = 1")]
    Task<List<WhereItem>> GetActiveItemsAsync();

    [SqlTemplate("SELECT * FROM items WHERE quantity < @maxQuantity")]
    Task<List<WhereItem>> GetLowStockAsync(int maxQuantity);

    [SqlTemplate("SELECT * FROM items WHERE name = @name")]
    Task<List<WhereItem>> GetByExactNameAsync(string name);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IWhereEnhancedRepository))]
public partial class WhereEnhancedRepository(IDbConnection connection) : IWhereEnhancedRepository { }

// Model
public class WhereItem
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public double Price { get; set; }
    public int Quantity { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedDate { get; set; }
}

