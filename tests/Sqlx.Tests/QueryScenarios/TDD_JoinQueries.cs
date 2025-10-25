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
    [Ignore("TODO: JOIN queries require additional generator support")]
    [TestCategory("TDD-Red")]
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
    [Ignore("TODO: JOIN queries require additional generator support")]
    [TestCategory("TDD-Red")]
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
    [Ignore("TODO: JOIN queries require additional generator support")]
    [TestCategory("TDD-Red")]
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
    [Ignore("TODO: JOIN queries require additional generator support")]
    [TestCategory("TDD-Red")]
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

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IJoinTestRepository))]
public partial class JoinTestRepository(IDbConnection connection) : IJoinTestRepository { }

public interface IJoinTestRepository
{
    [SqlTemplate(@"
        SELECT o.id as OrderId, u.name as UserName, o.amount as Amount
        FROM orders o
        INNER JOIN users u ON o.user_id = u.id
    ")]
    Task<List<JoinOrderWithUser>> GetOrdersWithUserNamesAsync();
    
    [SqlTemplate(@"
        SELECT u.id as UserId, u.name as UserName, COUNT(o.id) as OrderCount
        FROM users u
        LEFT JOIN orders o ON u.id = o.user_id
        GROUP BY u.id, u.name
    ")]
    Task<List<JoinUserWithOrderCount>> GetUsersWithOrderCountAsync();
    
    [SqlTemplate(@"
        SELECT o.id as OrderId, u.name as UserName, p.name as ProductName, 
               o.quantity as Quantity, p.price as Price
        FROM orders o
        INNER JOIN users u ON o.user_id = u.id
        INNER JOIN products p ON o.product_id = p.id
    ")]
    Task<List<JoinOrderDetail>> GetOrderDetailsAsync();
    
    [SqlTemplate(@"
        SELECT o.id as OrderId, u.name as UserName, o.amount as Amount
        FROM orders o
        INNER JOIN users u ON o.user_id = u.id
        WHERE o.amount > @minAmount
    ")]
    Task<List<JoinOrderWithUser>> GetExpensiveOrdersAsync(int minAmount);
}


