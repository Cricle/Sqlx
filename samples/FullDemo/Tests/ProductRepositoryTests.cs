using FullDemo.Models;
using FullDemo.Repositories;

namespace FullDemo.Tests;

/// <summary>
/// 产品仓储测试 - 测试软删除功能
/// </summary>
public class ProductRepositoryTests
{
    private readonly IProductRepository _repo;
    private readonly string _dbType;

    public ProductRepositoryTests(IProductRepository repo, string dbType)
    {
        _repo = repo;
        _dbType = dbType;
    }

    public async Task RunAllTestsAsync()
    {
        Console.WriteLine($"\n{'='} [{_dbType}] Product Repository Tests (Soft Delete) {new string('=', 40)}");

        await TestInsertAsync();
        await TestGetByCategoryAsync();
        await TestSearchAsync();
        await TestPriceRangeAsync();
        await TestSoftDeleteAsync();
        await TestRestoreAsync();
        await TestInventoryValueAsync();

        Console.WriteLine($"[{_dbType}] All Product Repository tests passed!\n");
    }

    private async Task TestInsertAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Insert...");

        var categories = new[] { "Electronics", "Books", "Clothing", "Food" };
        for (int i = 0; i < 5; i++)
        {
            var product = new Product
            {
                Name = $"Product_{Guid.NewGuid():N}",
                Category = categories[i % categories.Length],
                Price = 10.99m + (i * 5),
                Stock = 100 + (i * 10),
                IsDeleted = false
            };
            await _repo.InsertAsync(product);
        }
        Console.WriteLine($"    ✓ Inserted 5 products");
    }

    private async Task TestGetByCategoryAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByCategory...");

        var electronics = await _repo.GetByCategoryAsync("Electronics");
        Console.WriteLine($"    ✓ GetByCategory('Electronics'): Found {electronics.Count} products");
    }

    private async Task TestSearchAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Search...");

        var products = await _repo.SearchByNameAsync("%Product%");
        Console.WriteLine($"    ✓ SearchByName('%Product%'): Found {products.Count} products");
    }

    private async Task TestPriceRangeAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByPriceRange...");

        var products = await _repo.GetByPriceRangeAsync(10m, 30m);
        Console.WriteLine($"    ✓ GetByPriceRange(10-30): Found {products.Count} products");
    }

    private async Task TestSoftDeleteAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing SoftDelete...");

        // 获取一个产品
        var products = await _repo.GetActiveProductsAsync();
        if (products.Count == 0)
        {
            Console.WriteLine($"    ⚠ No products to soft delete, skipping...");
            return;
        }

        var product = products[0];
        var rowsAffected = await _repo.SoftDeleteProductAsync(product.Id);

        // 验证软删除
        var activeProducts = await _repo.GetActiveProductsAsync();
        var deletedProducts = await _repo.GetDeletedProductsAsync();

        Console.WriteLine($"    ✓ SoftDelete: {rowsAffected} row affected");
        Console.WriteLine($"    ✓ Active products: {activeProducts.Count}, Deleted products: {deletedProducts.Count}");
    }

    private async Task TestRestoreAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Restore...");

        var deletedProducts = await _repo.GetDeletedProductsAsync();
        if (deletedProducts.Count == 0)
        {
            Console.WriteLine($"    ⚠ No deleted products to restore, skipping...");
            return;
        }

        var product = deletedProducts[0];
        var rowsAffected = await _repo.RestoreProductAsync(product.Id);

        Console.WriteLine($"    ✓ Restore: {rowsAffected} row affected");
    }

    private async Task TestInventoryValueAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetTotalInventoryValue...");

        var totalValue = await _repo.GetTotalInventoryValueAsync();
        Console.WriteLine($"    ✓ TotalInventoryValue: {totalValue:C}");
    }
}
