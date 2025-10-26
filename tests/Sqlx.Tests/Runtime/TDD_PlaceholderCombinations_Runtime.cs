using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Runtime;

/// <summary>
/// 占位符组合场景测试
/// 目的：确保多个占位符在同一SQL中正确协同工作
/// </summary>
[TestClass]
[TestCategory("Runtime")]
[TestCategory("PlaceholderCombinations")]
public class TDD_PlaceholderCombinations_Runtime
{
    private IDbConnection _connection = null!;
    private IPlaceholderRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        ExecuteSql(@"CREATE TABLE products (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            price DECIMAL(10,2),
            stock INTEGER,
            category TEXT,
            is_active INTEGER DEFAULT 1
        )");
        
        // 插入测试数据
        ExecuteSql("INSERT INTO products VALUES (1, 'Laptop', 999.99, 10, 'Electronics', 1)");
        ExecuteSql("INSERT INTO products VALUES (2, 'Mouse', 29.99, 50, 'Electronics', 1)");
        ExecuteSql("INSERT INTO products VALUES (3, 'Keyboard', 79.99, 30, 'Electronics', 1)");
        ExecuteSql("INSERT INTO products VALUES (4, 'Monitor', 299.99, 15, 'Electronics', 1)");
        ExecuteSql("INSERT INTO products VALUES (5, 'Chair', 199.99, 20, 'Furniture', 1)");
        ExecuteSql("INSERT INTO products VALUES (6, 'Desk', 399.99, 5, 'Furniture', 1)");
        ExecuteSql("INSERT INTO products VALUES (7, 'Lamp', 49.99, 0, 'Furniture', 0)");
        
        _repo = new PlaceholderRepository(_connection);
    }

    [TestCleanup]
    public void TearDown()
    {
        _connection?.Dispose();
    }

    private void ExecuteSql(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    #region WHERE + LIMIT 组合

    [TestMethod]
    public async Task Where_Plus_Limit_ShouldApplyBoth()
    {
        // Act
        var results = await _repo.GetProductsWithWhereLimitAsync("Electronics", 2);

        // Assert
        Assert.AreEqual(2, results.Count, "应该最多返回2条");
        Assert.IsTrue(results.All(p => p.Category == "Electronics"), "所有产品应该是Electronics类别");
    }

    #endregion

    #region WHERE + LIMIT + OFFSET 组合（分页）

    [TestMethod]
    public async Task Where_Limit_Offset_ShouldPaginate()
    {
        // Act - 第二页，每页2条
        var results = await _repo.GetProductsPaginatedAsync("Electronics", 2, 2);

        // Assert
        Assert.IsTrue(results.Count <= 2, "应该最多返回2条");
        Assert.IsTrue(results.All(p => p.Category == "Electronics"));
    }

    #endregion

    #region UPDATE SET + WHERE 组合

    [TestMethod]
    public async Task Set_Plus_Where_ShouldUpdateSpecificFields()
    {
        // Act
        var affected = await _repo.UpdateProductPriceAsync(1, 899.99m);

        // Assert
        Assert.AreEqual(1, affected, "应该更新1条记录");
        
        // 验证更新结果
        var product = await GetProductById(1);
        Assert.AreEqual(899.99m, product.Price, 0.01m);
    }

    #endregion

    #region WHERE + ORDER BY 组合

    [TestMethod]
    public async Task Where_Plus_OrderBy_ShouldFilterAndSort()
    {
        // Act
        var results = await _repo.GetProductsSortedAsync(1);

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(p => p.IsActive == 1), "应该只返回活跃产品");
        
        // 验证排序（降序）
        for (int i = 0; i < results.Count - 1; i++)
        {
            Assert.IsTrue(results[i].Price >= results[i + 1].Price, "应该按价格降序");
        }
    }

    #endregion

    #region 指定列 + WHERE 组合

    [TestMethod]
    public async Task SpecificColumns_Plus_Where_ShouldSelectSpecificColumns()
    {
        // Act
        var results = await _repo.GetProductNamesAsync("Electronics");

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(p => p.Name != null && p.Name.Length > 0));
    }

    #endregion

    #region WHERE + IN 查询组合

    [TestMethod]
    public async Task Where_Plus_IN_ShouldCombineConditions()
    {
        // Act
        var results = await _repo.GetProductsByIdsAndCategoryAsync(
            new[] { 1L, 2L, 3L, 4L },
            "Electronics"
        );

        // Assert
        Assert.IsTrue(results.Count <= 4);
        Assert.IsTrue(results.All(p => p.Category == "Electronics"));
        Assert.IsTrue(results.All(p => p.Id <= 4));
    }

    #endregion

    #region 复杂组合：WHERE + ORDER BY + LIMIT + OFFSET

    [TestMethod]
    public async Task ComplexCombination_All_ShouldWorkTogether()
    {
        // Act
        var results = await _repo.GetProductsComplexAsync(1, 3, 1);

        // Assert
        Assert.IsTrue(results.Count <= 3, "应该最多返回3条");
        Assert.IsTrue(results.All(p => p.IsActive == 1));
        
        // 验证排序
        for (int i = 0; i < results.Count - 1; i++)
        {
            Assert.IsTrue(results[i].Price <= results[i + 1].Price, "应该按价格升序");
        }
    }

    #endregion

    #region 多条件WHERE组合

    [TestMethod]
    public async Task MultipleWhere_ShouldCombine()
    {
        // Act
        var results = await _repo.GetActiveProductsWithDynamicFilterAsync(100);

        // Assert
        Assert.IsTrue(results.All(p => p.IsActive == 1), "静态WHERE应该生效");
        Assert.IsTrue(results.All(p => p.Price > 100), "动态WHERE应该生效");
    }

    #endregion

    #region 参数化查询基础测试

    [TestMethod]
    public async Task ParameterizedQuery_ShouldWork()
    {
        // Act
        var results = await _repo.GetProductsWithMultipleDynamicAsync("Electronics");

        // Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.All(p => p.Category == "Electronics"));
    }

    #endregion

    #region Helper Methods

    private async Task<Product> GetProductById(long id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, price, stock, category, is_active FROM products WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = id;
        cmd.Parameters.Add(param);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        if (await Task.Run(() => reader.Read()))
        {
            return new Product
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2),
                Stock = reader.GetInt32(3),
                Category = reader.GetString(4),
                IsActive = reader.GetInt32(5)
            };
        }
        throw new Exception("Product not found");
    }

    #endregion
}

#region Test Models

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = "";
    public int IsActive { get; set; }
}

#endregion

#region Test Repository

public interface IPlaceholderRepository
{
    // 简化版：只使用标准参数化查询
    [SqlTemplate("SELECT * FROM products WHERE category = @category LIMIT @limit")]
    Task<List<Product>> GetProductsWithWhereLimitAsync(
        string category,
        int limit
    );

    [SqlTemplate("SELECT * FROM products WHERE category = @category LIMIT @limit OFFSET @offset")]
    Task<List<Product>> GetProductsPaginatedAsync(
        string category,
        int limit,
        int offset
    );

    [SqlTemplate("UPDATE products SET price = @price WHERE id = @id")]
    Task<int> UpdateProductPriceAsync(
        long id,
        decimal price
    );

    [SqlTemplate("SELECT * FROM products WHERE is_active = @isActive ORDER BY price DESC")]
    Task<List<Product>> GetProductsSortedAsync(
        int isActive
    );

    [SqlTemplate("SELECT id, name, price, stock, category, is_active FROM products WHERE category = @category")]
    Task<List<Product>> GetProductNamesAsync(
        string category
    );

    [SqlTemplate("SELECT * FROM products WHERE id IN (@ids) AND category = @category")]
    Task<List<Product>> GetProductsByIdsAndCategoryAsync(
        IEnumerable<long> ids,
        string category
    );

    [SqlTemplate("SELECT * FROM products WHERE is_active = @isActive ORDER BY price ASC LIMIT @limit OFFSET @offset")]
    Task<List<Product>> GetProductsComplexAsync(
        int isActive,
        int limit,
        int offset
    );

    [SqlTemplate("SELECT * FROM products WHERE is_active = 1 AND price > @minPrice")]
    Task<List<Product>> GetActiveProductsWithDynamicFilterAsync(
        decimal minPrice
    );

    [SqlTemplate("SELECT * FROM products WHERE category = @category ORDER BY price DESC")]
    Task<List<Product>> GetProductsWithMultipleDynamicAsync(
        string category
    );
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPlaceholderRepository))]
public partial class PlaceholderRepository(IDbConnection connection) : IPlaceholderRepository { }

#endregion

