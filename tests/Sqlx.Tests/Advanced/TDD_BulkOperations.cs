using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Advanced;

/// <summary>
/// TDD: 批量操作测试 - 高效的批量处理
/// Phase 3.3: 批量INSERT/UPDATE/DELETE
/// </summary>
[TestClass]
public class TDD_BulkOperations
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Bulk")]
    [TestCategory("Phase3")]
    public void Bulk_DeleteByIds_ShouldRemoveMultipleRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO products VALUES ({i}, 'Product {i}', {i * 10}, 'General')");
        }

        var repo = new BulkOperationRepository(connection);

        // Act: 删除ID为1,3,5的记录
        var affected = repo.DeleteProductsByIdsAsync().Result;

        // Assert
        Assert.IsTrue(affected >= 3);
        var remaining = repo.GetAllProductsAsync().Result;
        Assert.IsFalse(remaining.Any(p => p.Id == 1 || p.Id == 3 || p.Id == 5));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Bulk")]
    [TestCategory("Phase3")]
    public void Bulk_UpdateByCategory_ShouldUpdateMultipleRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        connection.Execute("INSERT INTO products VALUES (1, 'A', 100, 'Electronics')");
        connection.Execute("INSERT INTO products VALUES (2, 'B', 200, 'Electronics')");
        connection.Execute("INSERT INTO products VALUES (3, 'C', 50, 'Books')");

        var repo = new BulkOperationRepository(connection);

        // Act: 批量更新Electronics类别的价格
        var affected = repo.UpdateElectronicsPricesAsync(1.1).Result;

        // Assert
        Assert.AreEqual(2, affected);
        var products = repo.GetAllProductsAsync().Result;
        Assert.AreEqual(110, products.First(p => p.Id == 1).Price, 0.01); // 浮点数比较需要delta
        Assert.AreEqual(50, products.First(p => p.Id == 3).Price, 0.01); // Books不受影响

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Bulk")]
    [TestCategory("Phase3")]
    public void Bulk_DeleteByCondition_ShouldRemoveMatchingRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        connection.Execute("INSERT INTO products VALUES (1, 'Cheap', 10, 'General'), (2, 'Mid', 50, 'General'), (3, 'Expensive', 100, 'General')");

        var repo = new BulkOperationRepository(connection);

        // Act: 删除价格<30的产品
        var affected = repo.DeleteCheapProductsAsync(30).Result;

        // Assert
        Assert.AreEqual(1, affected);
        var remaining = repo.GetAllProductsAsync().Result;
        Assert.AreEqual(2, remaining.Count);
        Assert.IsTrue(remaining.All(p => p.Price >= 30));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Bulk")]
    [TestCategory("Phase3")]
    public void Bulk_SelectMultipleIds_ShouldReturnMatchingRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO products VALUES ({i}, 'Product {i}', {i * 10}, 'General')");
        }

        var repo = new BulkOperationRepository(connection);

        // Act: 查询ID为2,4,6的记录
        var results = repo.GetProductsByIdsAsync().Result;

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.Any(p => p.Id == 2));
        Assert.IsTrue(results.Any(p => p.Id == 4));
        Assert.IsTrue(results.Any(p => p.Id == 6));

        connection.Dispose();
    }
}

// Models
public class BulkProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public string? Category { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBulkOperationRepository))]
public partial class BulkOperationRepository(IDbConnection connection) : IBulkOperationRepository { }

public interface IBulkOperationRepository
{
    [SqlTemplate("DELETE FROM products WHERE id IN (1, 3, 5)")]
    Task<int> DeleteProductsByIdsAsync();

    [SqlTemplate("UPDATE products SET price = price * @multiplier WHERE category = 'Electronics'")]
    Task<int> UpdateElectronicsPricesAsync(double multiplier);

    [SqlTemplate("DELETE FROM products WHERE price < @maxPrice")]
    Task<int> DeleteCheapProductsAsync(double maxPrice);

    [SqlTemplate("SELECT id, name, price, category FROM products WHERE id IN (2, 4, 6)")]
    Task<List<BulkProduct>> GetProductsByIdsAsync();

    [SqlTemplate("SELECT id, name, price, category FROM products")]
    Task<List<BulkProduct>> GetAllProductsAsync();
}

