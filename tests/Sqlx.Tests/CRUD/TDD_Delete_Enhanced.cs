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
/// Phase 1: DELETE操作增强测试 - 确保100%常用场景覆盖
/// 新增15个DELETE场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("CRUD")]
[TestCategory("CoveragePhase1")]
public class TDD_Delete_Enhanced
{
    private IDbConnection? _connection;
    private IDeleteEnhancedRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                customer_name TEXT NOT NULL,
                amount REAL NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT
            )");

        _repo = new DeleteEnhancedRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("DELETE single record by ID should work")]
    public async Task Delete_SingleRecord_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'Alice', 100.0, 'pending', '2025-01-01')");

        // Act
        var affected = await _repo!.DeleteByIdAsync(1);

        // Assert
        Assert.AreEqual(1, affected);
        var order = await _repo.GetByIdAsync(1);
        Assert.IsNull(order);
    }

    [TestMethod]
    [Description("DELETE with IN clause should delete multiple records")]
    public async Task Delete_WithIn_ShouldDeleteMultiple()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'Alice', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'Bob', 200.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'Charlie', 300.0, 'completed', '2025-01-01')");

        // Act - Delete specific IDs (1 and 3)
        var affected = await _repo!.DeleteByIdsAsync();

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual(2, orders[0].Id);
    }

    [TestMethod]
    [Description("DELETE with BETWEEN should delete range")]
    public async Task Delete_WithBetween_ShouldDeleteRange()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 50.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 150.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (4, 'D', 200.0, 'pending', '2025-01-01')");

        // Act - Delete orders with amount between 100-150
        var affected = await _repo!.DeleteByAmountRangeAsync(100.0, 150.0);

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(2, orders.Count);
        Assert.IsTrue(orders.Any(o => o.Amount == 50.0));
        Assert.IsTrue(orders.Any(o => o.Amount == 200.0));
    }

    [TestMethod]
    [Description("DELETE with LIKE should match patterns")]
    public async Task Delete_WithLike_ShouldMatchPattern()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'Test Order 1', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'Production Order', 200.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'Test Order 2', 300.0, 'pending', '2025-01-01')");

        // Act - Delete all test orders
        var affected = await _repo!.DeleteByNamePatternAsync("Test%");

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual("Production Order", orders[0].CustomerName);
    }

    [TestMethod]
    [Description("DELETE with greater than operator should work")]
    public async Task Delete_WithGreaterThan_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 300.0, 'pending', '2025-01-01')");

        // Act - Delete orders with amount > 150
        var affected = await _repo!.DeleteByAmountAsync(150.0);

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual(100.0, orders[0].Amount, 0.01);
    }

    [TestMethod]
    [Description("DELETE with less than operator should work")]
    public async Task Delete_WithLessThan_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 300.0, 'pending', '2025-01-01')");

        // Act - Delete orders with amount < 250
        var affected = await _repo!.DeleteByLowAmountAsync(250.0);

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual(300.0, orders[0].Amount, 0.01);
    }

    [TestMethod]
    [Description("DELETE with equals operator should work")]
    public async Task Delete_WithEquals_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'completed', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 300.0, 'pending', '2025-01-01')");

        // Act - Delete all pending orders
        var affected = await _repo!.DeleteByStatusAsync("pending");

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual("completed", orders[0].Status);
    }

    [TestMethod]
    [Description("DELETE with NOT EQUALS should work")]
    public async Task Delete_WithNotEquals_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'completed', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 300.0, 'cancelled', '2025-01-01')");

        // Act - Delete all non-completed orders
        var affected = await _repo!.DeleteNonCompletedAsync();

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual("completed", orders[0].Status);
    }

    [TestMethod]
    [Description("DELETE with AND condition should work")]
    public async Task Delete_WithAnd_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 50.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 150.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 150.0, 'completed', '2025-01-01')");

        // Act - Delete pending orders with amount > 100
        var affected = await _repo!.DeletePendingHighAmountAsync(100.0);

        // Assert
        Assert.AreEqual(1, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(2, orders.Count);
    }

    [TestMethod]
    [Description("DELETE with OR condition should work")]
    public async Task Delete_WithOr_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 50.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 150.0, 'completed', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 150.0, 'cancelled', '2025-01-01')");

        // Act - Delete cancelled OR pending orders
        var affected = await _repo!.DeleteCancelledOrPendingAsync();

        // Assert
        Assert.AreEqual(2, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual("completed", orders[0].Status);
    }

    [TestMethod]
    [Description("DELETE all records (no WHERE) should clear table")]
    public async Task Delete_AllRecords_ShouldClearTable()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'completed', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 300.0, 'cancelled', '2025-01-01')");

        // Act - Delete all
        var affected = await _repo!.DeleteAllAsync();

        // Assert
        Assert.AreEqual(3, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(0, orders.Count);
    }

    [TestMethod]
    [Description("DELETE with no matches should return 0")]
    public async Task Delete_NoMatches_ShouldReturn0()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");

        // Act - Try to delete non-existent status
        var affected = await _repo!.DeleteByStatusAsync("nonexistent");

        // Assert
        Assert.AreEqual(0, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
    }

    [TestMethod]
    [Description("DELETE non-existent ID should return 0")]
    public async Task Delete_NonExistentId_ShouldReturn0()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");

        // Act
        var affected = await _repo!.DeleteByIdAsync(999);

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [Description("DELETE with IS NULL should work")]
    public async Task Delete_WithIsNull_ShouldWork()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', NULL)");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'pending', '2025-01-01')");

        // Act - Delete orders without created_at
        var affected = await _repo!.DeleteWithoutDateAsync();

        // Assert
        Assert.AreEqual(1, affected);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual(2, orders[0].Id);
    }

    [TestMethod]
    [Description("DELETE multiple times should accumulate")]
    public async Task Delete_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        _connection!.Execute("INSERT INTO orders VALUES (1, 'A', 100.0, 'pending', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (2, 'B', 200.0, 'cancelled', '2025-01-01')");
        _connection.Execute("INSERT INTO orders VALUES (3, 'C', 300.0, 'completed', '2025-01-01')");

        // Act
        var affected1 = await _repo!.DeleteByStatusAsync("pending");
        var affected2 = await _repo.DeleteByStatusAsync("cancelled");

        // Assert
        Assert.AreEqual(1, affected1);
        Assert.AreEqual(1, affected2);
        var orders = await _repo.GetAllAsync();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual("completed", orders[0].Status);
    }
}

// Repository interface
public interface IDeleteEnhancedRepository
{
    [SqlTemplate("SELECT * FROM orders WHERE id = @id")]
    Task<EnhancedOrder?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM orders")]
    Task<List<EnhancedOrder>> GetAllAsync();

    [SqlTemplate("DELETE FROM orders WHERE id = @id")]
    Task<int> DeleteByIdAsync(long id);

    [SqlTemplate("DELETE FROM orders WHERE id IN (1, 3)")]
    Task<int> DeleteByIdsAsync();

    [SqlTemplate("DELETE FROM orders WHERE amount BETWEEN @minAmount AND @maxAmount")]
    Task<int> DeleteByAmountRangeAsync(double minAmount, double maxAmount);

    [SqlTemplate("DELETE FROM orders WHERE customer_name LIKE @pattern")]
    Task<int> DeleteByNamePatternAsync(string pattern);

    [SqlTemplate("DELETE FROM orders WHERE amount > @minAmount")]
    Task<int> DeleteByAmountAsync(double minAmount);

    [SqlTemplate("DELETE FROM orders WHERE amount < @maxAmount")]
    Task<int> DeleteByLowAmountAsync(double maxAmount);

    [SqlTemplate("DELETE FROM orders WHERE status = @status")]
    Task<int> DeleteByStatusAsync(string status);

    [SqlTemplate("DELETE FROM orders WHERE status != 'completed'")]
    Task<int> DeleteNonCompletedAsync();

    [SqlTemplate("DELETE FROM orders WHERE status = 'pending' AND amount > @minAmount")]
    Task<int> DeletePendingHighAmountAsync(double minAmount);

    [SqlTemplate("DELETE FROM orders WHERE status = 'cancelled' OR status = 'pending'")]
    Task<int> DeleteCancelledOrPendingAsync();

    [SqlTemplate("DELETE FROM orders")]
    Task<int> DeleteAllAsync();

    [SqlTemplate("DELETE FROM orders WHERE created_at IS NULL")]
    Task<int> DeleteWithoutDateAsync();
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDeleteEnhancedRepository))]
public partial class DeleteEnhancedRepository(IDbConnection connection) : IDeleteEnhancedRepository { }

// Model
public class EnhancedOrder
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = "";
    public double Amount { get; set; }
    public string Status { get; set; } = "";
    public string? CreatedAt { get; set; }
}

