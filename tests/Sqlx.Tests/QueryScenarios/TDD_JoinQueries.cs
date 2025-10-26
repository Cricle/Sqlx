using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.QueryScenarios;

/// <summary>
/// TDD: JOIN查询测试
/// 验证INNER JOIN, LEFT JOIN等多表关联查询
/// </summary>
[TestClass]
public class TDD_JoinQueries
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Core")]
    public void Join_InnerJoin_ShouldReturnMatchingRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                amount INTEGER NOT NULL
            )");

        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        connection.Execute("INSERT INTO users (name) VALUES ('Bob')");
        connection.Execute("INSERT INTO users (name) VALUES ('Charlie')");

        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (1, 100)");
        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (1, 200)");
        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (2, 300)");

        var repo = new JoinTestRepository(connection);

        // Act: Get all orders with user names
        var results = repo.GetOrdersWithUserNamesAsync().Result;

        // Assert: Should get 3 orders (Charlie has no orders, so excluded)
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.Any(r => r.UserName == "Alice" && r.Amount == 100));
        Assert.IsTrue(results.Any(r => r.UserName == "Bob" && r.Amount == 300));
        Assert.IsFalse(results.Any(r => r.UserName == "Charlie"));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Core")]
    public void Join_LeftJoin_ShouldReturnAllLeftRecords()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER,
                amount INTEGER NOT NULL
            )");

        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        connection.Execute("INSERT INTO users (name) VALUES ('Bob')");
        connection.Execute("INSERT INTO users (name) VALUES ('Charlie')");

        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (1, 100)");
        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (2, 200)");
        // Charlie has no orders

        var repo = new JoinTestRepository(connection);

        // Act: Get all users with their order count
        var results = repo.GetUsersWithOrderCountAsync().Result;

        // Assert: Should get all 3 users
        Assert.AreEqual(3, results.Count);

        var alice = results.First(r => r.UserName == "Alice");
        Assert.AreEqual(1, alice.OrderCount);

        var bob = results.First(r => r.UserName == "Bob");
        Assert.AreEqual(1, bob.OrderCount);

        var charlie = results.First(r => r.UserName == "Charlie");
        Assert.AreEqual(0, charlie.OrderCount);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Multiple")]
    public void Join_MultipleJoins_ShouldWorkCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price INTEGER NOT NULL
            )");

        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Widget', 10)");
        connection.Execute("INSERT INTO products (name, price) VALUES ('Gadget', 20)");
        connection.Execute("INSERT INTO orders (user_id, product_id, quantity) VALUES (1, 1, 5)");
        connection.Execute("INSERT INTO orders (user_id, product_id, quantity) VALUES (1, 2, 3)");

        var repo = new JoinTestRepository(connection);

        // Act: Get order details with user and product info
        var results = repo.GetOrderDetailsAsync().Result;

        // Assert
        Assert.AreEqual(2, results.Count);

        var order1 = results.First(r => r.ProductName == "Widget");
        Assert.AreEqual("Alice", order1.UserName);
        Assert.AreEqual(5, order1.Quantity);
        Assert.AreEqual(10, order1.Price);

        var order2 = results.First(r => r.ProductName == "Gadget");
        Assert.AreEqual("Alice", order2.UserName);
        Assert.AreEqual(3, order2.Quantity);
        Assert.AreEqual(20, order2.Price);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Condition")]
    public void Join_WithWhereClause_ShouldFilterCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                amount INTEGER NOT NULL
            )");

        connection.Execute("INSERT INTO users (name) VALUES ('Alice')");
        connection.Execute("INSERT INTO users (name) VALUES ('Bob')");

        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (1, 50)");
        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (1, 150)");
        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (2, 200)");

        var repo = new JoinTestRepository(connection);

        // Act: Get orders with amount > 100
        var results = repo.GetExpensiveOrdersAsync(100).Result;

        // Assert: Should get 2 orders
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(r => r.Amount > 100));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_CrossJoin_ShouldReturnCartesianProduct()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE colors (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE sizes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute("INSERT INTO colors (name) VALUES ('Red'), ('Blue')");
        connection.Execute("INSERT INTO sizes (name) VALUES ('S'), ('M'), ('L')");

        var repo = new JoinTestRepository(connection);

        // Act: CROSS JOIN produces all combinations
        var results = repo.GetColorSizeCombinationsAsync().Result;

        // Assert: 2 colors × 3 sizes = 6 combinations
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual(3, results.Count(r => r.ColorName == "Red")); // Red × 3 sizes
        Assert.AreEqual(3, results.Count(r => r.ColorName == "Blue")); // Blue × 3 sizes
        Assert.AreEqual(2, results.Count(r => r.SizeName == "S")); // S × 2 colors

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_SelfJoin_ShouldJoinTableToItself()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE employees (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                manager_id INTEGER
            )");

        connection.Execute("INSERT INTO employees (name, manager_id) VALUES ('CEO', NULL)");
        connection.Execute("INSERT INTO employees (name, manager_id) VALUES ('Manager1', 1)");
        connection.Execute("INSERT INTO employees (name, manager_id) VALUES ('Manager2', 1)");
        connection.Execute("INSERT INTO employees (name, manager_id) VALUES ('Employee1', 2)");
        connection.Execute("INSERT INTO employees (name, manager_id) VALUES ('Employee2', 2)");

        var repo = new JoinTestRepository(connection);

        // Act: Self join to get employee with manager name
        var results = repo.GetEmployeesWithManagerAsync().Result;

        // Assert
        Assert.AreEqual(4, results.Count); // CEO没有manager，所以4条记录
        var emp1 = results.First(r => r.EmployeeName == "Manager1");
        Assert.AreEqual("CEO", emp1.ManagerName);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_MultipleLeftJoins_ShouldReturnAllLeftRecords()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER,
                total REAL
            )");

        connection.Execute(@"
            CREATE TABLE shipments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER,
                tracking TEXT
            )");

        connection.Execute("INSERT INTO customers (name) VALUES ('Alice'), ('Bob'), ('Charlie')");
        connection.Execute("INSERT INTO orders (customer_id, total) VALUES (1, 100), (1, 200)");
        connection.Execute("INSERT INTO shipments (order_id, tracking) VALUES (1, 'TRACK001')");

        var repo = new JoinTestRepository(connection);

        // Act: Multiple LEFT JOINs should return all customers
        var results = repo.GetCustomerOrderShipmentAsync().Result;

        // Assert: 所有3个customer都应该返回
        Assert.IsTrue(results.Count >= 3);
        Assert.IsTrue(results.Any(r => r.CustomerName == "Charlie")); // Charlie没有订单也应该返回

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_WithSubquery_ShouldWork()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                amount INTEGER NOT NULL
            )");

        connection.Execute("INSERT INTO users (name) VALUES ('Alice'), ('Bob'), ('Charlie')");
        connection.Execute("INSERT INTO orders (user_id, amount) VALUES (1, 100), (1, 200), (2, 50)");

        var repo = new JoinTestRepository(connection);

        // Act: JOIN with subquery (high value users)
        var results = repo.GetHighValueUserOrdersAsync(100).Result;

        // Assert: 只有总消费>100的用户的订单
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(r => r.UserName == "Alice")); // 只有Alice总消费>100

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_WithGroupBy_ShouldAggregateCorrectly()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                category_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                price REAL NOT NULL
            )");

        connection.Execute("INSERT INTO categories (name) VALUES ('Electronics'), ('Books')");
        connection.Execute("INSERT INTO products (category_id, name, price) VALUES (1, 'Laptop', 1000), (1, 'Mouse', 50), (2, 'Book1', 20)");

        var repo = new JoinTestRepository(connection);

        // Act: JOIN with GROUP BY
        var results = repo.GetCategoryStatsAsync().Result;

        // Assert
        Assert.AreEqual(2, results.Count);
        var electronics = results.First(r => r.CategoryName == "Electronics");
        Assert.AreEqual(2, electronics.ProductCount);
        Assert.AreEqual(1050, electronics.TotalValue, 0.01);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_WithHaving_ShouldFilterGroups()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                category_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                price REAL NOT NULL
            )");

        connection.Execute("INSERT INTO categories (name) VALUES ('Electronics'), ('Books'), ('Toys')");
        connection.Execute("INSERT INTO products (category_id, name, price) VALUES (1, 'Laptop', 1000), (1, 'Mouse', 50), (2, 'Book1', 20), (3, 'Toy1', 10)");

        var repo = new JoinTestRepository(connection);

        // Act: JOIN with GROUP BY and HAVING
        var results = repo.GetHighValueCategoriesAsync(500).Result;

        // Assert: 只返回总价值>500的类别
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Electronics", results[0].CategoryName);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Join")]
    [TestCategory("Phase2")]
    public void Join_MultipleOnConditions_ShouldWork()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE table1 (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key1 INTEGER NOT NULL,
                key2 INTEGER NOT NULL,
                value TEXT NOT NULL
            )");

        connection.Execute(@"
            CREATE TABLE table2 (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key1 INTEGER NOT NULL,
                key2 INTEGER NOT NULL,
                data TEXT NOT NULL
            )");

        connection.Execute("INSERT INTO table1 (key1, key2, value) VALUES (1, 1, 'A'), (1, 2, 'B'), (2, 1, 'C')");
        connection.Execute("INSERT INTO table2 (key1, key2, data) VALUES (1, 1, 'X'), (1, 2, 'Y'), (2, 2, 'Z')");

        var repo = new JoinTestRepository(connection);

        // Act: JOIN with multiple ON conditions
        var results = repo.GetJoinWithMultipleKeysAsync().Result;

        // Assert: 只有key1和key2都匹配的记录
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.Value == "A" && r.Data == "X"));
        Assert.IsTrue(results.Any(r => r.Value == "B" && r.Data == "Y"));

        connection.Dispose();
    }
}

// Test models
public class JoinOrderWithUser
{
    public long OrderId { get; set; }
    public string UserName { get; set; } = "";
    public int Amount { get; set; }
}

public class JoinUserWithOrderCount
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public int OrderCount { get; set; }
}

public class JoinOrderDetail
{
    public long OrderId { get; set; }
    public string UserName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public int Price { get; set; }
}

// Phase 2 new models
public class ColorSizeCombination
{
    public string ColorName { get; set; } = "";
    public string SizeName { get; set; } = "";
}

public class EmployeeWithManager
{
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string? ManagerName { get; set; }
}

public class CustomerOrderShipment
{
    public string CustomerName { get; set; } = "";
    public double? OrderTotal { get; set; }
    public string? TrackingNumber { get; set; }
}

public class CategoryStats
{
    public string CategoryName { get; set; } = "";
    public int ProductCount { get; set; }
    public double TotalValue { get; set; }
}

public class JoinMultiKey
{
    public string Value { get; set; } = "";
    public string Data { get; set; } = "";
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IJoinTestRepository))]
public partial class JoinTestRepository(IDbConnection connection) : IJoinTestRepository { }

public interface IJoinTestRepository
{
    [SqlTemplate("SELECT o.id as order_id, u.name as user_name, o.amount FROM orders o INNER JOIN users u ON o.user_id = u.id")]
    Task<List<JoinOrderWithUser>> GetOrdersWithUserNamesAsync();

    [SqlTemplate("SELECT u.id as user_id, u.name as user_name, COUNT(o.id) as order_count FROM users u LEFT JOIN orders o ON u.id = o.user_id GROUP BY u.id, u.name")]
    Task<List<JoinUserWithOrderCount>> GetUsersWithOrderCountAsync();

    [SqlTemplate("SELECT o.id as order_id, u.name as user_name, p.name as product_name, o.quantity, p.price FROM orders o INNER JOIN users u ON o.user_id = u.id INNER JOIN products p ON o.product_id = p.id")]
    Task<List<JoinOrderDetail>> GetOrderDetailsAsync();

    [SqlTemplate("SELECT o.id as order_id, u.name as user_name, o.amount FROM orders o INNER JOIN users u ON o.user_id = u.id WHERE o.amount > @minAmount")]
    Task<List<JoinOrderWithUser>> GetExpensiveOrdersAsync(int minAmount);

    // Phase 2 new methods
    [SqlTemplate("SELECT c.name as color_name, s.name as size_name FROM colors c CROSS JOIN sizes s")]
    Task<List<ColorSizeCombination>> GetColorSizeCombinationsAsync();

    [SqlTemplate("SELECT e.id as employee_id, e.name as employee_name, m.name as manager_name FROM employees e LEFT JOIN employees m ON e.manager_id = m.id WHERE e.manager_id IS NOT NULL")]
    Task<List<EmployeeWithManager>> GetEmployeesWithManagerAsync();

    [SqlTemplate("SELECT c.name as customer_name, o.total as order_total, s.tracking as tracking_number FROM customers c LEFT JOIN orders o ON c.id = o.customer_id LEFT JOIN shipments s ON o.id = s.order_id")]
    Task<List<CustomerOrderShipment>> GetCustomerOrderShipmentAsync();

    [SqlTemplate("SELECT o.id as order_id, u.name as user_name, o.amount FROM users u INNER JOIN (SELECT user_id FROM orders GROUP BY user_id HAVING SUM(amount) > @minTotal) high_users ON u.id = high_users.user_id INNER JOIN orders o ON u.id = o.user_id")]
    Task<List<JoinOrderWithUser>> GetHighValueUserOrdersAsync(int minTotal);

    [SqlTemplate("SELECT c.name as category_name, COUNT(p.id) as product_count, SUM(p.price) as total_value FROM categories c INNER JOIN products p ON c.id = p.category_id GROUP BY c.id, c.name")]
    Task<List<CategoryStats>> GetCategoryStatsAsync();

    [SqlTemplate("SELECT c.name as category_name, COUNT(p.id) as product_count, SUM(p.price) as total_value FROM categories c INNER JOIN products p ON c.id = p.category_id GROUP BY c.id, c.name HAVING SUM(p.price) > @minValue")]
    Task<List<CategoryStats>> GetHighValueCategoriesAsync(double minValue);

    [SqlTemplate("SELECT t1.value, t2.data FROM table1 t1 INNER JOIN table2 t2 ON t1.key1 = t2.key1 AND t1.key2 = t2.key2")]
    Task<List<JoinMultiKey>> GetJoinWithMultipleKeysAsync();
}


