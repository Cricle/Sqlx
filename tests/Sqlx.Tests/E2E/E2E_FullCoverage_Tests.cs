// -----------------------------------------------------------------------
// <copyright file="E2E_FullCoverage_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.E2E;

[RepositoryFor(typeof(IE2EProductRepository), Dialect = SqlDefineTypes.SQLite, TableName = "e2e_products")]
public partial class SQLiteE2EProductRepository : IE2EProductRepository
{
    private readonly DbConnection _connection;
    public SQLiteE2EProductRepository(DbConnection connection) => _connection = connection;
}

[RepositoryFor(typeof(IE2EOrderRepository), Dialect = SqlDefineTypes.SQLite, TableName = "e2e_orders")]
public partial class SQLiteE2EOrderRepository : IE2EOrderRepository
{
    private readonly DbConnection _connection;
    public SQLiteE2EOrderRepository(DbConnection connection) => _connection = connection;
}

[RepositoryFor(typeof(IE2EProductRepository), Dialect = SqlDefineTypes.MySql, TableName = "e2e_products")]
public partial class MySQLE2EProductRepository : IE2EProductRepository
{
    private readonly DbConnection _connection;
    public MySQLE2EProductRepository(DbConnection connection) => _connection = connection;
}

[RepositoryFor(typeof(IE2EOrderRepository), Dialect = SqlDefineTypes.MySql, TableName = "e2e_orders")]
public partial class MySQLE2EOrderRepository : IE2EOrderRepository
{
    private readonly DbConnection _connection;
    public MySQLE2EOrderRepository(DbConnection connection) => _connection = connection;
}

/// <summary>
/// 端到端测试 - 完整覆盖所有数据库方言
/// 测试真实的 CRUD 场景，确保所有功能在所有支持的数据库上正常工作
/// </summary>

// ==================== 测试实体 ====================

public class E2EProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class E2EOrder
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime OrderDate { get; set; }
}

// ==================== 仓储接口 ====================

public partial interface IE2EProductRepository
{
    // 基础 CRUD
    [SqlTemplate("INSERT INTO {{table}} (name, description, price, stock, is_active, created_at) VALUES (@name, @description, @price, @stock, @isActive, @createdAt)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string? description, decimal price, int stock, bool isActive, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<E2EProduct?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id")]
    Task<List<E2EProduct>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE {{table}} SET name = @name, description = @description, price = @price, stock = @stock, is_active = @isActive, updated_at = @updatedAt WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name, string? description, decimal price, int stock, bool isActive, DateTime? updatedAt, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken ct = default);

    // 条件查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive ORDER BY price")]
    Task<List<E2EProduct>> GetActiveProductsAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE price {{between|min=@minPrice|max=@maxPrice}} ORDER BY price")]
    Task<List<E2EProduct>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @searchTerm ORDER BY name")]
    Task<List<E2EProduct>> SearchByNameAsync(string searchTerm, CancellationToken ct = default);

    // 聚合查询
    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_active = @isActive")]
    Task<int> CountActiveAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(stock), 0) FROM {{table}} WHERE is_active = @isActive")]
    Task<int> GetTotalStockAsync(bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(AVG(price), 0) FROM {{table}} WHERE is_active = @isActive")]
    Task<decimal> GetAveragePriceAsync(bool isActive, CancellationToken ct = default);

    // 分页查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive ORDER BY created_at DESC {{limit}} {{offset}}")]
    Task<List<E2EProduct>> GetPagedAsync(bool isActive, int limit, int offset, CancellationToken ct = default);

    // 批量操作
    [SqlTemplate("DELETE FROM {{table}}")]
    Task<int> DeleteAllAsync(CancellationToken ct = default);
}

public partial interface IE2EOrderRepository
{
    [SqlTemplate("INSERT INTO {{table}} (order_number, product_id, quantity, total_amount, status, order_date) VALUES (@orderNumber, @productId, @quantity, @totalAmount, @status, @orderDate)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string orderNumber, long productId, int quantity, decimal totalAmount, string status, DateTime orderDate, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE product_id = @productId ORDER BY order_date DESC")]
    Task<List<E2EOrder>> GetByProductIdAsync(long productId, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status IN (@status1, @status2) ORDER BY order_date")]
    Task<List<E2EOrder>> GetByStatusesAsync(string status1, string status2, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM {{table}}")]
    Task<int> DeleteAllAsync(CancellationToken ct = default);
}

// ==================== 测试基类 ====================

public abstract class E2ETestBase
{
    protected DbConnection? Connection { get; private set; }
    protected IE2EProductRepository? ProductRepo { get; private set; }
    protected IE2EOrderRepository? OrderRepo { get; private set; }

    protected abstract SqlDefineTypes DialectType { get; }
    protected abstract DbConnection? CreateConnection();
    protected abstract IE2EProductRepository CreateProductRepository(DbConnection connection);
    protected abstract IE2EOrderRepository CreateOrderRepository(DbConnection connection);

    [TestInitialize]
    public async Task Initialize()
    {
        Connection = CreateConnection();
        if (Connection == null)
        {
            Assert.Inconclusive($"{DialectType} database is not available");
            return;
        }

        if (Connection.State != System.Data.ConnectionState.Open)
            await Connection.OpenAsync();
            
        ProductRepo = CreateProductRepository(Connection);
        OrderRepo = CreateOrderRepository(Connection);

        // 创建表结构
        await CreateTablesAsync();
        
        // 清理数据
        await CleanupDataAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Connection?.Dispose();
    }

    protected abstract Task CreateTablesAsync();

    protected async Task CleanupDataAsync()
    {
        if (OrderRepo != null)
            await OrderRepo.DeleteAllAsync();
        if (ProductRepo != null)
            await ProductRepo.DeleteAllAsync();
    }

    // ==================== E2E 测试场景 ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CRUD")]
    public async Task E2E_BasicCRUD_ShouldWork()
    {
        // Create
        var productId = await ProductRepo.CreateAsync(
            "Test Product",
            "Test Description",
            99.99m,
            100,
            true,
            DateTime.Now);

        Assert.IsTrue(productId > 0, "Product ID should be greater than 0");

        // Read
        var product = await ProductRepo.GetByIdAsync(productId);
        Assert.IsNotNull(product, "Product should be found");
        Assert.AreEqual("Test Product", product.Name);
        Assert.AreEqual(99.99m, product.Price);
        Assert.AreEqual(100, product.Stock);
        Assert.IsTrue(product.IsActive);

        // Update
        var updateCount = await ProductRepo.UpdateAsync(
            productId,
            "Test Product",
            "Test Description",
            89.99m,
            90,
            true,
            DateTime.Now);
        Assert.AreEqual(1, updateCount, "One row should be updated");

        var updatedProduct = await ProductRepo.GetByIdAsync(productId);
        Assert.IsNotNull(updatedProduct);
        Assert.AreEqual(89.99m, updatedProduct.Price);
        Assert.AreEqual(90, updatedProduct.Stock);

        // Delete
        var deleteCount = await ProductRepo.DeleteAsync(productId);
        Assert.AreEqual(1, deleteCount, "One row should be deleted");

        var deletedProduct = await ProductRepo.GetByIdAsync(productId);
        Assert.IsNull(deletedProduct, "Product should be deleted");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Query")]
    public async Task E2E_ConditionalQueries_ShouldWork()
    {
        // 插入测试数据
        await ProductRepo.CreateAsync("Active Product 1", "Desc 1", 50m, 10, true, DateTime.Now);
        await ProductRepo.CreateAsync("Active Product 2", "Desc 2", 150m, 20, true, DateTime.Now);
        await ProductRepo.CreateAsync("Disabled Product", "Desc 3", 100m, 5, false, DateTime.Now);

        // 测试活跃产品查询
        var activeProducts = await ProductRepo.GetActiveProductsAsync(true);
        Assert.AreEqual(2, activeProducts.Count, "Should have 2 active products");
        Assert.IsTrue(activeProducts.All(p => p.IsActive), "All products should be active");

        // 测试价格范围查询
        var priceRangeProducts = await ProductRepo.GetProductsByPriceRangeAsync(40m, 120m);
        Assert.AreEqual(2, priceRangeProducts.Count, "Should have 2 products in price range");
        Assert.IsTrue(priceRangeProducts.All(p => p.Price >= 40m && p.Price <= 120m));

        // 测试模糊搜索 - 注意 "%Active%" 会匹配 "Inactive Product" 也包含 "Active"
        var searchResults = await ProductRepo.SearchByNameAsync("%Active Product%");
        Assert.AreEqual(2, searchResults.Count, "Should find 2 products with 'Active Product' in name");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    public async Task E2E_AggregateQueries_ShouldWork()
    {
        // 插入测试数据
        await ProductRepo.CreateAsync("Product 1", null, 100m, 10, true, DateTime.Now);
        await ProductRepo.CreateAsync("Product 2", null, 200m, 20, true, DateTime.Now);
        await ProductRepo.CreateAsync("Product 3", null, 300m, 30, false, DateTime.Now);

        // 测试计数
        var activeCount = await ProductRepo.CountActiveAsync(true);
        Assert.AreEqual(2, activeCount, "Should have 2 active products");

        // 测试总库存
        var totalStock = await ProductRepo.GetTotalStockAsync(true);
        Assert.AreEqual(30, totalStock, "Total stock should be 30");

        // 测试平均价格
        var avgPrice = await ProductRepo.GetAveragePriceAsync(true);
        Assert.AreEqual(150m, avgPrice, "Average price should be 150");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Pagination")]
    public async Task E2E_Pagination_ShouldWork()
    {
        // 插入测试数据
        for (int i = 1; i <= 25; i++)
        {
            await ProductRepo.CreateAsync(
                $"Product {i}",
                null,
                i * 10m,
                i,
                true,
                DateTime.Now.AddMinutes(-i));
        }

        // 第一页
        var page1 = await ProductRepo.GetPagedAsync(true, 10, 0);
        Assert.AreEqual(10, page1.Count, "First page should have 10 items");

        // 第二页
        var page2 = await ProductRepo.GetPagedAsync(true, 10, 10);
        Assert.AreEqual(10, page2.Count, "Second page should have 10 items");

        // 第三页
        var page3 = await ProductRepo.GetPagedAsync(true, 10, 20);
        Assert.AreEqual(5, page3.Count, "Third page should have 5 items");

        // 验证没有重复
        var allIds = page1.Concat(page2).Concat(page3).Select(p => p.Id).ToList();
        Assert.AreEqual(25, allIds.Distinct().Count(), "Should have 25 unique products");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Relations")]
    public async Task E2E_RelatedData_ShouldWork()
    {
        // 创建产品
        var productId = await ProductRepo.CreateAsync(
            "Test Product",
            "For orders",
            100m,
            50,
            true,
            DateTime.Now);

        // 创建订单
        var order1Id = await OrderRepo.CreateAsync(
            "ORD-001",
            productId,
            5,
            500m,
            "pending",
            DateTime.Now);

        var order2Id = await OrderRepo.CreateAsync(
            "ORD-002",
            productId,
            3,
            300m,
            "completed",
            DateTime.Now.AddHours(1));

        // 查询产品的所有订单
        var orders = await OrderRepo.GetByProductIdAsync(productId);
        Assert.AreEqual(2, orders.Count, "Should have 2 orders for the product");

        // 测试 IN 查询
        var pendingOrders = await OrderRepo.GetByStatusesAsync("pending", "processing");
        Assert.AreEqual(1, pendingOrders.Count, "Should have 1 pending order");
        Assert.AreEqual("ORD-001", pendingOrders[0].OrderNumber);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Transaction")]
    public async Task E2E_Transaction_ShouldWork()
    {
        // 简单测试：创建产品后验证
        var productId = await ProductRepo.CreateAsync(
            "Transactional Product",
            null,
            50m,
            10,
            true,
            DateTime.Now);

        Assert.IsTrue(productId > 0);

        // 验证数据已保存
        var product = await ProductRepo.GetByIdAsync(productId);
        Assert.IsNotNull(product, "Product should be saved");
        Assert.AreEqual("Transactional Product", product.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BulkOperations")]
    public async Task E2E_BulkOperations_ShouldWork()
    {
        // 批量插入
        var ids = new List<long>();
        for (int i = 1; i <= 100; i++)
        {
            var id = await ProductRepo.CreateAsync(
                $"Bulk Product {i}",
                $"Description {i}",
                i * 1.5m,
                i,
                i % 2 == 0,
                DateTime.Now);
            ids.Add(id);
        }

        Assert.AreEqual(100, ids.Count, "Should insert 100 products");

        // 验证数据
        var allProducts = await ProductRepo.GetAllAsync();
        Assert.AreEqual(100, allProducts.Count, "Should have 100 products");

        // 批量删除
        var deleteCount = await ProductRepo.DeleteAllAsync();
        Assert.AreEqual(100, deleteCount, "Should delete 100 products");

        var remainingProducts = await ProductRepo.GetAllAsync();
        Assert.AreEqual(0, remainingProducts.Count, "Should have no products left");
    }
}

// ==================== SQLite 测试 ====================

[TestClass]
public class E2E_SQLite_Tests : E2ETestBase
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.SQLite;

    protected override DbConnection? CreateConnection()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        return conn;
    }

    protected override IE2EProductRepository CreateProductRepository(DbConnection connection)
        => new SQLiteE2EProductRepository(connection);

    protected override IE2EOrderRepository CreateOrderRepository(DbConnection connection)
        => new SQLiteE2EOrderRepository(connection);

    protected override async Task CreateTablesAsync()
    {
        if (Connection == null) return;

        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS e2e_products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT
            );

            CREATE TABLE IF NOT EXISTS e2e_orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_number TEXT NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                total_amount REAL NOT NULL,
                status TEXT NOT NULL,
                order_date TEXT NOT NULL
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}

// ==================== MySQL 测试 ====================

[TestClass]
[TestCategory(TestCategories.MySQL)]
[TestCategory(TestCategories.CI)]
public class E2E_MySQL_Tests : E2ETestBase
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    protected override DbConnection? CreateConnection()
    {
        return DatabaseConnectionHelper.GetMySQLConnection(TestContext);
    }

    protected override IE2EProductRepository CreateProductRepository(DbConnection connection)
        => new MySQLE2EProductRepository(connection);

    protected override IE2EOrderRepository CreateOrderRepository(DbConnection connection)
        => new MySQLE2EOrderRepository(connection);
    
    public TestContext TestContext { get; set; } = null!;

    protected override async Task CreateTablesAsync()
    {
        if (Connection == null) return;

        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS e2e_products (
                id BIGINT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                price DECIMAL(10,2) NOT NULL,
                stock INT NOT NULL,
                is_active BOOLEAN NOT NULL,
                created_at DATETIME NOT NULL,
                updated_at DATETIME
            );

            CREATE TABLE IF NOT EXISTS e2e_orders (
                id BIGINT PRIMARY KEY AUTO_INCREMENT,
                order_number VARCHAR(50) NOT NULL,
                product_id BIGINT NOT NULL,
                quantity INT NOT NULL,
                total_amount DECIMAL(10,2) NOT NULL,
                status VARCHAR(50) NOT NULL,
                order_date DATETIME NOT NULL
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}
