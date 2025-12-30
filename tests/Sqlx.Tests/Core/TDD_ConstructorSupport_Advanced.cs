// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_Advanced.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 高级测试实体 ====================

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
}

// ==================== 场景7: 事务支持 ====================

public partial interface ITransactionRepo
{
    [SqlTemplate("INSERT INTO products (name, price, stock, created_at) VALUES (@name, @price, @stock, @createdAt)")]
    [ReturnInsertedId]
    Task<long> InsertProductAsync(string name, decimal price, int stock, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("UPDATE products SET stock = stock - @quantity WHERE id = @id AND stock >= @quantity")]
    Task<int> DecrementStockAsync(long id, int quantity, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM products WHERE id = @id")]
    Task<Product?> GetProductAsync(long id, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITransactionRepo))]
public partial class TransactionRepo(DbConnection connection, DbTransaction? transaction = null)
    : ITransactionRepo
{
    public DbTransaction? CurrentTransaction => transaction;
}

// ==================== 场景8: 复杂查询支持 ====================

public partial interface IComplexQueryRepo
{
    [SqlTemplate(@"
        SELECT {{columns}} FROM users
        WHERE age >= @minAge
        ORDER BY age DESC
        LIMIT @limit")]
    Task<List<ConstructorUser>> SearchUsersAsync(
        int minAge,
        int limit = 10,
        CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM users WHERE age >= @minAge")]
    Task<int> CountUsersAboveAgeAsync(int minAge, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM users")]
    Task<int> CountAllUsersAsync(CancellationToken ct = default);

    [SqlTemplate(@"
        SELECT age, COUNT(*) as count
        FROM users
        GROUP BY age
        HAVING COUNT(*) >= @minCount
        ORDER BY count DESC")]
    Task<List<Dictionary<string, object>>> GetAgeDistributionAsync(int minCount = 1, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IComplexQueryRepo))]
public partial class ComplexQueryRepo(DbConnection connection) : IComplexQueryRepo
{
}

// ==================== 场景9: NULL值处理 ====================

public partial interface INullHandlingRepo
{
    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    [ReturnInsertedId]
    Task<long> InsertWithNullAsync(string name, int age, string? email, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM users WHERE email IS NULL")]
    Task<List<ConstructorUser>> GetUsersWithoutEmailAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM users WHERE email IS NOT NULL")]
    Task<List<ConstructorUser>> GetUsersWithEmailAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE users SET email = @email WHERE id = @id")]
    Task<int> UpdateEmailAsync(long id, string? email, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(INullHandlingRepo))]
public partial class NullHandlingRepo(DbConnection connection) : INullHandlingRepo
{
}

// ==================== 场景10: 批量操作 ====================

public partial interface IBatchOperationRepo
{
    [SqlTemplate("DELETE FROM users WHERE age < @minAge")]
    Task<int> DeleteByAgeAsync(int minAge, CancellationToken ct = default);

    [SqlTemplate("UPDATE users SET age = age + 1 WHERE age < @maxAge")]
    Task<int> IncrementAgeAsync(int maxAge, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM users WHERE age BETWEEN @minAge AND @maxAge")]
    Task<int> CountInAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchOperationRepo))]
public partial class BatchOperationRepo(DbConnection connection) : IBatchOperationRepo
{
}

// ==================== 场景11: 只读仓储 ====================

public partial interface IReadOnlyRepo
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<ConstructorUser?> GetAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM users")]
    Task<List<ConstructorUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM users")]
    Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM users ORDER BY age DESC LIMIT 1")]
    Task<ConstructorUser?> GetOldestAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IReadOnlyRepo))]
public partial class ReadOnlyRepo(DbConnection connection) : IReadOnlyRepo
{
    // 只读仓储，不提供修改方法
}

// ==================== 场景12: 多表关联（模拟） ====================

public partial interface IMultiTableRepo
{
    [SqlTemplate(@"
        INSERT INTO orders (user_id, total_amount, status)
        VALUES (@userId, @amount, @status)")]
    [ReturnInsertedId]
    Task<long> CreateOrderAsync(long userId, decimal amount, string status, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM orders WHERE user_id = @userId")]
    Task<List<Order>> GetUserOrdersAsync(long userId, CancellationToken ct = default);

    [SqlTemplate("SELECT COALESCE(SUM(total_amount), 0) FROM orders WHERE user_id = @userId AND status = @status")]
    Task<decimal> GetTotalAmountAsync(long userId, string status, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMultiTableRepo))]
public partial class MultiTableRepo(DbConnection connection) : IMultiTableRepo
{
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数和有参构造函数的高级测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_Advanced : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_Advanced()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                email TEXT
            );

            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price DECIMAL(10,2) NOT NULL,
                stock INTEGER NOT NULL,
                created_at TEXT NOT NULL
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                total_amount DECIMAL(10,2) NOT NULL,
                status TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== 事务测试 ====================

    [TestMethod]
    public async Task Transaction_Commit_ShouldPersistChanges()
    {
        // Arrange
        using var transaction = _connection.BeginTransaction();
        var repo = new TransactionRepo(_connection, transaction);

        // Act - 在事务中插入产品
        var productId = await repo.InsertProductAsync(
            "Laptop", 999.99m, 10, DateTime.UtcNow);

        // 提交事务
        transaction.Commit();

        // Assert - 验证数据已持久化
        var readRepo = new TransactionRepo(_connection);
        var product = await readRepo.GetProductAsync(productId);

        Assert.IsNotNull(product);
        Assert.AreEqual("Laptop", product.Name);
        Assert.AreEqual(999.99m, product.Price);
    }

    [TestMethod]
    public async Task Transaction_Rollback_ShouldNotPersistChanges()
    {
        // Arrange
        using var transaction = _connection.BeginTransaction();
        var repo = new TransactionRepo(_connection, transaction);

        // Act - 在事务中插入产品
        var productId = await repo.InsertProductAsync(
            "Mouse", 29.99m, 50, DateTime.UtcNow);

        // 回滚事务
        transaction.Rollback();

        // Assert - 验证数据未持久化
        var readRepo = new TransactionRepo(_connection);
        var product = await readRepo.GetProductAsync(productId);

        Assert.IsNull(product);
    }

    [TestMethod]
    public async Task Transaction_MultipleOperations_ShouldBeAtomic()
    {
        // Arrange
        using var transaction = _connection.BeginTransaction();
        var repo = new TransactionRepo(_connection, transaction);

        // Act - 插入产品并减少库存
        var productId = await repo.InsertProductAsync(
            "Keyboard", 79.99m, 20, DateTime.UtcNow);

        var updated = await repo.DecrementStockAsync(productId, 5);

        transaction.Commit();

        // Assert
        Assert.AreEqual(1, updated);

        var readRepo = new TransactionRepo(_connection);
        var product = await readRepo.GetProductAsync(productId);
        Assert.IsNotNull(product);
        Assert.AreEqual(15, product.Stock); // 20 - 5
    }

    // ==================== 复杂查询测试 ====================

    [TestMethod]
    public async Task ComplexQuery_Search_ShouldFilterAndLimit()
    {
        // Arrange
        var repo = new ComplexQueryRepo(_connection);

        // 插入测试数据
        await InsertTestUsers();

        // Act - 查询所有用户
        var allUsers = await repo.SearchUsersAsync(minAge: 0, limit: 100);

        // Act - 限制年龄
        var filteredUsers = await repo.SearchUsersAsync(minAge: 30, limit: 10);

        // Assert
        Assert.IsTrue(allUsers.Count >= 3);
        Assert.IsTrue(filteredUsers.Count <= allUsers.Count);
        Assert.IsTrue(filteredUsers.All(u => u.Age >= 30));
    }

    [TestMethod]
    public async Task ComplexQuery_Count_ShouldReturnCorrect()
    {
        // Arrange
        var repo = new ComplexQueryRepo(_connection);
        await InsertTestUsers();

        // Act
        var totalCount = await repo.CountAllUsersAsync();
        var filteredCount = await repo.CountUsersAboveAgeAsync(30);

        // Assert
        Assert.IsTrue(totalCount >= filteredCount);
        Assert.AreEqual(3, totalCount);
        Assert.IsTrue(filteredCount >= 1);
    }

    [TestMethod]
    public async Task ComplexQuery_AgeDistribution_ShouldGroupCorrectly()
    {
        // Arrange
        var repo = new ComplexQueryRepo(_connection);

        // 插入多个相同年龄的用户
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, age, email) VALUES
            ('User1', 25, 'user1@test.com'),
            ('User2', 25, 'user2@test.com'),
            ('User3', 30, 'user3@test.com'),
            ('User4', 30, 'user4@test.com'),
            ('User5', 30, 'user5@test.com')
        ";
        cmd.ExecuteNonQuery();

        // Act
        var distribution = await repo.GetAgeDistributionAsync(minCount: 2);

        // Assert
        Assert.IsTrue(distribution.Count >= 1); // 至少有一个年龄组满足条件
        Assert.IsTrue(distribution.All(d => d.ContainsKey("age")));
        Assert.IsTrue(distribution.All(d => d.ContainsKey("count")));
    }

    // ==================== NULL值处理测试 ====================

    [TestMethod]
    public async Task NullHandling_InsertWithNull_ShouldSucceed()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);

        // Act
        var userId = await repo.InsertWithNullAsync("John", 30, null);

        // Assert
        Assert.IsTrue(userId > 0);

        var usersWithoutEmail = await repo.GetUsersWithoutEmailAsync();
        Assert.AreEqual(1, usersWithoutEmail.Count);
        Assert.AreEqual("John", usersWithoutEmail[0].Name);
    }

    [TestMethod]
    public async Task NullHandling_UpdateToNull_ShouldSucceed()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);
        var userId = await repo.InsertWithNullAsync("Jane", 28, "jane@test.com");

        // Act - 更新为NULL
        var updated = await repo.UpdateEmailAsync(userId, null);

        // Assert
        Assert.AreEqual(1, updated);

        var usersWithoutEmail = await repo.GetUsersWithoutEmailAsync();
        Assert.IsTrue(usersWithoutEmail.Any(u => u.Id == userId));
    }

    [TestMethod]
    public async Task NullHandling_QueryWithNull_ShouldFilterCorrectly()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);
        await repo.InsertWithNullAsync("User1", 25, "user1@test.com");
        await repo.InsertWithNullAsync("User2", 30, null);
        await repo.InsertWithNullAsync("User3", 35, "user3@test.com");

        // Act
        var withEmail = await repo.GetUsersWithEmailAsync();
        var withoutEmail = await repo.GetUsersWithoutEmailAsync();

        // Assert
        Assert.AreEqual(2, withEmail.Count);
        Assert.AreEqual(1, withoutEmail.Count);
        Assert.AreEqual("User2", withoutEmail[0].Name);
    }

    // ==================== 批量操作测试 ====================

    [TestMethod]
    public async Task BatchOperation_DeleteByCondition_ShouldRemoveMultiple()
    {
        // Arrange
        var repo = new BatchOperationRepo(_connection);
        await InsertTestUsers();

        // Act
        var deleted = await repo.DeleteByAgeAsync(minAge: 30);

        // Assert
        Assert.IsTrue(deleted >= 1);

        var remaining = await repo.CountInAgeRangeAsync(0, 100);
        Assert.IsTrue(remaining < 3);
    }

    [TestMethod]
    public async Task BatchOperation_UpdateMultiple_ShouldModifyAll()
    {
        // Arrange
        var repo = new BatchOperationRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO users (name, age, email) VALUES ('Young1', 20, 'y1@test.com'), ('Young2', 22, 'y2@test.com')";
        cmd.ExecuteNonQuery();

        // Act
        var updated = await repo.IncrementAgeAsync(maxAge: 25);

        // Assert
        Assert.AreEqual(2, updated);

        var count = await repo.CountInAgeRangeAsync(21, 23);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task BatchOperation_CountInRange_ShouldReturnCorrect()
    {
        // Arrange
        var repo = new BatchOperationRepo(_connection);
        await InsertTestUsers();

        // Act
        var count = await repo.CountInAgeRangeAsync(25, 35);

        // Assert
        Assert.IsTrue(count > 0);
    }

    // ==================== 只读仓储测试 ====================

    [TestMethod]
    public async Task ReadOnly_GetAll_ShouldReturnAllRecords()
    {
        // Arrange
        var repo = new ReadOnlyRepo(_connection);
        await InsertTestUsers();

        // Act
        var users = await repo.GetAllAsync();

        // Assert
        Assert.IsTrue(users.Count >= 3);
    }

    [TestMethod]
    public async Task ReadOnly_Count_ShouldReturnCorrectCount()
    {
        // Arrange
        var repo = new ReadOnlyRepo(_connection);
        await InsertTestUsers();

        // Act
        var count = await repo.CountAsync();

        // Assert
        Assert.IsTrue(count >= 3);
    }

    [TestMethod]
    public async Task ReadOnly_GetOldest_ShouldReturnHighestAge()
    {
        // Arrange
        var repo = new ReadOnlyRepo(_connection);
        await InsertTestUsers();

        // Act
        var oldest = await repo.GetOldestAsync();

        // Assert
        Assert.IsNotNull(oldest);
        Assert.IsTrue(oldest.Age >= 25);
    }

    // ==================== 多表关联测试 ====================

    [TestMethod]
    public async Task MultiTable_CreateOrder_ShouldSucceed()
    {
        // Arrange
        var repo = new MultiTableRepo(_connection);

        // 先创建用户
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO users (name, age, email) VALUES ('Customer', 30, 'customer@test.com')";
        cmd.ExecuteNonQuery();
        var userId = 1L;

        // Act
        var orderId = await repo.CreateOrderAsync(userId, 199.99m, "Pending");

        // Assert
        Assert.IsTrue(orderId > 0);
    }

    [TestMethod]
    public async Task MultiTable_GetUserOrders_ShouldReturnOrders()
    {
        // Arrange
        var repo = new MultiTableRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, age, email) VALUES ('Customer', 30, 'customer@test.com');
            INSERT INTO orders (user_id, total_amount, status) VALUES (1, 99.99, 'Completed');
            INSERT INTO orders (user_id, total_amount, status) VALUES (1, 199.99, 'Pending');
        ";
        cmd.ExecuteNonQuery();

        // Act
        var orders = await repo.GetUserOrdersAsync(1);

        // Assert
        Assert.AreEqual(2, orders.Count);
    }

    [TestMethod]
    public async Task MultiTable_GetTotalAmount_ShouldCalculateSum()
    {
        // Arrange
        var repo = new MultiTableRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, age, email) VALUES ('Customer', 30, 'customer@test.com');
            INSERT INTO orders (user_id, total_amount, status) VALUES (1, 100, 'Completed');
            INSERT INTO orders (user_id, total_amount, status) VALUES (1, 200, 'Completed');
            INSERT INTO orders (user_id, total_amount, status) VALUES (1, 50, 'Pending');
        ";
        cmd.ExecuteNonQuery();

        // Act
        var total = await repo.GetTotalAmountAsync(1, "Completed");

        // Assert
        Assert.AreEqual(300m, total);
    }

    // ==================== 并发测试 ====================

    [TestMethod]
    public async Task Concurrent_MultipleRepos_ShouldWorkIndependently()
    {
        // Arrange
        var repo1 = new ReadOnlyRepo(_connection);
        var repo2 = new ReadOnlyRepo(_connection);
        var repo3 = new NullHandlingRepo(_connection);

        // Act - 并发执行
        var task1 = repo3.InsertWithNullAsync("Concurrent1", 25, "c1@test.com");
        var task2 = repo3.InsertWithNullAsync("Concurrent2", 30, null);
        var task3 = repo3.InsertWithNullAsync("Concurrent3", 35, "c3@test.com");

        await Task.WhenAll(task1, task2, task3);

        // Assert
        var count = await repo1.CountAsync();
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public async Task Concurrent_SameRepo_MultipleOperations_ShouldSucceed()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);

        // Act - 多个并发操作
        var tasks = Enumerable.Range(1, 10)
            .Select(i => repo.InsertWithNullAsync($"User{i}", 20 + i, $"user{i}@test.com"))
            .ToArray();

        var ids = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10, ids.Length);
        Assert.IsTrue(ids.All(id => id > 0));
        Assert.AreEqual(10, ids.Distinct().Count()); // 所有ID都不同
    }

    // ==================== 错误处理测试 ====================

    [TestMethod]
    public async Task ErrorHandling_InvalidData_ShouldThrow()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);

        // Act & Assert - 插入NULL作为必填字段应该失败
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            // SQLite会抛出异常，因为name是NOT NULL
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO users (name, age) VALUES (NULL, 25)";
            await cmd.ExecuteNonQueryAsync();
        });
    }

    [TestMethod]
    public async Task ErrorHandling_NonExistentRecord_ShouldReturnNull()
    {
        // Arrange
        var repo = new ReadOnlyRepo(_connection);

        // Act
        var user = await repo.GetAsync(999999);

        // Assert
        Assert.IsNull(user);
    }

    // ==================== 边界测试 ====================

    [TestMethod]
    public async Task Boundary_EmptyTable_QueryShouldReturnEmpty()
    {
        // Arrange
        var repo = new ReadOnlyRepo(_connection);

        // Act
        var users = await repo.GetAllAsync();
        var count = await repo.CountAsync();

        // Assert
        Assert.AreEqual(0, users.Count);
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task Boundary_LargeStringValue_ShouldHandle()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);
        var longName = new string('A', 1000);

        // Act
        var userId = await repo.InsertWithNullAsync(longName, 25, "test@test.com");

        // Assert
        Assert.IsTrue(userId > 0);

        var users = await repo.GetUsersWithEmailAsync();
        Assert.IsTrue(users.Any(u => u.Name == longName));
    }

    [TestMethod]
    public async Task Boundary_SpecialCharacters_ShouldEscape()
    {
        // Arrange
        var repo = new NullHandlingRepo(_connection);
        var specialName = "O'Brien \"The Great\"";

        // Act
        var userId = await repo.InsertWithNullAsync(specialName, 30, "obrien@test.com");

        // Assert
        Assert.IsTrue(userId > 0);

        var users = await repo.GetUsersWithEmailAsync();
        Assert.IsTrue(users.Any(u => u.Name == specialName));
    }

    // ==================== Helper Methods ====================

    private async Task InsertTestUsers()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (name, age, email) VALUES
            ('Alice', 25, 'alice@test.com'),
            ('Bob', 30, 'bob@test.com'),
            ('Charlie', 35, 'charlie@test.com')
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}

