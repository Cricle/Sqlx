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
/// TDD: 分页查询测试 - LIMIT/OFFSET
/// Phase 3.1: 实用的分页场景
/// </summary>
[TestClass]
public class TDD_Pagination
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Phase3")]
    public void Pagination_FirstPage_ShouldReturnCorrectRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        for (int i = 1; i <= 20; i++)
        {
            connection.Execute($"INSERT INTO products VALUES ({i}, 'Product {i}', {i * 10}, 'General')");
        }

        var repo = new PaginationRepository(connection);

        // Act: 获取第1页，每页10条
        var results = repo.GetPagedProductsAsync(10, 0).Result;

        // Assert
        Assert.AreEqual(10, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual(10, results[9].Id);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Phase3")]
    public void Pagination_SecondPage_ShouldReturnCorrectRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        for (int i = 1; i <= 20; i++)
        {
            connection.Execute($"INSERT INTO products VALUES ({i}, 'Product {i}', {i * 10}, 'General')");
        }

        var repo = new PaginationRepository(connection);

        // Act: 获取第2页，每页10条
        var results = repo.GetPagedProductsAsync(10, 10).Result;

        // Assert
        Assert.AreEqual(10, results.Count);
        Assert.AreEqual(11, results[0].Id);
        Assert.AreEqual(20, results[9].Id);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Phase3")]
    public void Pagination_LastPagePartial_ShouldReturnRemainingRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        for (int i = 1; i <= 25; i++)
        {
            connection.Execute($"INSERT INTO products VALUES ({i}, 'Product {i}', {i * 10}, 'General')");
        }

        var repo = new PaginationRepository(connection);

        // Act: 获取第3页，每页10条（只剩5条）
        var results = repo.GetPagedProductsAsync(10, 20).Result;

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual(21, results[0].Id);
        Assert.AreEqual(25, results[4].Id);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Phase3")]
    public void Pagination_WithOrderBy_ShouldRespectOrdering()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        connection.Execute("INSERT INTO products VALUES (1, 'C', 300, 'General'), (2, 'A', 100, 'General'), (3, 'B', 200, 'General')");

        var repo = new PaginationRepository(connection);

        // Act: 按价格排序后分页
        var results = repo.GetPagedProductsByPriceAsync(2, 0).Result;

        // Assert: 应该是价格最低的2个
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(100, results[0].Price);
        Assert.AreEqual(200, results[1].Price);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Phase3")]
    public void Pagination_EmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        connection.Execute("INSERT INTO products VALUES (1, 'A', 100, 'General')");

        var repo = new PaginationRepository(connection);

        // Act: 请求超出范围的页
        var results = repo.GetPagedProductsAsync(10, 100).Result;

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Pagination")]
    [TestCategory("Phase3")]
    public void Pagination_WithFilter_ShouldCombineWhereAndLimit()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, category TEXT)");
        for (int i = 1; i <= 20; i++)
        {
            var category = i % 2 == 0 ? "Electronics" : "Books";
            connection.Execute($"INSERT INTO products VALUES ({i}, 'Product {i}', {i * 10}, '{category}')");
        }

        var repo = new PaginationRepository(connection);

        // Act: 过滤Electronics类别，然后分页
        var results = repo.GetPagedElectronicsAsync(5, 0).Result;

        // Assert: 应该返回前5个Electronics产品
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(p => p.Category == "Electronics"));

        connection.Dispose();
    }
}

// Models
public class PaginationProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public string? Category { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPaginationRepository))]
public partial class PaginationRepository(IDbConnection connection) : IPaginationRepository { }

public interface IPaginationRepository
{
    [SqlTemplate("SELECT id, name, price, category FROM products LIMIT @limit OFFSET @offset")]
    Task<List<PaginationProduct>> GetPagedProductsAsync(int limit, int offset);

    [SqlTemplate("SELECT id, name, price, category FROM products ORDER BY price ASC LIMIT @limit OFFSET @offset")]
    Task<List<PaginationProduct>> GetPagedProductsByPriceAsync(int limit, int offset);

    [SqlTemplate("SELECT id, name, price, category FROM products WHERE category = 'Electronics' LIMIT @limit OFFSET @offset")]
    Task<List<PaginationProduct>> GetPagedElectronicsAsync(int limit, int offset);
}

// Extension method for test setup
public static class PaginationTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

