// -----------------------------------------------------------------------
// <copyright file="ECommerceScenarios.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// E2E tests for e-commerce scenarios.
/// **Validates: Requirements 2.2**
/// </summary>
public class EComProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EComCart
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EComCartItem
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtAdd { get; set; }
}

public class EComOrder
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
}

public partial interface IEComProductRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = 1")]
    Task<List<EComProduct>> GetActiveProductsAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = 1 AND category = @category")]
    Task<List<EComProduct>> GetProductsByCategoryAsync(string category);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<EComProduct?> GetByIdAsync(long id);

    [SqlTemplate("UPDATE {{table}} SET stock = @stock WHERE id = @id")]
    Task<int> UpdateStockAsync(long id, int stock);
}

public partial interface IEComCartRepository
{
    [SqlTemplate("INSERT INTO ecom_carts (user_id, created_at) VALUES (@userId, @createdAt)")]
    Task<int> CreateCartAsync(long userId, DateTime createdAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId ORDER BY id DESC LIMIT 1")]
    Task<EComCart?> GetActiveCartByUserIdAsync(long userId);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @cartId")]
    Task<int> DeleteCartAsync(long cartId);
}

public partial interface IEComCartItemRepository
{
    [SqlTemplate("INSERT INTO ecom_cart_items (cart_id, product_id, quantity, price_at_add) VALUES (@cartId, @productId, @quantity, @priceAtAdd)")]
    Task<int> AddItemAsync(long cartId, long productId, int quantity, decimal priceAtAdd);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE cart_id = @cartId")]
    Task<List<EComCartItem>> GetCartItemsAsync(long cartId);

    [SqlTemplate("UPDATE {{table}} SET quantity = @quantity WHERE id = @id")]
    Task<int> UpdateQuantityAsync(long id, int quantity);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> RemoveItemAsync(long id);

    [SqlTemplate("DELETE FROM {{table}} WHERE cart_id = @cartId")]
    Task<int> ClearCartAsync(long cartId);
}

public partial interface IEComOrderRepository
{
    [SqlTemplate("INSERT INTO ecom_orders (user_id, order_number, total_amount, status, order_date) VALUES (@userId, @orderNumber, @totalAmount, @status, @orderDate)")]
    Task<int> CreateOrderAsync(long userId, string orderNumber, decimal totalAmount, string status, DateTime orderDate);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId ORDER BY order_date DESC")]
    Task<List<EComOrder>> GetOrdersByUserIdAsync(long userId);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId AND status = @status ORDER BY order_date DESC")]
    Task<List<EComOrder>> GetOrdersByUserIdAndStatusAsync(long userId, string status);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("ecom_products")]
[RepositoryFor(typeof(IEComProductRepository))]
public partial class EComProductRepository(DbConnection connection) : IEComProductRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("ecom_carts")]
[RepositoryFor(typeof(IEComCartRepository))]
public partial class EComCartRepository(DbConnection connection) : IEComCartRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("ecom_cart_items")]
[RepositoryFor(typeof(IEComCartItemRepository))]
public partial class EComCartItemRepository(DbConnection connection) : IEComCartItemRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("ecom_orders")]
[RepositoryFor(typeof(IEComOrderRepository))]
public partial class EComOrderRepository(DbConnection connection) : IEComOrderRepository { }

/// <summary>
/// SQLite-specific e-commerce E2E tests.
/// </summary>
[TestClass]
public class ECommerceScenarios_SQLite
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
        _fixture.SeedECommerceData(SqlDefineTypes.SQLite);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    public async Task ProductCatalogBrowsing_ShouldFilterByCategory()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComProductRepository(connection);

        // Act
        var allProducts = await repo.GetActiveProductsAsync();
        var electronicsProducts = await repo.GetProductsByCategoryAsync("Electronics");

        // Assert
        Assert.IsTrue(allProducts.Count >= 2, "Should have at least 2 products");
        Assert.IsTrue(electronicsProducts.Count >= 1, "Should have at least 1 electronics product");
        Assert.IsTrue(electronicsProducts.All(p => p.Category == "Electronics"), "All products should be in Electronics category");
    }

    [TestMethod]
    public async Task ShoppingCartOperations_ShouldManageItems()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var cartRepo = new EComCartRepository(connection);
        var cartItemRepo = new EComCartItemRepository(connection);
        var productRepo = new EComProductRepository(connection);

        var userId = 1L;
        var product = await productRepo.GetByIdAsync(1);
        Assert.IsNotNull(product, "Product should exist");

        // Act - Create cart and add item
        await cartRepo.CreateCartAsync(userId, DateTime.UtcNow);
        var cart = await cartRepo.GetActiveCartByUserIdAsync(userId);
        Assert.IsNotNull(cart, "Cart should be created");

        await cartItemRepo.AddItemAsync(cart.Id, product.Id, 2, product.Price);
        var items = await cartItemRepo.GetCartItemsAsync(cart.Id);

        // Assert - Item added
        Assert.AreEqual(1, items.Count, "Should have 1 item in cart");
        Assert.AreEqual(2, items[0].Quantity, "Quantity should be 2");

        // Act - Update quantity
        await cartItemRepo.UpdateQuantityAsync(items[0].Id, 5);
        items = await cartItemRepo.GetCartItemsAsync(cart.Id);

        // Assert - Quantity updated
        Assert.AreEqual(5, items[0].Quantity, "Quantity should be updated to 5");

        // Act - Remove item
        await cartItemRepo.RemoveItemAsync(items[0].Id);
        items = await cartItemRepo.GetCartItemsAsync(cart.Id);

        // Assert - Item removed
        Assert.AreEqual(0, items.Count, "Cart should be empty");
    }

    [TestMethod]
    public async Task OrderCheckout_ShouldCreateOrderAndClearCart()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var cartRepo = new EComCartRepository(connection);
        var cartItemRepo = new EComCartItemRepository(connection);
        var orderRepo = new EComOrderRepository(connection);
        var productRepo = new EComProductRepository(connection);

        var userId = 1L;
        var product = await productRepo.GetByIdAsync(1);
        Assert.IsNotNull(product, "Product should exist");

        // Create cart with items
        await cartRepo.CreateCartAsync(userId, DateTime.UtcNow);
        var cart = await cartRepo.GetActiveCartByUserIdAsync(userId);
        Assert.IsNotNull(cart, "Cart should be created");

        await cartItemRepo.AddItemAsync(cart.Id, product.Id, 2, product.Price);
        var items = await cartItemRepo.GetCartItemsAsync(cart.Id);
        var totalAmount = items.Sum(i => i.Quantity * i.PriceAtAdd);

        // Act - Checkout
        var orderNumber = $"ORD-{DateTime.UtcNow.Ticks}";
        await orderRepo.CreateOrderAsync(userId, orderNumber, totalAmount, "pending", DateTime.UtcNow);
        await cartItemRepo.ClearCartAsync(cart.Id);

        // Assert - Order created and cart cleared
        var orders = await orderRepo.GetOrdersByUserIdAsync(userId);
        Assert.IsTrue(orders.Any(o => o.OrderNumber == orderNumber), "Order should be created");
        
        var clearedItems = await cartItemRepo.GetCartItemsAsync(cart.Id);
        Assert.AreEqual(0, clearedItems.Count, "Cart should be cleared after checkout");
    }

    [TestMethod]
    public async Task OrderHistory_ShouldFilterByStatus()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var orderRepo = new EComOrderRepository(connection);
        var userId = 1L;

        // Create orders with different statuses
        await orderRepo.CreateOrderAsync(userId, "ORD-001", 100.00m, "completed", DateTime.UtcNow);
        await orderRepo.CreateOrderAsync(userId, "ORD-002", 200.00m, "pending", DateTime.UtcNow);
        await orderRepo.CreateOrderAsync(userId, "ORD-003", 150.00m, "completed", DateTime.UtcNow);

        // Act
        var allOrders = await orderRepo.GetOrdersByUserIdAsync(userId);
        var completedOrders = await orderRepo.GetOrdersByUserIdAndStatusAsync(userId, "completed");
        var pendingOrders = await orderRepo.GetOrdersByUserIdAndStatusAsync(userId, "pending");

        // Assert
        Assert.IsTrue(allOrders.Count >= 3, "Should have at least 3 orders");
        Assert.IsTrue(completedOrders.Count >= 2, "Should have at least 2 completed orders");
        Assert.IsTrue(pendingOrders.Count >= 1, "Should have at least 1 pending order");
        Assert.IsTrue(completedOrders.All(o => o.Status == "completed"), "All orders should be completed");
        Assert.IsTrue(pendingOrders.All(o => o.Status == "pending"), "All orders should be pending");
    }
}
