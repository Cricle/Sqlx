// <copyright file="RepositoryE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Annotations;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Repository;

#region Test Models

[Sqlx]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
}

#endregion

#region Test Repositories

public interface IProductRepository
{
    [SqlTemplate("INSERT INTO products (name, price, stock, description) VALUES (@name, @price, @stock, @description)")]
    Task<int> InsertAsync(string name, decimal price, int stock, string? description);

    [SqlTemplate("SELECT id, name, price, stock, description FROM products WHERE id = @id")]
    Task<Product?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, name, price, stock, description FROM products WHERE price >= @minPrice")]
    Task<List<Product>> GetByMinPriceAsync(decimal minPrice);

    [SqlTemplate("UPDATE products SET name = @name, price = @price, stock = @stock, description = @description WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name, decimal price, int stock, string? description);

    [SqlTemplate("DELETE FROM products WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    [SqlTemplate("SELECT COUNT(*) FROM products")]
    Task<int> CountAsync();

    [SqlTemplate("SELECT id, name, price, stock, description FROM products ORDER BY price DESC LIMIT @limit")]
    Task<List<Product>> GetTopExpensiveAsync(int limit);
}

[RepositoryFor(typeof(IProductRepository), TableName = "products")]
public partial class MySqlProductRepository : IProductRepository
{
    private readonly DbConnection _connection;

    public MySqlProductRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IProductRepository), TableName = "products")]
public partial class PostgreSqlProductRepository : IProductRepository
{
    private readonly DbConnection _connection;

    public PostgreSqlProductRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IProductRepository), TableName = "products")]
public partial class SqlServerProductRepository : IProductRepository
{
    private readonly DbConnection _connection;

    public SqlServerProductRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IProductRepository), TableName = "products")]
public partial class SQLiteProductRepository : IProductRepository
{
    private readonly DbConnection _connection;

    public SQLiteProductRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

#endregion

/// <summary>
/// E2E tests for Sqlx Repository pattern functionality.
/// Tests CRUD operations, parameterization, and query execution through repositories.
/// </summary>
[TestClass]
public class RepositoryE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE products (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(200) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    stock INT NOT NULL,
                    description TEXT
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(200) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    stock INT NOT NULL,
                    description TEXT
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(200) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    stock INT NOT NULL,
                    description NVARCHAR(MAX)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL,
                    description TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== Repository Insert Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("MySQL")]
    public async Task MySQL_Repository_Insert_ExecutesSuccessfully()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var repo = new MySqlProductRepository(fixture.Connection, SqlDefine.MySql);

        // Act - Test Repository Insert with SqlTemplate
        var rowsAffected = await repo.InsertAsync("Laptop", 999.99m, 10, "High-end laptop");

        // Assert - Repository INSERT may return -1 if not configured to return rows affected
        Assert.IsTrue(rowsAffected == 1 || rowsAffected == -1, "Should execute successfully");
        
        // Verify by querying
        var products = await repo.GetByMinPriceAsync(0m);
        Assert.IsTrue(products.Count >= 1, "Should have at least one product");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Repository_Insert_WithNullDescription_HandlesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlProductRepository(fixture.Connection, SqlDefine.PostgreSql);

        // Act - Test Repository with nullable parameter
        var rowsAffected = await repo.InsertAsync("Mouse", 29.99m, 50, null);

        // Assert
        Assert.AreEqual(1, rowsAffected);
    }

    // ==================== Repository Query Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("MySQL")]
    public async Task MySQL_Repository_GetByMinPrice_ReturnsFilteredResults()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var repo = new MySqlProductRepository(fixture.Connection, SqlDefine.MySql);

        await repo.InsertAsync("Laptop", 999.99m, 10, "Expensive");
        await repo.InsertAsync("Mouse", 29.99m, 50, "Cheap");
        await repo.InsertAsync("Keyboard", 79.99m, 30, "Mid-range");

        // Act - Test Repository query with parameter
        var products = await repo.GetByMinPriceAsync(50m);

        // Assert
        Assert.AreEqual(2, products.Count);
        Assert.IsTrue(products.All(p => p.Price >= 50m));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Repository_GetTopExpensive_ReturnsLimitedResults()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlProductRepository(fixture.Connection, SqlDefine.PostgreSql);

        await repo.InsertAsync("Product A", 100m, 10, null);
        await repo.InsertAsync("Product B", 200m, 10, null);
        await repo.InsertAsync("Product C", 300m, 10, null);
        await repo.InsertAsync("Product D", 400m, 10, null);

        // Act - Test Repository with LIMIT parameter
        var products = await repo.GetTopExpensiveAsync(2);

        // Assert
        Assert.AreEqual(2, products.Count);
        Assert.AreEqual(400m, products[0].Price);
        Assert.AreEqual(300m, products[1].Price);
    }

    // ==================== Repository Update Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Repository_Update_ModifiesExistingRecord()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerProductRepository(fixture.Connection, SqlDefine.SqlServer);

        await repo.InsertAsync("Old Name", 100m, 10, "Old Description");
        
        // Get the inserted ID
        var products = await repo.GetByMinPriceAsync(0m);
        var productId = products[0].Id;

        // Act - Test Repository Update
        var rowsAffected = await repo.UpdateAsync(productId, "New Name", 150m, 20, "New Description");

        // Assert
        Assert.AreEqual(1, rowsAffected);
        var updated = await repo.GetByIdAsync(productId);
        Assert.IsNotNull(updated);
        Assert.AreEqual("New Name", updated.Name);
        Assert.AreEqual(150m, updated.Price);
        Assert.AreEqual(20, updated.Stock);
        Assert.AreEqual("New Description", updated.Description);
    }

    // ==================== Repository Delete Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("SQLite")]
    public async Task SQLite_Repository_Delete_RemovesRecord()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var repo = new SQLiteProductRepository(fixture.Connection, SqlDefine.SQLite);

        await repo.InsertAsync("To Delete", 50m, 5, null);
        var products = await repo.GetByMinPriceAsync(0m);
        var productId = products[0].Id;

        // Act - Test Repository Delete
        var rowsAffected = await repo.DeleteAsync(productId);

        // Assert - DELETE may return -1 if not configured to return rows affected
        Assert.IsTrue(rowsAffected == 1 || rowsAffected == -1, "Should execute successfully");
        var deleted = await repo.GetByIdAsync(productId);
        Assert.IsNull(deleted, "Product should be deleted");
    }

    // ==================== Repository Entity Mapping Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("MySQL")]
    public async Task MySQL_Repository_GetById_MapsToEntityCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var repo = new MySqlProductRepository(fixture.Connection, SqlDefine.MySql);

        await repo.InsertAsync("Test Product", 123.45m, 99, "Test Description");
        var products = await repo.GetByMinPriceAsync(0m);
        var productId = products[0].Id;

        // Act - Test Repository entity mapping
        var product = await repo.GetByIdAsync(productId);

        // Assert
        Assert.IsNotNull(product);
        Assert.AreEqual(productId, product.Id);
        Assert.AreEqual("Test Product", product.Name);
        Assert.AreEqual(123.45m, product.Price);
        Assert.AreEqual(99, product.Stock);
        Assert.AreEqual("Test Description", product.Description);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Repository_GetAll_ReturnsAllRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlProductRepository(fixture.Connection, SqlDefine.PostgreSql);

        await repo.InsertAsync("Product 1", 10m, 1, null);
        await repo.InsertAsync("Product 2", 20m, 2, null);
        await repo.InsertAsync("Product 3", 30m, 3, null);

        // Act - Test Repository GetAll
        var products = await repo.GetByMinPriceAsync(0m);

        // Assert
        Assert.AreEqual(3, products.Count);
        Assert.IsTrue(products.All(p => p.Id > 0));
        Assert.IsTrue(products.All(p => !string.IsNullOrEmpty(p.Name)));
    }
}
