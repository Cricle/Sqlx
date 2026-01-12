using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CRUD;

/// <summary>
/// TDD: CRUD操作工作流测试 - 完整业务场景
/// Phase 1.3: 8个CRUD组合工作流测试
/// </summary>
[TestClass]
public class TDD_CRUD_Workflows
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_InsertThenSelect_ShouldReturnInsertedData()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        var repo = new WorkflowOrderRepository(connection);

        // Act
        var insertedRows = repo.InsertOrderAsync("John Doe", 299.99, "2025-01-15").Result;
        var order = repo.GetByIdAsync(1).Result;

        // Assert
        Assert.AreEqual(1, insertedRows);
        Assert.IsNotNull(order);
        Assert.AreEqual("John Doe", order.CustomerName);
        Assert.AreEqual(299.99, order.TotalAmount, 0.01);
        Assert.AreEqual("2025-01-15", order.OrderDate);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_InsertUpdateSelect_ShouldReflectChanges()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        var repo = new WorkflowOrderRepository(connection);

        // Act
        // Step 1: Insert
        repo.InsertOrderAsync("Alice", 100.00, "2025-01-01").Wait();
        var orderBefore = repo.GetByIdAsync(1).Result;

        // Step 2: Update
        var updatedRows = repo.UpdateOrderAsync(1, "Alice Updated", 150.00, "2025-01-02").Result;

        // Step 3: Select again
        var orderAfter = repo.GetByIdAsync(1).Result;

        // Assert
        Assert.IsNotNull(orderBefore);
        Assert.AreEqual("Alice", orderBefore.CustomerName);
        Assert.AreEqual(100.00, orderBefore.TotalAmount);

        Assert.AreEqual(1, updatedRows);

        Assert.IsNotNull(orderAfter);
        Assert.AreEqual("Alice Updated", orderAfter.CustomerName);
        Assert.AreEqual(150.00, orderAfter.TotalAmount, 0.01);
        Assert.AreEqual("2025-01-02", orderAfter.OrderDate);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_InsertDeleteSelect_ShouldReturnNull()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        var repo = new WorkflowOrderRepository(connection);

        // Act
        // Step 1: Insert
        repo.InsertOrderAsync("Bob", 200.00, "2025-01-01").Wait();
        var orderBefore = repo.GetByIdAsync(1).Result;

        // Step 2: Delete
        var deletedRows = repo.DeleteOrderAsync(1).Result;

        // Step 3: Try to select
        var orderAfter = repo.GetByIdAsync(1).Result;

        // Assert
        Assert.IsNotNull(orderBefore);
        Assert.AreEqual("Bob", orderBefore.CustomerName);

        Assert.AreEqual(1, deletedRows);
        Assert.IsNull(orderAfter);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_BatchInsertThenCount_ShouldMatchCount()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        var repo = new WorkflowOrderRepository(connection);

        // Act
        // 批量插入10条记录
        for (int i = 1; i <= 10; i++)
        {
            repo.InsertOrderAsync($"Customer {i}", i * 100.0, "2025-01-01").Wait();
        }

        var count = repo.CountAsync().Result;
        var allOrders = repo.GetAllAsync().Result;

        // Assert
        Assert.AreEqual(10, count);
        Assert.AreEqual(10, allOrders.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_SelectUpdateVerify_CompleteCycle()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        connection.Execute(@"
            INSERT INTO orders (customer_name, total_amount, order_date) VALUES
            ('Customer 1', 100, '2025-01-01'),
            ('Customer 2', 200, '2025-01-02'),
            ('Customer 3', 300, '2025-01-03')");

        var repo = new WorkflowOrderRepository(connection);

        // Act
        // Step 1: Select by condition
        var ordersToUpdate = repo.GetByAmountRangeAsync(150, 350).Result;
        Assert.AreEqual(2, ordersToUpdate.Count); // Customer 2 and 3

        // Step 2: Update those orders
        foreach (var order in ordersToUpdate)
        {
            repo.UpdateOrderAsync(order.Id, order.CustomerName + " (Updated)",
                order.TotalAmount * 1.1, order.OrderDate).Wait();
        }

        // Step 3: Verify updates
        var order2After = repo.GetByIdAsync(2).Result;
        var order3After = repo.GetByIdAsync(3).Result;

        // Assert
        Assert.IsNotNull(order2After);
        Assert.IsTrue(order2After.CustomerName.Contains("(Updated)"));
        Assert.AreEqual(220, order2After.TotalAmount, 0.01); // 200 * 1.1

        Assert.IsNotNull(order3After);
        Assert.IsTrue(order3After.CustomerName.Contains("(Updated)"));
        Assert.AreEqual(330, order3After.TotalAmount, 0.01); // 300 * 1.1

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_MultipleInsertsThenSelectAll_ShouldReturnAll()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        var repo = new WorkflowOrderRepository(connection);

        // Act
        var names = new[] { "Alice", "Bob", "Charlie", "David", "Eve" };
        foreach (var name in names)
        {
            repo.InsertOrderAsync(name, 100, "2025-01-01").Wait();
        }

        var allOrders = repo.GetAllAsync().Result;

        // Assert
        Assert.AreEqual(5, allOrders.Count);
        for (int i = 0; i < names.Length; i++)
        {
            Assert.AreEqual(names[i], allOrders[i].CustomerName);
        }

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_ConditionalUpdate_ShouldOnlyAffectMatching()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        connection.Execute(@"
            INSERT INTO orders (customer_name, total_amount, order_date) VALUES
            ('Customer 1', 50, '2025-01-01'),
            ('Customer 2', 100, '2025-01-02'),
            ('Customer 3', 150, '2025-01-03'),
            ('Customer 4', 200, '2025-01-04')");

        var repo = new WorkflowOrderRepository(connection);

        // Act - 只更新金额<100的订单
        var ordersToUpdate = repo.GetByAmountRangeAsync(0, 99).Result;
        Assert.AreEqual(1, ordersToUpdate.Count); // 只有Customer 1

        foreach (var order in ordersToUpdate)
        {
            repo.UpdateOrderAsync(order.Id, order.CustomerName, 999, order.OrderDate).Wait();
        }

        // Assert - 验证只有一条被更新
        var order1 = repo.GetByIdAsync(1).Result;
        var order2 = repo.GetByIdAsync(2).Result;

        Assert.IsNotNull(order1);
        Assert.AreEqual(999, order1.TotalAmount); // 已更新

        Assert.IsNotNull(order2);
        Assert.AreEqual(100, order2.TotalAmount); // 未更新

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CRUD")]
    [TestCategory("Workflow")]
    [TestCategory("Phase1")]
    public void Workflow_ConditionalDelete_ShouldOnlyDeleteMatching()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                total_amount REAL NOT NULL,
                order_date TEXT NOT NULL
            )");

        connection.Execute(@"
            INSERT INTO orders (customer_name, total_amount, order_date) VALUES
            ('Customer 1', 50, '2025-01-01'),
            ('Customer 2', 100, '2025-01-02'),
            ('Customer 3', 150, '2025-01-03')");

        var repo = new WorkflowOrderRepository(connection);

        // Act - 删除金额<100的订单
        var ordersToDelete = repo.GetByAmountRangeAsync(0, 99).Result;
        Assert.AreEqual(1, ordersToDelete.Count);

        foreach (var order in ordersToDelete)
        {
            repo.DeleteOrderAsync(order.Id).Wait();
        }

        // Assert
        var allOrders = repo.GetAllAsync().Result;
        Assert.AreEqual(2, allOrders.Count); // 剩余2条
        Assert.IsTrue(allOrders.All(o => o.TotalAmount >= 100));

        connection.Dispose();
    }
}

#region Test Models and Repositories

public class WorkflowOrder
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = "";
    public double TotalAmount { get; set; }
    public string OrderDate { get; set; } = "";
}

public interface IWorkflowOrderRepository
{
    // INSERT
    [SqlTemplate("INSERT INTO orders (customer_name, total_amount, order_date) VALUES (@customerName, @totalAmount, @orderDate)")]
    Task<int> InsertOrderAsync(string customerName, double totalAmount, string orderDate);

    // SELECT
    [SqlTemplate("SELECT id, customer_name, total_amount, order_date FROM orders WHERE id = @id")]
    Task<WorkflowOrder?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, customer_name, total_amount, order_date FROM orders")]
    Task<List<WorkflowOrder>> GetAllAsync();

    [SqlTemplate("SELECT id, customer_name, total_amount, order_date FROM orders WHERE total_amount BETWEEN @minAmount AND @maxAmount")]
    Task<List<WorkflowOrder>> GetByAmountRangeAsync(double minAmount, double maxAmount);

    [SqlTemplate("SELECT COUNT(*) FROM orders")]
    Task<int> CountAsync();

    // UPDATE
    [SqlTemplate("UPDATE orders SET customer_name = @customerName, total_amount = @totalAmount, order_date = @orderDate WHERE id = @id")]
    Task<int> UpdateOrderAsync(long id, string customerName, double totalAmount, string orderDate);

    // DELETE
    [SqlTemplate("DELETE FROM orders WHERE id = @id")]
    Task<int> DeleteOrderAsync(long id);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IWorkflowOrderRepository))]
public partial class WorkflowOrderRepository : IWorkflowOrderRepository
{
    private readonly IDbConnection _connection;

    public WorkflowOrderRepository(IDbConnection connection)
    {
        _connection = connection;
    }
}

#endregion


