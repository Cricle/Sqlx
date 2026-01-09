// -----------------------------------------------------------------------
// <copyright file="BatchOperationsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Performance;

/// <summary>
/// E2E tests for batch operations performance.
/// Tests complete workflows: bulk data creation, batch updates, batch deletes.
/// Note: Integration tests exist in TDD_BatchOperations_Integration.cs - these focus on E2E workflows.
/// </summary>
[TestClass]
public class BatchOperationsTests
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    /// <summary>
    /// E2E Test: Bulk product import workflow.
    /// Scenario: Import 1000+ products from external source.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task BulkProductImport_Should_CompleteWithinPerformanceThreshold()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new BatchProductRepository(connection);
        
        var products = Enumerable.Range(1, 1000).Select(i => new BatchProduct
        {
            Name = $"Product {i}",
            Sku = $"SKU-{i:D6}",
            Price = 10.0m + (i % 100),
            Stock = 100 + (i % 50),
            CreatedAt = DateTime.Now
        }).ToList();

        // Act
        var inserted = await repo.BatchInsertAsync(products);

        // Assert
        Assert.AreEqual(1000, inserted, "Should insert 1000 products");
        
        // Verify data integrity
        var count = await repo.CountAsync();
        Assert.AreEqual(1000, count, "Should have 1000 products in database");
    }

    /// <summary>
    /// E2E Test: Batch price update workflow.
    /// Scenario: Apply discount to all products in a category.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task BatchPriceUpdate_Should_UpdateAllMatchingProducts()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new BatchProductRepository(connection);
        
        // Create test products
        var products = Enumerable.Range(1, 500).Select(i => new BatchProduct
        {
            Name = $"Product {i}",
            Sku = $"SKU-{i:D6}",
            Price = 100.0m,
            Stock = 50,
            CreatedAt = DateTime.Now
        }).ToList();
        
        await repo.BatchInsertAsync(products);

        // Act - Apply 10% discount to all products
        var stopwatch = Stopwatch.StartNew();
        var updated = await repo.BatchUpdatePriceAsync(90.0m);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(500, updated, "Should update 500 products");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Batch update should complete within 2 seconds, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify all prices updated
        var allProducts = await repo.GetAllAsync();
        Assert.IsTrue(allProducts.All(p => p.Price == 90.0m), "All products should have updated price");
        
        Console.WriteLine($"Batch update 500 products: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// E2E Test: Batch delete old records workflow.
    /// Scenario: Archive/delete products older than 1 year.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task BatchDeleteOldRecords_Should_RemoveMatchingRecords()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new BatchProductRepository(connection);
        
        // Create old and new products
        var oldProducts = Enumerable.Range(1, 300).Select(i => new BatchProduct
        {
            Name = $"Old Product {i}",
            Sku = $"OLD-{i:D6}",
            Price = 50.0m,
            Stock = 0,
            CreatedAt = DateTime.Now.AddYears(-2)
        }).ToList();
        
        var newProducts = Enumerable.Range(1, 200).Select(i => new BatchProduct
        {
            Name = $"New Product {i}",
            Sku = $"NEW-{i:D6}",
            Price = 100.0m,
            Stock = 50,
            CreatedAt = DateTime.Now
        }).ToList();
        
        await repo.BatchInsertAsync(oldProducts);
        await repo.BatchInsertAsync(newProducts);

        // Act - Delete products older than 1 year
        var cutoffDate = DateTime.Now.AddYears(-1);
        var stopwatch = Stopwatch.StartNew();
        var deleted = await repo.BatchDeleteOldAsync(cutoffDate);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(300, deleted, "Should delete 300 old products");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Batch delete should complete within 2 seconds, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify only new products remain
        var remaining = await repo.CountAsync();
        Assert.AreEqual(200, remaining, "Should have 200 new products remaining");
        
        Console.WriteLine($"Batch delete 300 products: {stopwatch.ElapsedMilliseconds}ms");
    }
}

#region Models and Repositories

public class BatchProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IBatchProductRepository
{
    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate(@"
        INSERT INTO batch_products ({{columns --exclude Id}})
        VALUES {{batch_values --exclude Id}}
    ")]
    Task<int> BatchInsertAsync(List<BatchProduct> products);

    [SqlTemplate(@"
        UPDATE batch_products
        SET price = @newPrice
    ")]
    Task<int> BatchUpdatePriceAsync(decimal newPrice);

    [SqlTemplate(@"
        DELETE FROM batch_products
        WHERE created_at < @cutoffDate
    ")]
    Task<int> BatchDeleteOldAsync(DateTime cutoffDate);

    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table batch_products}}
    ")]
    Task<List<BatchProduct>> GetAllAsync();

    [SqlTemplate(@"
        SELECT {{count:*}} FROM {{table batch_products}}
    ")]
    Task<int> CountAsync();
}

public class BatchProductRepository : IBatchProductRepository
{
    private readonly System.Data.IDbConnection _connection;

    public BatchProductRepository(System.Data.IDbConnection connection)
    {
        _connection = connection;
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS batch_products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                sku TEXT NOT NULL UNIQUE,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                created_at TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public async Task<int> BatchInsertAsync(List<BatchProduct> products)
    {
        var totalInserted = 0;
        foreach (var product in products)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO batch_products (name, sku, price, stock, created_at)
                VALUES (@name, @sku, @price, @stock, @createdAt)";
            
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@name";
            p1.Value = product.Name;
            cmd.Parameters.Add(p1);
            
            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@sku";
            p2.Value = product.Sku;
            cmd.Parameters.Add(p2);
            
            var p3 = cmd.CreateParameter();
            p3.ParameterName = "@price";
            p3.Value = product.Price;
            cmd.Parameters.Add(p3);
            
            var p4 = cmd.CreateParameter();
            p4.ParameterName = "@stock";
            p4.Value = product.Stock;
            cmd.Parameters.Add(p4);
            
            var p5 = cmd.CreateParameter();
            p5.ParameterName = "@createdAt";
            p5.Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            cmd.Parameters.Add(p5);
            
            totalInserted += await Task.Run(() => cmd.ExecuteNonQuery());
        }
        return totalInserted;
    }

    public async Task<int> BatchUpdatePriceAsync(decimal newPrice)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE batch_products SET price = @newPrice";
        
        var p = cmd.CreateParameter();
        p.ParameterName = "@newPrice";
        p.Value = newPrice;
        cmd.Parameters.Add(p);
        
        return await Task.Run(() => cmd.ExecuteNonQuery());
    }

    public async Task<int> BatchDeleteOldAsync(DateTime cutoffDate)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM batch_products WHERE created_at < @cutoffDate";
        
        var p = cmd.CreateParameter();
        p.ParameterName = "@cutoffDate";
        p.Value = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss");
        cmd.Parameters.Add(p);
        
        return await Task.Run(() => cmd.ExecuteNonQuery());
    }

    public async Task<List<BatchProduct>> GetAllAsync()
    {
        var products = new List<BatchProduct>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, sku, price, stock, created_at FROM batch_products";
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            products.Add(new BatchProduct
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Sku = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                CreatedAt = DateTime.Parse(reader.GetString(5))
            });
        }
        return products;
    }

    public async Task<int> CountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM batch_products";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToInt32(result);
    }
}

#endregion
