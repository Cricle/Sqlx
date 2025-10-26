using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests.AdvancedSQL;

/// <summary>
/// Phase 2高级SQL特性测试
/// 包括：窗口函数、CTE、高级JOIN、深度子查询
/// </summary>
[TestClass]
public class TDD_AdvancedSQL_Phase2
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    #region 窗口函数测试 (10个)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("WindowFunction")]
    [Description("ROW_NUMBER基础功能")]
    public void WindowFunction_ROW_NUMBER_Basic_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE sales (id INTEGER, amount REAL)");
        _connection.Execute("INSERT INTO sales VALUES (1, 100), (2, 200), (3, 150)");

        var repo = new WindowFunctionRepo(_connection);

        // Act
        var results = repo.GetSalesWithRowNumber().Result;

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(1, results[0].RowNum);
        Assert.AreEqual(2, results[1].RowNum);
        Assert.AreEqual(3, results[2].RowNum);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("WindowFunction")]
    [Description("ROW_NUMBER with PARTITION BY")]
    public void WindowFunction_ROW_NUMBER_WithPartition_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE sales (id INTEGER, category TEXT, amount REAL)");
        _connection.Execute("INSERT INTO sales VALUES (1, 'A', 100), (2, 'A', 200), (3, 'B', 150), (4, 'B', 250)");

        var repo = new WindowFunctionRepo(_connection);

        // Act
        var results = repo.GetSalesWithRowNumberPartitioned().Result;

        // Assert
        Assert.AreEqual(4, results.Count);
        // 每个category内的行号应该重新开始
        Assert.AreEqual(1, results.First(r => r.Id == 1).RowNum); // A组第1个
        Assert.AreEqual(2, results.First(r => r.Id == 2).RowNum); // A组第2个
        Assert.AreEqual(1, results.First(r => r.Id == 3).RowNum); // B组第1个
        Assert.AreEqual(2, results.First(r => r.Id == 4).RowNum); // B组第2个
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("WindowFunction")]
    [Description("RANK应该处理并列情况")]
    public void WindowFunction_RANK_ShouldHandleTies()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE scores (id INTEGER, name TEXT, score INTEGER)");
        _connection.Execute("INSERT INTO scores VALUES (1, 'Alice', 90), (2, 'Bob', 90), (3, 'Charlie', 85)");

        var repo = new WindowFunctionRepo(_connection);

        // Act
        var results = repo.GetScoresWithRank().Result;

        // Assert
        Assert.AreEqual(3, results.Count);
        // Alice和Bob并列第1，所以都是rank=1
        Assert.AreEqual(1, results.First(r => r.Name == "Alice").RankNum);
        Assert.AreEqual(1, results.First(r => r.Name == "Bob").RankNum);
        // Charlie应该是rank=3（跳过rank=2）
        Assert.AreEqual(3, results.First(r => r.Name == "Charlie").RankNum);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("WindowFunction")]
    [Description("SUM OVER - 累计求和")]
    public void WindowFunction_SUM_Over_Running_Total_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE transactions (id INTEGER, amount REAL)");
        _connection.Execute("INSERT INTO transactions VALUES (1, 100), (2, 50), (3, 75)");

        var repo = new WindowFunctionRepo(_connection);

        // Act
        var results = repo.GetRunningTotal().Result;

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(100, results[0].RunningTotal, 0.01);
        Assert.AreEqual(150, results[1].RunningTotal, 0.01); // 100+50
        Assert.AreEqual(225, results[2].RunningTotal, 0.01); // 100+50+75
    }

    #endregion

    #region CTE测试 (8个)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CTE")]
    [Description("简单CTE应该正常工作")]
    public void CTE_Simple_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE users (id INTEGER, name TEXT, age INTEGER)");
        _connection.Execute("INSERT INTO users VALUES (1, 'Alice', 25), (2, 'Bob', 30), (3, 'Charlie', 35)");

        var repo = new CTERepo(_connection);

        // Act
        var results = repo.GetAdultsUsingCTE().Result;

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.All(u => u.Age >= 18));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CTE")]
    [Description("多个CTE应该可以链式使用")]
    public void CTE_Multiple_ShouldChain()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE employees (id INTEGER, name TEXT, salary REAL, department TEXT)");
        _connection.Execute("INSERT INTO employees VALUES (1, 'Alice', 50000, 'IT'), (2, 'Bob', 60000, 'IT'), (3, 'Charlie', 45000, 'HR')");

        var repo = new CTERepo(_connection);

        // Act
        var results = repo.GetHighSalaryITEmployees().Result;

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(e => e.Department == "IT" && e.Salary >= 50000));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CTE")]
    [Description("递归CTE应该处理层级数据")]
    public void CTE_Recursive_ShouldHandleHierarchy()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE categories (id INTEGER, name TEXT, parent_id INTEGER)");
        _connection.Execute("INSERT INTO categories VALUES (1, 'Root', NULL), (2, 'Child1', 1), (3, 'Child2', 1), (4, 'GrandChild', 2)");

        var repo = new CTERepo(_connection);

        // Act
        var results = repo.GetCategoryHierarchy().Result;

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Any(c => c.Name == "Root"));
        Assert.IsTrue(results.Any(c => c.Name == "GrandChild"));
    }

    #endregion

    #region 高级JOIN测试 (9个)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("AdvancedJoin")]
    [Description("3表JOIN应该正常工作")]
    public void Join_ThreeTables_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE users (id INTEGER, name TEXT)");
        _connection!.Execute("CREATE TABLE orders (id INTEGER, user_id INTEGER, product_id INTEGER)");
        _connection!.Execute("CREATE TABLE products (id INTEGER, name TEXT)");
        _connection.Execute("INSERT INTO users VALUES (1, 'Alice')");
        _connection.Execute("INSERT INTO products VALUES (10, 'Laptop')");
        _connection.Execute("INSERT INTO orders VALUES (100, 1, 10)");

        var repo = new AdvancedJoinRepo(_connection);

        // Act
        var results = repo.GetOrderDetails().Result;

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Alice", results[0].UserName);
        Assert.AreEqual("Laptop", results[0].ProductName);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("AdvancedJoin")]
    [Description("LEFT JOIN with WHERE filter应该正确处理NULL")]
    public void Join_LeftJoin_WithFilter_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE users (id INTEGER, name TEXT)");
        _connection!.Execute("CREATE TABLE orders (id INTEGER, user_id INTEGER, amount REAL)");
        _connection.Execute("INSERT INTO users VALUES (1, 'Alice'), (2, 'Bob')");
        _connection.Execute("INSERT INTO orders VALUES (10, 1, 100)");
        // Bob没有订单

        var repo = new AdvancedJoinRepo(_connection);

        // Act
        var results = repo.GetUsersWithOrders().Result;

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsNotNull(results.First(u => u.Name == "Alice").OrderCount);
        Assert.AreEqual(0, results.First(u => u.Name == "Bob").OrderCount);
    }

    #endregion

    #region 深度子查询测试 (8个)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("DeepSubquery")]
    [Description("3层嵌套子查询应该工作")]
    public void Subquery_ThreeLevels_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE products (id INTEGER, category TEXT, price REAL)");
        _connection.Execute("INSERT INTO products VALUES (1, 'A', 100), (2, 'A', 200), (3, 'B', 150)");

        var repo = new SubqueryRepo(_connection);

        // Act
        var results = repo.GetExpensiveProductsThreeLevels().Result;

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(p => p.Price >= 150));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("DeepSubquery")]
    [Description("相关子查询应该正确过滤")]
    public void Subquery_Correlated_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE employees (id INTEGER, name TEXT, salary REAL, department TEXT)");
        _connection.Execute("INSERT INTO employees VALUES (1, 'Alice', 50000, 'IT'), (2, 'Bob', 60000, 'IT'), (3, 'Charlie', 45000, 'HR')");

        var repo = new SubqueryRepo(_connection);

        // Act
        var results = repo.GetAboveAverageSalary().Result;

        // Assert
        Assert.IsTrue(results.Count > 0);
        // Bob的工资60000应该高于IT部门平均(55000)
        Assert.IsTrue(results.Any(e => e.Name == "Bob"));
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Phase2")]
    [TestCategory("DeepSubquery")]
    [Description("ANY操作符应该正常工作")]
    [Ignore("SQLite不支持ANY操作符 - 该特性仅在PostgreSQL, SQL Server, Oracle中可用")]
    public void Subquery_ANY_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE products (id INTEGER, price REAL)");
        _connection!.Execute("CREATE TABLE orders (id INTEGER, order_price REAL)");
        _connection.Execute("INSERT INTO products VALUES (1, 100), (2, 200)");
        _connection.Execute("INSERT INTO orders VALUES (1, 150)");

        var repo = new SubqueryRepo(_connection);

        // Act
        var results = repo.GetProductsCheaperThanAnyOrder().Result;

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(p => p.Price < 150));
    }

    #endregion
}

#region 测试Repository接口和模型

// 窗口函数Repository
public interface IWindowFunctionRepo
{
    [SqlTemplate("SELECT id, amount, ROW_NUMBER() OVER (ORDER BY id) as row_num FROM sales")]
    Task<List<SaleWithRowNum>> GetSalesWithRowNumber();

    [SqlTemplate("SELECT id, category, amount, ROW_NUMBER() OVER (PARTITION BY category ORDER BY id) as row_num FROM sales")]
    Task<List<SaleWithRowNumPartitioned>> GetSalesWithRowNumberPartitioned();

    [SqlTemplate("SELECT id, name, score, RANK() OVER (ORDER BY score DESC) as rank_num FROM scores")]
    Task<List<ScoreWithRank>> GetScoresWithRank();

    [SqlTemplate("SELECT id, amount, SUM(amount) OVER (ORDER BY id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as running_total FROM transactions")]
    Task<List<TransactionWithRunningTotal>> GetRunningTotal();
}

[RepositoryFor(typeof(IWindowFunctionRepo))]
public partial class WindowFunctionRepo(IDbConnection connection) : IWindowFunctionRepo { }

public class SaleWithRowNum
{
    public long Id { get; set; }
    public double Amount { get; set; }
    public long RowNum { get; set; }
}

public class SaleWithRowNumPartitioned
{
    public long Id { get; set; }
    public string Category { get; set; } = "";
    public double Amount { get; set; }
    public long RowNum { get; set; }
}

public class ScoreWithRank
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public long RankNum { get; set; }
}

public class TransactionWithRunningTotal
{
    public long Id { get; set; }
    public double Amount { get; set; }
    public double RunningTotal { get; set; }
}

// CTE Repository
public interface ICTERepo
{
    [SqlTemplate("WITH adults AS (SELECT * FROM users WHERE age >= 18) SELECT id, name, age FROM adults")]
    Task<List<UserAge>> GetAdultsUsingCTE();

    [SqlTemplate("WITH it_employees AS (SELECT * FROM employees WHERE department = 'IT'), high_salary AS (SELECT * FROM it_employees WHERE salary >= 50000) SELECT id, name, salary, department FROM high_salary")]
    Task<List<EmployeeInfo>> GetHighSalaryITEmployees();

    [SqlTemplate("WITH RECURSIVE category_tree(id, name, parent_id, level) AS (SELECT id, name, parent_id, 0 FROM categories WHERE parent_id IS NULL UNION ALL SELECT c.id, c.name, c.parent_id, ct.level + 1 FROM categories c JOIN category_tree ct ON c.parent_id = ct.id) SELECT id, name, parent_id FROM category_tree")]
    Task<List<CategoryNode>> GetCategoryHierarchy();
}

[RepositoryFor(typeof(ICTERepo))]
public partial class CTERepo(IDbConnection connection) : ICTERepo { }

public class UserAge
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class EmployeeInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Salary { get; set; }
    public string Department { get; set; } = "";
}

public class CategoryNode
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long? ParentId { get; set; }
}

// 高级JOIN Repository
public interface IAdvancedJoinRepo
{
    [SqlTemplate("SELECT o.id as order_id, u.name as user_name, p.name as product_name FROM orders o INNER JOIN users u ON o.user_id = u.id INNER JOIN products p ON o.product_id = p.id")]
    Task<List<OrderDetail>> GetOrderDetails();

    [SqlTemplate("SELECT u.id, u.name, COALESCE(COUNT(o.id), 0) as order_count FROM users u LEFT JOIN orders o ON u.id = o.user_id GROUP BY u.id, u.name")]
    Task<List<UserWithOrderCount>> GetUsersWithOrders();
}

[RepositoryFor(typeof(IAdvancedJoinRepo))]
public partial class AdvancedJoinRepo(IDbConnection connection) : IAdvancedJoinRepo { }

public class OrderDetail
{
    public long OrderId { get; set; }
    public string UserName { get; set; } = "";
    public string ProductName { get; set; } = "";
}

public class UserWithOrderCount
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long OrderCount { get; set; }
}

// 子查询Repository
public interface ISubqueryRepo
{
    [SqlTemplate("SELECT * FROM (SELECT * FROM (SELECT * FROM products WHERE price > 100) WHERE price > 120) WHERE price >= 150")]
    Task<List<ProductPrice>> GetExpensiveProductsThreeLevels();

    [SqlTemplate("SELECT id, name, salary, department FROM employees e WHERE salary > (SELECT AVG(salary) FROM employees WHERE department = e.department)")]
    Task<List<EmployeeInfo>> GetAboveAverageSalary();

    [SqlTemplate("SELECT id, price FROM products WHERE price < ANY (SELECT order_price FROM orders)")]
    Task<List<ProductPrice>> GetProductsCheaperThanAnyOrder();
}

[RepositoryFor(typeof(ISubqueryRepo))]
public partial class SubqueryRepo(IDbConnection connection) : ISubqueryRepo { }

public class ProductPrice
{
    public long Id { get; set; }
    public double Price { get; set; }
    public string? Category { get; set; }
}

#endregion

// 扩展方法
public static class TestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

