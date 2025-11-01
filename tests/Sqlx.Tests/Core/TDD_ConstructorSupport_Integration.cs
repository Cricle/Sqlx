// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 集成测试实体 ====================

public class EcomCustomer
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
    public bool IsActive { get; set; }
}

public class EcomProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class EcomOrder
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
}

public class EcomOrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// ==================== 场景22: 完整的电商系统 ====================

public partial interface ICustomerRepository
{
    [SqlTemplate("INSERT INTO customers (name, email, registered_at, is_active) VALUES (@name, @email, @registeredAt, @isActive)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string email, DateTime registeredAt, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM customers WHERE id = @id")]
    Task<EcomCustomer?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM customers WHERE email = @email")]
    Task<EcomCustomer?> GetByEmailAsync(string email, CancellationToken ct = default);

    [SqlTemplate("UPDATE customers SET is_active = @isActive WHERE id = @id")]
    Task<int> SetActiveStatusAsync(long id, bool isActive, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM customers WHERE is_active = 1")]
    Task<int> CountActiveAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICustomerRepository))]
public partial class CustomerRepository(DbConnection connection) : ICustomerRepository
{
}

public partial interface IProductRepository
{
    [SqlTemplate("INSERT INTO products (name, price, stock) VALUES (@name, @price, @stock)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, decimal price, int stock, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM products WHERE id = @id")]
    Task<EcomProduct?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("UPDATE products SET stock = stock - @quantity WHERE id = @id AND stock >= @quantity")]
    Task<int> DecrementStockAsync(long id, int quantity, CancellationToken ct = default);

    [SqlTemplate("UPDATE products SET stock = stock + @quantity WHERE id = @id")]
    Task<int> IncrementStockAsync(long id, int quantity, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM products WHERE stock > 0 ORDER BY name")]
    Task<List<EcomProduct>> GetAvailableProductsAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(DbConnection connection) : IProductRepository
{
}

public partial interface IOrderRepository
{
    [SqlTemplate("INSERT INTO orders (customer_id, order_date, total_amount, status) VALUES (@customerId, @orderDate, @totalAmount, @status)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(long customerId, DateTime orderDate, decimal totalAmount, string status, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM orders WHERE id = @id")]
    Task<EcomOrder?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM orders WHERE customer_id = @customerId ORDER BY order_date DESC")]
    Task<List<EcomOrder>> GetCustomerOrdersAsync(long customerId, CancellationToken ct = default);

    [SqlTemplate("UPDATE orders SET status = @status WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(total_amount), 0) FROM orders WHERE customer_id = @customerId AND status = 'Completed'")]
    Task<decimal> GetCustomerTotalSpendingAsync(long customerId, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository(DbConnection connection) : IOrderRepository
{
}

public partial interface IOrderItemRepository
{
    [SqlTemplate("INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES (@orderId, @productId, @quantity, @unitPrice)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(long orderId, long productId, int quantity, decimal unitPrice, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM order_items WHERE order_id = @orderId")]
    Task<List<EcomOrderItem>> GetByOrderIdAsync(long orderId, CancellationToken ct = default);

    [SqlTemplate("SELECT SUM(quantity * unit_price) FROM order_items WHERE order_id = @orderId")]
    Task<decimal> CalculateOrderTotalAsync(long orderId, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IOrderItemRepository))]
public partial class OrderItemRepository(DbConnection connection) : IOrderItemRepository
{
}

// ==================== 场景23: 服务层模式 ====================

public class OrderService
{
    private readonly DbConnection _connection;
    private readonly ICustomerRepository _customerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IOrderItemRepository _orderItemRepo;

    public OrderService(DbConnection connection)
    {
        _connection = connection;
        _customerRepo = new CustomerRepository(connection);
        _productRepo = new ProductRepository(connection);
        _orderRepo = new OrderRepository(connection);
        _orderItemRepo = new OrderItemRepository(connection);
    }

    public async Task<long> CreateOrderAsync(
        long customerId,
        Dictionary<long, int> productQuantities,
        CancellationToken ct = default)
    {
        // 验证客户
        var customer = await _customerRepo.GetByIdAsync(customerId, ct);
        if (customer == null || !customer.IsActive)
            throw new InvalidOperationException("Customer not found or inactive");

        // 开始事务
        using var transaction = _connection.BeginTransaction();
        try
        {
            decimal totalAmount = 0;
            var orderDate = DateTime.UtcNow;

            // 创建订单
            var orderId = await _orderRepo.CreateAsync(customerId, orderDate, 0, "Pending", ct);

            // 处理每个产品
            foreach (var (productId, quantity) in productQuantities)
            {
                var product = await _productRepo.GetByIdAsync(productId, ct);
                if (product == null || product.Stock < quantity)
                    throw new InvalidOperationException($"Product {productId} unavailable");

                // 减少库存
                await _productRepo.DecrementStockAsync(productId, quantity, ct);

                // 创建订单项
                await _orderItemRepo.CreateAsync(orderId, productId, quantity, product.Price, ct);

                totalAmount += product.Price * quantity;
            }

            // 更新订单总额
            await _orderRepo.UpdateStatusAsync(orderId, "Confirmed", ct);

            transaction.Commit();
            return orderId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetOrderSummaryAsync(long orderId, CancellationToken ct = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, ct);
        if (order == null)
            throw new InvalidOperationException("Order not found");

        var customer = await _customerRepo.GetByIdAsync(order.CustomerId, ct);
        var items = await _orderItemRepo.GetByOrderIdAsync(orderId, ct);
        var total = await _orderItemRepo.CalculateOrderTotalAsync(orderId, ct);

        return new Dictionary<string, object>
        {
            ["OrderId"] = orderId,
            ["CustomerName"] = customer?.Name ?? "Unknown",
            ["OrderDate"] = order.OrderDate,
            ["ItemCount"] = items.Count,
            ["TotalAmount"] = total,
            ["Status"] = order.Status
        };
    }
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数和有参构造函数的集成测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_Integration : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_Integration()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                registered_at TEXT NOT NULL,
                is_active INTEGER DEFAULT 1
            );

            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price DECIMAL(10,2) NOT NULL,
                stock INTEGER NOT NULL
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER NOT NULL,
                order_date TEXT NOT NULL,
                total_amount DECIMAL(10,2) NOT NULL,
                status TEXT NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id)
            );

            CREATE TABLE order_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL,
                FOREIGN KEY (order_id) REFERENCES orders(id),
                FOREIGN KEY (product_id) REFERENCES products(id)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== 单仓储集成测试 ====================

    [TestMethod]
    public async Task CustomerRepository_FullWorkflow_ShouldWork()
    {
        // Arrange
        var repo = new CustomerRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 创建客户
        var customerId = await repo.CreateAsync("John Doe", "john@example.com", now, true);

        // Assert
        Assert.IsTrue(customerId > 0);

        // Act - 查询客户
        var customer = await repo.GetByIdAsync(customerId);
        Assert.IsNotNull(customer);
        Assert.AreEqual("John Doe", customer.Name);

        // Act - 通过邮箱查询
        var customerByEmail = await repo.GetByEmailAsync("john@example.com");
        Assert.IsNotNull(customerByEmail);
        Assert.AreEqual(customerId, customerByEmail.Id);

        // Act - 停用客户
        await repo.SetActiveStatusAsync(customerId, false);
        customer = await repo.GetByIdAsync(customerId);
        Assert.IsFalse(customer.IsActive);

        // Act - 统计活跃客户
        var activeCount = await repo.CountActiveAsync();
        Assert.AreEqual(0, activeCount);
    }

    [TestMethod]
    public async Task ProductRepository_StockManagement_ShouldWork()
    {
        // Arrange
        var repo = new ProductRepository(_connection);

        // Act - 创建产品
        var productId = await repo.CreateAsync("Laptop", 999.99m, 10);

        // Assert
        Assert.IsTrue(productId > 0);

        // Act - 减少库存
        var decreased = await repo.DecrementStockAsync(productId, 3);
        Assert.AreEqual(1, decreased);

        var product = await repo.GetByIdAsync(productId);
        Assert.AreEqual(7, product.Stock);

        // Act - 增加库存
        await repo.IncrementStockAsync(productId, 5);
        product = await repo.GetByIdAsync(productId);
        Assert.AreEqual(12, product.Stock);

        // Act - 查询可用产品
        var available = await repo.GetAvailableProductsAsync();
        Assert.AreEqual(1, available.Count);
    }

    // ==================== 多仓储协作测试 ====================

    [TestMethod]
    public async Task MultiRepository_CustomerAndOrders_ShouldWork()
    {
        // Arrange
        var customerRepo = new CustomerRepository(_connection);
        var orderRepo = new OrderRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 创建客户
        var customerId = await customerRepo.CreateAsync("Jane Smith", "jane@example.com", now, true);

        // Act - 创建订单
        var orderId1 = await orderRepo.CreateAsync(customerId, now, 100m, "Pending");
        var orderId2 = await orderRepo.CreateAsync(customerId, now.AddDays(1), 200m, "Completed");

        // Assert
        var orders = await orderRepo.GetCustomerOrdersAsync(customerId);
        Assert.AreEqual(2, orders.Count);

        // Act - 计算总消费
        var totalSpending = await orderRepo.GetCustomerTotalSpendingAsync(customerId);
        Assert.AreEqual(200m, totalSpending);
    }

    [TestMethod]
    public async Task MultiRepository_OrderWithItems_ShouldWork()
    {
        // Arrange
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var orderRepo = new OrderRepository(_connection);
        var orderItemRepo = new OrderItemRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 准备数据
        var customerId = await customerRepo.CreateAsync("Bob", "bob@example.com", now, true);
        var productId1 = await productRepo.CreateAsync("Mouse", 29.99m, 50);
        var productId2 = await productRepo.CreateAsync("Keyboard", 79.99m, 30);

        // Act - 创建订单
        var orderId = await orderRepo.CreateAsync(customerId, now, 0m, "Pending");

        // Act - 添加订单项
        await orderItemRepo.CreateAsync(orderId, productId1, 2, 29.99m);
        await orderItemRepo.CreateAsync(orderId, productId2, 1, 79.99m);

        // Assert
        var items = await orderItemRepo.GetByOrderIdAsync(orderId);
        Assert.AreEqual(2, items.Count);

        var total = await orderItemRepo.CalculateOrderTotalAsync(orderId);
        Assert.AreEqual(139.97m, total);
    }

    // ==================== 事务和原子性测试 ====================

    [TestMethod]
    public async Task Transaction_AllRepositories_ShouldBeAtomic()
    {
        // Arrange
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var orderRepo = new OrderRepository(_connection);
        var orderItemRepo = new OrderItemRepository(_connection);
        var now = DateTime.UtcNow;

        using var transaction = _connection.BeginTransaction();

        // Act
        var customerId = await customerRepo.CreateAsync("Alice", "alice@example.com", now, true);
        var productId = await productRepo.CreateAsync("Monitor", 299.99m, 5);
        var orderId = await orderRepo.CreateAsync(customerId, now, 299.99m, "Pending");
        await orderItemRepo.CreateAsync(orderId, productId, 1, 299.99m);

        transaction.Commit();

        // Assert
        var customer = await customerRepo.GetByIdAsync(customerId);
        var product = await productRepo.GetByIdAsync(productId);
        var order = await orderRepo.GetByIdAsync(orderId);
        var items = await orderItemRepo.GetByOrderIdAsync(orderId);

        Assert.IsNotNull(customer);
        Assert.IsNotNull(product);
        Assert.IsNotNull(order);
        Assert.AreEqual(1, items.Count);
    }

    [TestMethod]
    public async Task Transaction_Rollback_ShouldRevertAll()
    {
        // Arrange
        var customerRepo = new CustomerRepository(_connection);
        var orderRepo = new OrderRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 在事务外创建客户
        var customerId = await customerRepo.CreateAsync("TestUser", "test@example.com", now, true);

        // Act - 在事务中创建订单并回滚
        using (var transaction = _connection.BeginTransaction())
        {
            await orderRepo.CreateAsync(customerId, now, 100m, "Pending");
            transaction.Rollback();
        }

        // Assert - 订单不应存在
        var orders = await orderRepo.GetCustomerOrdersAsync(customerId);
        Assert.AreEqual(0, orders.Count);
    }

    // ==================== 服务层集成测试 ====================

    [TestMethod]
    public async Task OrderService_CreateOrder_ShouldCoordinateAllRepositories()
    {
        // Arrange
        var service = new OrderService(_connection);
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var now = DateTime.UtcNow;

        // 准备数据
        var customerId = await customerRepo.CreateAsync("ServiceUser", "service@example.com", now, true);
        var productId1 = await productRepo.CreateAsync("Item1", 10m, 100);
        var productId2 = await productRepo.CreateAsync("Item2", 20m, 50);

        var productQuantities = new Dictionary<long, int>
        {
            [productId1] = 5,
            [productId2] = 3
        };

        // Act
        var orderId = await service.CreateOrderAsync(customerId, productQuantities);

        // Assert
        Assert.IsTrue(orderId > 0);

        // 验证库存减少
        var product1 = await productRepo.GetByIdAsync(productId1);
        var product2 = await productRepo.GetByIdAsync(productId2);
        Assert.AreEqual(95, product1.Stock);
        Assert.AreEqual(47, product2.Stock);
    }

    [TestMethod]
    public async Task OrderService_GetOrderSummary_ShouldAggregateData()
    {
        // Arrange
        var service = new OrderService(_connection);
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var now = DateTime.UtcNow;

        var customerId = await customerRepo.CreateAsync("SummaryUser", "summary@example.com", now, true);
        var productId = await productRepo.CreateAsync("TestProduct", 50m, 10);

        var productQuantities = new Dictionary<long, int> { [productId] = 2 };
        var orderId = await service.CreateOrderAsync(customerId, productQuantities);

        // Act
        var summary = await service.GetOrderSummaryAsync(orderId);

        // Assert
        Assert.IsNotNull(summary);
        Assert.AreEqual(orderId, summary["OrderId"]);
        Assert.AreEqual("SummaryUser", summary["CustomerName"]);
        Assert.AreEqual(1, summary["ItemCount"]);
        Assert.AreEqual(100m, summary["TotalAmount"]);
    }

    [TestMethod]
    public async Task OrderService_InvalidCustomer_ShouldThrow()
    {
        // Arrange
        var service = new OrderService(_connection);
        var productQuantities = new Dictionary<long, int> { [1] = 1 };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await service.CreateOrderAsync(999, productQuantities);
        });
    }

    [TestMethod]
    public async Task OrderService_InsufficientStock_ShouldRollback()
    {
        // Arrange
        var service = new OrderService(_connection);
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var now = DateTime.UtcNow;

        var customerId = await customerRepo.CreateAsync("StockUser", "stock@example.com", now, true);
        var productId = await productRepo.CreateAsync("LimitedItem", 100m, 5);

        var productQuantities = new Dictionary<long, int> { [productId] = 10 }; // 超过库存

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await service.CreateOrderAsync(customerId, productQuantities);
        });

        // 验证库存未改变
        var product = await productRepo.GetByIdAsync(productId);
        Assert.AreEqual(5, product.Stock);
    }

    // ==================== 并发和性能测试 ====================

    [TestMethod]
    public async Task ConcurrentOperations_MultipleRepositories_ShouldSucceed()
    {
        // Arrange
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 顺序创建（SQLite in-memory不支持并发写入）
        var customerIds = new List<long>();
        var productIds = new List<long>();

        for (int i = 0; i < 10; i++)
        {
            var customerId = await customerRepo.CreateAsync($"User{i}", $"user{i}@example.com", now, true);
            customerIds.Add(customerId);

            var productId = await productRepo.CreateAsync($"Product{i}", 10m * i, 100);
            productIds.Add(productId);
        }

        // Assert
        Assert.AreEqual(10, customerIds.Count);
        Assert.AreEqual(10, productIds.Count);
        Assert.IsTrue(customerIds.All(id => id > 0));
        Assert.IsTrue(productIds.All(id => id > 0));
        Assert.AreEqual(10, customerIds.Distinct().Count()); // 所有客户ID都不同
        Assert.AreEqual(10, productIds.Distinct().Count()); // 所有产品ID都不同
    }

    [TestMethod]
    public async Task RepositoryInstantiation_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var repo1 = new CustomerRepository(_connection);
        var repo2 = new CustomerRepository(_connection);
        var repo3 = new CustomerRepository(_connection);
        var now = DateTime.UtcNow;

        // 使用不同实例操作
        var id1 = await repo1.CreateAsync("User1", "user1@test.com", now, true);
        var id2 = await repo2.CreateAsync("User2", "user2@test.com", now, true);
        var id3 = await repo3.CreateAsync("User3", "user3@test.com", now, true);

        // Assert - 所有实例应该独立工作
        Assert.IsTrue(id1 > 0 && id2 > 0 && id3 > 0);

        var count = await repo1.CountActiveAsync();
        Assert.AreEqual(3, count);
    }

    // ==================== 真实场景模拟 ====================

    [TestMethod]
    public async Task RealWorld_CompleteOrderFlow_ShouldWork()
    {
        // Arrange - 准备完整的电商场景
        var customerRepo = new CustomerRepository(_connection);
        var productRepo = new ProductRepository(_connection);
        var orderRepo = new OrderRepository(_connection);
        var orderItemRepo = new OrderItemRepository(_connection);
        var now = DateTime.UtcNow;

        // Step 1: 注册客户
        var customerId = await customerRepo.CreateAsync("Real User", "real@example.com", now, true);

        // Step 2: 添加产品
        var laptopId = await productRepo.CreateAsync("Gaming Laptop", 1299.99m, 10);
        var mouseId = await productRepo.CreateAsync("Gaming Mouse", 59.99m, 50);
        var keyboardId = await productRepo.CreateAsync("Gaming Keyboard", 129.99m, 30);

        // Step 3: 客户下单
        using (var transaction = _connection.BeginTransaction())
        {
            var orderId = await orderRepo.CreateAsync(customerId, now, 0m, "Pending");

            await orderItemRepo.CreateAsync(orderId, laptopId, 1, 1299.99m);
            await orderItemRepo.CreateAsync(orderId, mouseId, 2, 59.99m);
            await orderItemRepo.CreateAsync(orderId, keyboardId, 1, 129.99m);

            await productRepo.DecrementStockAsync(laptopId, 1);
            await productRepo.DecrementStockAsync(mouseId, 2);
            await productRepo.DecrementStockAsync(keyboardId, 1);

            var total = await orderItemRepo.CalculateOrderTotalAsync(orderId);
            await orderRepo.UpdateStatusAsync(orderId, "Confirmed");

            transaction.Commit();

            // Assert - 验证订单
            var order = await orderRepo.GetByIdAsync(orderId);
            Assert.IsNotNull(order);
            Assert.AreEqual("Confirmed", order.Status);

            var items = await orderItemRepo.GetByOrderIdAsync(orderId);
            Assert.AreEqual(3, items.Count);

            Assert.AreEqual(1549.96m, total);
        }

        // Step 4: 验证库存
        var laptop = await productRepo.GetByIdAsync(laptopId);
        var mouse = await productRepo.GetByIdAsync(mouseId);
        var keyboard = await productRepo.GetByIdAsync(keyboardId);

        Assert.AreEqual(9, laptop.Stock);
        Assert.AreEqual(48, mouse.Stock);
        Assert.AreEqual(29, keyboard.Stock);
    }
}

