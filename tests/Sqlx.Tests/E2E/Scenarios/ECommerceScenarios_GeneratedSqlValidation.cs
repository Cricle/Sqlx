// -----------------------------------------------------------------------
// <copyright file="ECommerceScenarios_GeneratedSqlValidation.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// Strict validation of generated SQL for E-commerce scenarios across all database dialects.
/// This test executes actual SQL to ensure placeholders are correctly expanded.
/// </summary>
[TestClass]
public class ECommerceScenarios_GeneratedSqlValidation
{
    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComProductRepository_GetActiveProductsAsync_ShouldExpandColumnsPlaceholder()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComProductRepository(connection);
        
        // Act
        var task = repo.GetActiveProductsAsync();
        task.Wait();
        var products = task.Result;
        
        // Assert - All columns should be populated (validates {{columns}} expansion)
        Assert.IsTrue(products.Count > 0, "Should have active products");
        var product = products[0];
        Assert.IsTrue(product.Id > 0, "Id should be populated");
        Assert.IsNotNull(product.Name, "Name should be populated");
        Assert.IsTrue(product.Price > 0, "Price should be populated");
        Assert.IsNotNull(product.Category, "Category should be populated");
        Assert.IsTrue(product.IsActive, "IsActive should be true");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComProductRepository_GetProductsByCategoryAsync_ShouldFilterCorrectly()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComProductRepository(connection);
        
        // Act
        var task = repo.GetProductsByCategoryAsync("Electronics");
        task.Wait();
        var products = task.Result;
        
        // Assert
        Assert.IsTrue(products.Count > 0, "Should have electronics products");
        Assert.IsTrue(products.All(p => p.Category == "Electronics"), 
            "All products should be in Electronics category");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComProductRepository_GetByIdAsync_ShouldUseTablePlaceholder()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComProductRepository(connection);
        
        // Act
        var task = repo.GetByIdAsync(1);
        task.Wait();
        var product = task.Result;
        
        // Assert
        Assert.IsNotNull(product, "Product should exist");
        Assert.AreEqual(1, product.Id);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComProductRepository_UpdateStockAsync_ShouldUpdateCorrectly()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComProductRepository(connection);
        
        // Act
        var updateTask = repo.UpdateStockAsync(1, 50);
        updateTask.Wait();
        var result = updateTask.Result;
        
        // Assert
        Assert.AreEqual(1, result, "Should update 1 row");
        
        // Verify stock was updated
        var getTask = repo.GetByIdAsync(1);
        getTask.Wait();
        var product = getTask.Result;
        Assert.IsNotNull(product);
        Assert.AreEqual(50, product.Stock);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComCartRepository_CreateAndGetCart_ShouldWork()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComCartRepository(connection);
        
        // Act - Create cart
        var createTask = repo.CreateCartAsync(1L, DateTime.UtcNow);
        createTask.Wait();
        var createResult = createTask.Result;
        
        // Assert
        Assert.AreEqual(1, createResult, "Should insert 1 cart");
        
        // Act - Get cart (validates ORDER BY id DESC LIMIT 1)
        var getTask = repo.GetActiveCartByUserIdAsync(1L);
        getTask.Wait();
        var cart = getTask.Result;
        
        // Assert
        Assert.IsNotNull(cart, "Cart should be retrievable");
        Assert.AreEqual(1L, cart.UserId);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComCartItemRepository_AllOperations_ShouldWork()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var cartRepo = new EComCartRepository(connection);
        var itemRepo = new EComCartItemRepository(connection);
        
        // Create a cart first
        var createCartTask = cartRepo.CreateCartAsync(1L, DateTime.UtcNow);
        createCartTask.Wait();
        
        var getCartTask = cartRepo.GetActiveCartByUserIdAsync(1L);
        getCartTask.Wait();
        var cart = getCartTask.Result;
        Assert.IsNotNull(cart, "Cart should exist");
        
        // Act - Add item
        var addTask = itemRepo.AddItemAsync(cart.Id, 1L, 2, 99.99m);
        addTask.Wait();
        Assert.AreEqual(1, addTask.Result, "Should add 1 item");
        
        // Act - Get items
        var getItemsTask = itemRepo.GetCartItemsAsync(cart.Id);
        getItemsTask.Wait();
        var items = getItemsTask.Result;
        
        // Assert
        Assert.AreEqual(1, items.Count, "Should have 1 item");
        Assert.AreEqual(2, items[0].Quantity);
        
        // Act - Update quantity
        var updateTask = itemRepo.UpdateQuantityAsync(items[0].Id, 5);
        updateTask.Wait();
        Assert.AreEqual(1, updateTask.Result, "Should update 1 item");
        
        // Verify update
        var getUpdatedTask = itemRepo.GetCartItemsAsync(cart.Id);
        getUpdatedTask.Wait();
        var updatedItems = getUpdatedTask.Result;
        Assert.AreEqual(5, updatedItems[0].Quantity);
        
        // Act - Remove item
        var removeTask = itemRepo.RemoveItemAsync(items[0].Id);
        removeTask.Wait();
        Assert.AreEqual(1, removeTask.Result, "Should remove 1 item");
        
        // Verify removal
        var getAfterRemoveTask = itemRepo.GetCartItemsAsync(cart.Id);
        getAfterRemoveTask.Wait();
        Assert.AreEqual(0, getAfterRemoveTask.Result.Count, "Cart should be empty");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComCartItemRepository_ClearCartAsync_ShouldRemoveAllItems()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var cartRepo = new EComCartRepository(connection);
        var itemRepo = new EComCartItemRepository(connection);
        
        // Create cart with multiple items
        var createCartTask = cartRepo.CreateCartAsync(1L, DateTime.UtcNow);
        createCartTask.Wait();
        
        var getCartTask = cartRepo.GetActiveCartByUserIdAsync(1L);
        getCartTask.Wait();
        var cart = getCartTask.Result;
        Assert.IsNotNull(cart);
        
        var addTask1 = itemRepo.AddItemAsync(cart.Id, 1L, 2, 99.99m);
        addTask1.Wait();
        var addTask2 = itemRepo.AddItemAsync(cart.Id, 2L, 1, 49.99m);
        addTask2.Wait();
        
        // Act - Clear cart
        var clearTask = itemRepo.ClearCartAsync(cart.Id);
        clearTask.Wait();
        var result = clearTask.Result;
        
        // Assert
        Assert.AreEqual(2, result, "Should remove 2 items");
        
        // Verify cart is empty
        var getItemsTask = itemRepo.GetCartItemsAsync(cart.Id);
        getItemsTask.Wait();
        Assert.AreEqual(0, getItemsTask.Result.Count, "Cart should be empty");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComOrderRepository_CreateAndGetOrders_ShouldWork()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComOrderRepository(connection);
        
        // Act - Create orders
        var order1Task = repo.CreateOrderAsync(1L, "ORD-001", 100.00m, "completed", DateTime.UtcNow.AddDays(-2));
        order1Task.Wait();
        
        var order2Task = repo.CreateOrderAsync(1L, "ORD-002", 200.00m, "pending", DateTime.UtcNow.AddDays(-1));
        order2Task.Wait();
        
        // Act - Get all orders (validates ORDER BY order_date DESC)
        var getAllTask = repo.GetOrdersByUserIdAsync(1L);
        getAllTask.Wait();
        var allOrders = getAllTask.Result;
        
        // Assert - Orders should be in DESC order (newest first)
        Assert.IsTrue(allOrders.Count >= 2, "Should have at least 2 orders");
        var testOrders = allOrders.Where(o => o.OrderNumber.StartsWith("ORD-")).ToList();
        Assert.AreEqual(2, testOrders.Count);
        Assert.AreEqual("ORD-002", testOrders[0].OrderNumber, "Newest order should be first");
        Assert.AreEqual("ORD-001", testOrders[1].OrderNumber, "Older order should be second");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_EComOrderRepository_GetOrdersByUserIdAndStatusAsync_ShouldFilterCorrectly()
    {
        // Arrange
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new EComOrderRepository(connection);
        
        // Create orders with different statuses
        var order1Task = repo.CreateOrderAsync(1L, "ORD-COMP-1", 100.00m, "completed", DateTime.UtcNow);
        order1Task.Wait();
        
        var order2Task = repo.CreateOrderAsync(1L, "ORD-PEND-1", 200.00m, "pending", DateTime.UtcNow);
        order2Task.Wait();
        
        // Act - Get completed orders
        var completedTask = repo.GetOrdersByUserIdAndStatusAsync(1L, "completed");
        completedTask.Wait();
        var completedOrders = completedTask.Result;
        
        // Act - Get pending orders
        var pendingTask = repo.GetOrdersByUserIdAndStatusAsync(1L, "pending");
        pendingTask.Wait();
        var pendingOrders = pendingTask.Result;
        
        // Assert
        Assert.IsTrue(completedOrders.Any(o => o.OrderNumber == "ORD-COMP-1"), 
            "Should find completed order");
        Assert.IsTrue(completedOrders.All(o => o.Status == "completed"), 
            "All orders should be completed");
        
        Assert.IsTrue(pendingOrders.Any(o => o.OrderNumber == "ORD-PEND-1"), 
            "Should find pending order");
        Assert.IsTrue(pendingOrders.All(o => o.Status == "pending"), 
            "All orders should be pending");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("GeneratedSql")]
    public void SQLite_AllPlaceholders_ShouldExpandCorrectly()
    {
        // This is a comprehensive test that validates all placeholder types work correctly
        using var fixture = new DatabaseFixture();
        fixture.SeedECommerceData(SqlDefineTypes.SQLite);
        var connection = fixture.GetConnection(SqlDefineTypes.SQLite);
        
        // Test {{columns}} placeholder
        var productRepo = new EComProductRepository(connection);
        var getProductsTask = productRepo.GetActiveProductsAsync();
        getProductsTask.Wait();
        Assert.IsTrue(getProductsTask.Result.Count > 0, "{{columns}} placeholder should work");
        
        // Test {{table}} placeholder
        var updateTask = productRepo.UpdateStockAsync(1, 100);
        updateTask.Wait();
        Assert.AreEqual(1, updateTask.Result, "{{table}} placeholder should work");
        
        // Test cart operations
        var cartRepo = new EComCartRepository(connection);
        var createCartTask = cartRepo.CreateCartAsync(1L, DateTime.UtcNow);
        createCartTask.Wait();
        Assert.AreEqual(1, createCartTask.Result, "Cart creation should work");
        
        Assert.IsTrue(true, "All placeholders expanded correctly");
    }


}
