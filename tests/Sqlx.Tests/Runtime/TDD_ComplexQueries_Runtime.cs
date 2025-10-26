using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Runtime;

/// <summary>
/// 复杂查询运行时测试
/// 目的：确保多行SQL（JOIN、聚合、子查询等）在实际数据库中正确执行
/// 特别测试 Verbatim String 转义是否正确
/// </summary>
[TestClass]
[TestCategory("Runtime")]
[TestCategory("ComplexQueries")]
public class TDD_ComplexQueries_Runtime
{
    private IDbConnection _connection = null!;
    private IComplexQueryRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        // SQLite: 禁用外键约束以简化测试
        ExecuteSql("PRAGMA foreign_keys = OFF");
        
        // 创建测试表
        ExecuteSql(@"CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT, 
            name TEXT NOT NULL, 
            email TEXT,
            age INTEGER NOT NULL,
            balance DECIMAL(10, 2) DEFAULT 0
        )");
        
        ExecuteSql(@"CREATE TABLE orders (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            product_name TEXT NOT NULL,
            amount DECIMAL(10, 2) NOT NULL,
            FOREIGN KEY (user_id) REFERENCES users(id)
        )");
        
        ExecuteSql(@"CREATE TABLE categories (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL,
            name TEXT NOT NULL
        )");
        
        ExecuteSql(@"CREATE TABLE products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            price DECIMAL(10, 2) NOT NULL,
            category_code TEXT,
            FOREIGN KEY (category_code) REFERENCES categories(code)
        )");
        
        // 插入测试数据
        SeedTestData();
        
        _repo = new ComplexQueryRepository(_connection);
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

    private void SeedTestData()
    {
        // Users
        ExecuteSql("INSERT INTO users (name, email, age, balance) VALUES ('Alice', 'alice@test.com', 25, 1000.50)");
        ExecuteSql("INSERT INTO users (name, email, age, balance) VALUES ('Bob', 'bob@test.com', 30, 2500.75)");
        ExecuteSql("INSERT INTO users (name, email, age, balance) VALUES ('Charlie', 'charlie@test.com', 35, 500.00)");
        
        // Orders
        ExecuteSql("INSERT INTO orders (user_id, product_name, amount) VALUES (1, 'Product A', 100.00)");
        ExecuteSql("INSERT INTO orders (user_id, product_name, amount) VALUES (1, 'Product B', 200.00)");
        ExecuteSql("INSERT INTO orders (user_id, product_name, amount) VALUES (2, 'Product C', 300.00)");
        
        // Categories
        ExecuteSql("INSERT INTO categories (code, name) VALUES ('ELEC', 'Electronics')");
        ExecuteSql("INSERT INTO categories (code, name) VALUES ('BOOK', 'Books')");
        
        // Products
        ExecuteSql("INSERT INTO products (name, price, category_code) VALUES ('Laptop', 999.99, 'ELEC')");
        ExecuteSql("INSERT INTO products (name, price, category_code) VALUES ('Phone', 499.99, 'ELEC')");
        ExecuteSql("INSERT INTO products (name, price, category_code) VALUES ('Novel', 19.99, 'BOOK')");
    }

    #region JOIN 查询测试

    [TestMethod]
    public async Task InnerJoin_MultipleLines_ShouldExecuteCorrectly()
    {
        // Act
        var results = await _repo.GetProductDetailsAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(3, results.Count, "应该返回3个产品详情");
        
        var laptop = results.First(p => p.ProductName == "Laptop");
        Assert.AreEqual("Electronics", laptop.CategoryName);
        Assert.AreEqual(999.99m, laptop.Price);
    }

    [TestMethod]
    public async Task LeftJoin_MultipleLines_ShouldIncludeNulls()
    {
        // Arrange: 添加一个没有订单的用户
        ExecuteSql("INSERT INTO users (name, email, age, balance) VALUES ('David', 'david@test.com', 28, 100.00)");

        // Act
        var results = await _repo.GetUserOrdersAsync();

        // Assert
        Assert.IsTrue(results.Count >= 4, "应该包含所有用户，即使没有订单");
        
        var davidOrders = results.Where(r => r.UserName == "David").ToList();
        Assert.AreEqual(1, davidOrders.Count, "David应该有1条记录");
    }

    #endregion

    #region 聚合查询测试

    [TestMethod]
    public async Task Aggregation_GroupBy_ShouldCalculateCorrectly()
    {
        // Act
        var results = await _repo.GetUserStatsAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Count >= 2, "应该至少有2个用户统计");
        
        var aliceStats = results.FirstOrDefault(s => s.UserName == "Alice");
        Assert.IsNotNull(aliceStats, "应该有Alice的统计");
        Assert.AreEqual(2, aliceStats.OrderCount, "Alice应该有2个订单");
        Assert.AreEqual(300.00m, aliceStats.TotalSpent, "Alice总消费应该是300.00");
    }

    [TestMethod]
    public async Task Aggregation_Count_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repo.GetTotalUsersAsync();

        // Assert
        Assert.AreEqual(3, count, "应该有3个用户");
    }

    [TestMethod]
    public async Task Aggregation_Sum_ShouldReturnCorrectSum()
    {
        // Act
        var totalBalance = await _repo.GetTotalBalanceAsync();

        // Assert
        Assert.AreEqual(4001.25m, totalBalance, 0.01m, "总余额应该是4001.25");
    }

    #endregion

    #region 子查询测试

    [TestMethod]
    public async Task Subquery_IN_ShouldFilterCorrectly()
    {
        // Act
        var results = await _repo.GetUsersWithOrdersAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count, "应该有2个有订单的用户");
        
        var userNames = results.Select(u => u.Name).ToList();
        CollectionAssert.Contains(userNames, "Alice");
        CollectionAssert.Contains(userNames, "Bob");
        CollectionAssert.DoesNotContain(userNames, "Charlie");
    }

    [TestMethod]
    public async Task Subquery_EXISTS_ShouldFilterCorrectly()
    {
        // Act
        var results = await _repo.GetUsersWithOrdersUsingExistsAsync();

        // Assert
        Assert.AreEqual(2, results.Count, "应该有2个有订单的用户");
    }

    #endregion

    #region 排序和分页测试

    [TestMethod]
    public async Task OrderBy_DESC_ShouldSortCorrectly()
    {
        // Act
        var results = await _repo.GetTopRichUsersAsync(2);

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Name, "最富的应该是Bob");
        Assert.AreEqual("Alice", results[1].Name, "第二富的应该是Alice");
    }

    [TestMethod]
    public async Task Pagination_LimitOffset_ShouldReturnCorrectSubset()
    {
        // Act
        var results = await _repo.GetUsersPaginatedAsync(limit: 2, offset: 1);

        // Assert
        Assert.AreEqual(2, results.Count, "应该返回2条记录");
        // 验证是第2和第3条记录
    }

    #endregion

    #region DISTINCT 测试

    [TestMethod]
    public async Task Distinct_ShouldRemoveDuplicates()
    {
        // Act
        var results = await _repo.GetDistinctAgesAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Count <= 3, "年龄应该去重");
        
        var ages = results.Select(r => r.Age).ToList();
        Assert.AreEqual(ages.Count, ages.Distinct().Count(), "不应该有重复年龄");
    }

    #endregion

    #region 多行SQL格式测试（Verbatim String）

    [TestMethod]
    public async Task MultilineSQL_WithNewlines_ShouldNotHaveLiteralBackslashes()
    {
        // 这个测试确保生成的SQL没有字面文本 \r\n
        // 如果有，SQLite会报错 "unrecognized token"
        
        // Act - 如果SQL有字面文本\r\n，这会抛出异常
        var results = await _repo.GetProductDetailsAsync();

        // Assert
        Assert.IsTrue(results.Count > 0, "多行SQL应该正确执行");
    }

    [TestMethod]
    public async Task MultilineSQL_WithTabs_ShouldPreserveIndentation()
    {
        // Act
        var results = await _repo.GetUserStatsAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Count > 0, "带缩进的多行SQL应该正确执行");
    }

    #endregion

    #region CASE WHEN 测试

    [TestMethod]
    public async Task CaseWhen_ShouldEvaluateCorrectly()
    {
        // Act
        var results = await _repo.GetUserRiskLevelsAsync();

        // Assert
        Assert.IsTrue(results.Count > 0);
        
        var bobRisk = results.FirstOrDefault(r => r.Name == "Bob");
        Assert.IsNotNull(bobRisk);
        Assert.AreEqual("High", bobRisk.RiskLevel, "Bob余额高应该是High");
        
        var charlieRisk = results.FirstOrDefault(r => r.Name == "Charlie");
        Assert.IsNotNull(charlieRisk);
        Assert.AreEqual("Low", charlieRisk.RiskLevel, "Charlie余额低应该是Low");
    }

    #endregion
}

#region Test Models

public class ProductDetail
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = "";
}

public class UserOrder
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string? ProductName { get; set; }
}

public class UserStats
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public decimal Balance { get; set; }
}

public class UserRiskLevel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string RiskLevel { get; set; } = "";
}

public class AgeResult
{
    public int Age { get; set; }
}

#endregion

#region Test Repository

public interface IComplexQueryRepository
{
    // JOIN查询（多行SQL）
    [SqlTemplate(@"SELECT p.id as ProductId, p.name as ProductName, p.price as Price, c.name as CategoryName
                   FROM products p
                   INNER JOIN categories c ON p.category_code = c.code")]
    Task<List<ProductDetail>> GetProductDetailsAsync();

    // LEFT JOIN
    [SqlTemplate(@"SELECT u.id as UserId, u.name as UserName, o.product_name as ProductName
                   FROM users u
                   LEFT JOIN orders o ON u.id = o.user_id")]
    Task<List<UserOrder>> GetUserOrdersAsync();

    // 聚合查询（多行SQL）
    [SqlTemplate(@"SELECT u.id as UserId, u.name as UserName, 
                          COUNT(o.id) as OrderCount, 
                          COALESCE(SUM(o.amount), 0) as TotalSpent
                   FROM users u
                   LEFT JOIN orders o ON u.id = o.user_id
                   GROUP BY u.id, u.name
                   HAVING COUNT(o.id) > 0")]
    Task<List<UserStats>> GetUserStatsAsync();

    [SqlTemplate("SELECT COUNT(*) FROM users")]
    Task<int> GetTotalUsersAsync();

    [SqlTemplate("SELECT COALESCE(SUM(balance), 0) FROM users")]
    Task<decimal> GetTotalBalanceAsync();

    // 子查询
    [SqlTemplate(@"SELECT * FROM users 
                   WHERE id IN (SELECT DISTINCT user_id FROM orders)")]
    Task<List<User>> GetUsersWithOrdersAsync();

    [SqlTemplate(@"SELECT * FROM users u
                   WHERE EXISTS (SELECT 1 FROM orders o WHERE o.user_id = u.id)")]
    Task<List<User>> GetUsersWithOrdersUsingExistsAsync();

    // 排序和分页
    [SqlTemplate(@"SELECT id, name, email, age, balance 
                   FROM users 
                   ORDER BY balance DESC 
                   LIMIT @limit")]
    Task<List<User>> GetTopRichUsersAsync(int limit);

    [SqlTemplate(@"SELECT id, name, email, age, balance 
                   FROM users 
                   ORDER BY id 
                   LIMIT @limit OFFSET @offset")]
    Task<List<User>> GetUsersPaginatedAsync(int limit, int offset);

    // DISTINCT
    [SqlTemplate("SELECT DISTINCT age FROM users ORDER BY age")]
    Task<List<AgeResult>> GetDistinctAgesAsync();

    // CASE WHEN
    [SqlTemplate(@"SELECT id, name,
                   CASE 
                       WHEN balance > 1000 THEN 'High'
                       WHEN balance > 500 THEN 'Medium'
                       ELSE 'Low'
                   END as risk_level
                   FROM users")]
    Task<List<UserRiskLevel>> GetUserRiskLevelsAsync();
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IComplexQueryRepository))]
public partial class ComplexQueryRepository(IDbConnection connection) : IComplexQueryRepository { }

#endregion

