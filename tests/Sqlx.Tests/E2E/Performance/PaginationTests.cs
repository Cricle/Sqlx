// -----------------------------------------------------------------------
// <copyright file="PaginationTests.cs" company="Cricle">
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
/// E2E tests for pagination performance and correctness.
/// Tests complete workflows: large dataset pagination, no duplicates, correct ordering.
/// Note: Property tests exist in PaginationPropertyTests.cs - these focus on E2E workflows.
/// </summary>
[TestClass]
public class PaginationTests
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
    /// E2E Test: Paginate through large product catalog.
    /// Scenario: Customer browses product catalog with 1000+ products.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task ProductCatalogPagination_Should_ReturnAllRecordsWithoutDuplicates()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new PaginatedProductRepository(connection);
        
        // Create 1000 products
        var products = Enumerable.Range(1, 1000).Select(i => new PaginatedProduct
        {
            Name = $"Product {i:D4}",
            Category = $"Category {i % 10}",
            Price = 10.0m + (i % 100),
            CreatedAt = DateTime.Now.AddDays(-i)
        }).ToList();
        
        await repo.BatchInsertAsync(products);

        // Act - Paginate through all products (page size 50)
        var pageSize = 50;
        var allFetchedProducts = new List<PaginatedProduct>();
        var stopwatch = Stopwatch.StartNew();
        
        for (int page = 0; page < 20; page++)
        {
            var pageProducts = await repo.GetPageAsync(pageSize, page * pageSize);
            allFetchedProducts.AddRange(pageProducts);
            
            if (pageProducts.Count < pageSize)
                break;
        }
        
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(1000, allFetchedProducts.Count, "Should fetch all 1000 products");
        
        // Verify no duplicates
        var uniqueIds = allFetchedProducts.Select(p => p.Id).Distinct().Count();
        Assert.AreEqual(1000, uniqueIds, "Should have no duplicate products");
        
        // Verify correct ordering (by id ascending)
        var orderedIds = allFetchedProducts.Select(p => p.Id).ToList();
        var expectedIds = Enumerable.Range(1, 1000).ToList();
        CollectionAssert.AreEqual(expectedIds, orderedIds, "Products should be in correct order");
        
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000, 
            $"Pagination should complete within 3 seconds, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine($"Paginated 1000 products (20 pages × 50): {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// E2E Test: Filtered pagination with search.
    /// Scenario: Customer searches for products in specific category with pagination.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task FilteredPagination_Should_ReturnCorrectSubset()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new PaginatedProductRepository(connection);
        
        // Create products in different categories
        var products = Enumerable.Range(1, 500).Select(i => new PaginatedProduct
        {
            Name = $"Product {i:D4}",
            Category = i % 5 == 0 ? "Electronics" : $"Category {i % 4}",
            Price = 10.0m + (i % 100),
            CreatedAt = DateTime.Now.AddDays(-i)
        }).ToList();
        
        await repo.BatchInsertAsync(products);

        // Act - Paginate through Electronics category (100 products, page size 25)
        var category = "Electronics";
        var pageSize = 25;
        var allElectronics = new List<PaginatedProduct>();
        
        for (int page = 0; page < 5; page++)
        {
            var pageProducts = await repo.GetPageByCategoryAsync(category, pageSize, page * pageSize);
            allElectronics.AddRange(pageProducts);
            
            if (pageProducts.Count < pageSize)
                break;
        }

        // Assert
        Assert.AreEqual(100, allElectronics.Count, "Should fetch all 100 Electronics products");
        Assert.IsTrue(allElectronics.All(p => p.Category == "Electronics"), 
            "All products should be in Electronics category");
        
        // Verify no duplicates
        var uniqueIds = allElectronics.Select(p => p.Id).Distinct().Count();
        Assert.AreEqual(100, uniqueIds, "Should have no duplicate products");
        
        Console.WriteLine($"Filtered pagination: {allElectronics.Count} products");
    }

    /// <summary>
    /// E2E Test: Pagination with sorting.
    /// Scenario: Customer sorts products by price (descending) with pagination.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task PaginationWithSorting_Should_MaintainCorrectOrder()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new PaginatedProductRepository(connection);
        
        // Create products with varying prices
        var products = Enumerable.Range(1, 200).Select(i => new PaginatedProduct
        {
            Name = $"Product {i:D4}",
            Category = $"Category {i % 5}",
            Price = (decimal)(Math.Sin(i) * 50 + 100), // Varying prices
            CreatedAt = DateTime.Now.AddDays(-i)
        }).ToList();
        
        await repo.BatchInsertAsync(products);

        // Act - Paginate with price descending (page size 20)
        var pageSize = 20;
        var allProducts = new List<PaginatedProduct>();
        
        for (int page = 0; page < 10; page++)
        {
            var pageProducts = await repo.GetPageOrderByPriceDescAsync(pageSize, page * pageSize);
            allProducts.AddRange(pageProducts);
            
            if (pageProducts.Count < pageSize)
                break;
        }

        // Assert
        Assert.AreEqual(200, allProducts.Count, "Should fetch all 200 products");
        
        // Verify descending price order
        for (int i = 0; i < allProducts.Count - 1; i++)
        {
            Assert.IsTrue(allProducts[i].Price >= allProducts[i + 1].Price, 
                $"Products should be in descending price order at index {i}");
        }
        
        Console.WriteLine($"Sorted pagination: {allProducts.Count} products in correct order");
    }

    /// <summary>
    /// E2E Test: Empty result pagination.
    /// Scenario: Customer navigates beyond available pages.
    /// </summary>
    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Performance")]
    public async Task PaginationBeyondAvailablePages_Should_ReturnEmptyList()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new PaginatedProductRepository(connection);
        
        // Create only 50 products
        var products = Enumerable.Range(1, 50).Select(i => new PaginatedProduct
        {
            Name = $"Product {i:D4}",
            Category = "Test",
            Price = 10.0m,
            CreatedAt = DateTime.Now
        }).ToList();
        
        await repo.BatchInsertAsync(products);

        // Act - Request page beyond available data
        var pageProducts = await repo.GetPageAsync(50, 100); // Offset 100, but only 50 records

        // Assert
        Assert.AreEqual(0, pageProducts.Count, "Should return empty list for pages beyond available data");
    }
}

#region Models and Repositories

public class PaginatedProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IPaginatedProductRepository
{
    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate(@"
        INSERT INTO paginated_products ({{columns --exclude Id}})
        VALUES {{batch_values --exclude Id}}
    ")]
    Task<int> BatchInsertAsync(List<PaginatedProduct> products);

    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table paginated_products}}
        ORDER BY id
        {{limit @limit, @offset}}
    ")]
    Task<List<PaginatedProduct>> GetPageAsync(int limit, int offset);

    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table paginated_products}}
        WHERE category = @category
        ORDER BY id
        {{limit @limit, @offset}}
    ")]
    Task<List<PaginatedProduct>> GetPageByCategoryAsync(string category, int limit, int offset);

    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table paginated_products}}
        ORDER BY price DESC, id
        {{limit @limit, @offset}}
    ")]
    Task<List<PaginatedProduct>> GetPageOrderByPriceDescAsync(int limit, int offset);
}

public class PaginatedProductRepository : IPaginatedProductRepository
{
    private readonly System.Data.IDbConnection _connection;

    public PaginatedProductRepository(System.Data.IDbConnection connection)
    {
        _connection = connection;
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS paginated_products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                category TEXT NOT NULL,
                price REAL NOT NULL,
                created_at TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public async Task<int> BatchInsertAsync(List<PaginatedProduct> products)
    {
        var totalInserted = 0;
        foreach (var product in products)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO paginated_products (name, category, price, created_at)
                VALUES (@name, @category, @price, @createdAt)";
            
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@name";
            p1.Value = product.Name;
            cmd.Parameters.Add(p1);
            
            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@category";
            p2.Value = product.Category;
            cmd.Parameters.Add(p2);
            
            var p3 = cmd.CreateParameter();
            p3.ParameterName = "@price";
            p3.Value = product.Price;
            cmd.Parameters.Add(p3);
            
            var p4 = cmd.CreateParameter();
            p4.ParameterName = "@createdAt";
            p4.Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            cmd.Parameters.Add(p4);
            
            totalInserted += await Task.Run(() => cmd.ExecuteNonQuery());
        }
        return totalInserted;
    }

    public async Task<List<PaginatedProduct>> GetPageAsync(int limit, int offset)
    {
        var products = new List<PaginatedProduct>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, category, price, created_at 
            FROM paginated_products
            ORDER BY id
            LIMIT @limit OFFSET @offset";
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@limit";
        p1.Value = limit;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@offset";
        p2.Value = offset;
        cmd.Parameters.Add(p2);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            products.Add(new PaginatedProduct
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return products;
    }

    public async Task<List<PaginatedProduct>> GetPageByCategoryAsync(string category, int limit, int offset)
    {
        var products = new List<PaginatedProduct>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, category, price, created_at 
            FROM paginated_products
            WHERE category = @category
            ORDER BY id
            LIMIT @limit OFFSET @offset";
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@category";
        p1.Value = category;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@limit";
        p2.Value = limit;
        cmd.Parameters.Add(p2);
        
        var p3 = cmd.CreateParameter();
        p3.ParameterName = "@offset";
        p3.Value = offset;
        cmd.Parameters.Add(p3);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            products.Add(new PaginatedProduct
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return products;
    }

    public async Task<List<PaginatedProduct>> GetPageOrderByPriceDescAsync(int limit, int offset)
    {
        var products = new List<PaginatedProduct>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, category, price, created_at 
            FROM paginated_products
            ORDER BY price DESC, id
            LIMIT @limit OFFSET @offset";
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@limit";
        p1.Value = limit;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@offset";
        p2.Value = offset;
        cmd.Parameters.Add(p2);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            products.Add(new PaginatedProduct
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return products;
    }
}

#endregion
