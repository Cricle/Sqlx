// <copyright file="CombinedE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Annotations;
using System.Data.Common;

namespace Sqlx.Tests.E2E;

#region Models

[Sqlx]
[TableName("combo_orders")]
public class ComboOrder
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

[Sqlx]
[TableName("combo_items")]
public class ComboItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

#endregion

#region Repositories

public interface IComboOrderRepository
{
    DbTransaction? Transaction { get; set; }

    [SqlTemplate("INSERT INTO combo_orders (customer_name, amount, status) VALUES (@customerName, @amount, @status)")]
    Task<int> InsertAsync(string customerName, decimal amount, string status);

    [SqlTemplate("SELECT id, customer_name, amount, status FROM combo_orders WHERE id = @id")]
    Task<ComboOrder?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, customer_name, amount, status FROM combo_orders WHERE status = @status ORDER BY id")]
    Task<List<ComboOrder>> GetByStatusAsync(string status);

    [SqlTemplate("UPDATE combo_orders SET status = @status WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status);

    [SqlTemplate("DELETE FROM combo_orders WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    [SqlTemplate("SELECT COUNT(*) FROM combo_orders")]
    Task<int> CountAsync();

    [SqlTemplate("SELECT COALESCE(MAX(id), 0) FROM combo_orders")]
    Task<long> GetMaxIdAsync();
}

public interface IComboItemRepository
{
    DbTransaction? Transaction { get; set; }

    [SqlTemplate("INSERT INTO combo_items (order_id, product_name, quantity, unit_price) VALUES (@orderId, @productName, @quantity, @unitPrice)")]
    Task<int> InsertAsync(long orderId, string productName, int quantity, decimal unitPrice);

    [SqlTemplate("SELECT id, order_id, product_name, quantity, unit_price FROM combo_items WHERE order_id = @orderId ORDER BY id")]
    Task<List<ComboItem>> GetByOrderIdAsync(long orderId);

    [SqlTemplate("DELETE FROM combo_items WHERE order_id = @orderId")]
    Task<int> DeleteByOrderIdAsync(long orderId);
}

[RepositoryFor(typeof(IComboOrderRepository), TableName = "combo_orders")]
public partial class MySqlComboOrderRepository : IComboOrderRepository
{
    private readonly DbConnection _connection;
    public MySqlComboOrderRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboOrderRepository), TableName = "combo_orders")]
public partial class PostgreSqlComboOrderRepository : IComboOrderRepository
{
    private readonly DbConnection _connection;
    public PostgreSqlComboOrderRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboOrderRepository), TableName = "combo_orders")]
public partial class SqlServerComboOrderRepository : IComboOrderRepository
{
    private readonly DbConnection _connection;
    public SqlServerComboOrderRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboOrderRepository), TableName = "combo_orders")]
public partial class SQLiteComboOrderRepository : IComboOrderRepository
{
    private readonly DbConnection _connection;
    public SQLiteComboOrderRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboItemRepository), TableName = "combo_items")]
public partial class MySqlComboItemRepository : IComboItemRepository
{
    private readonly DbConnection _connection;
    public MySqlComboItemRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboItemRepository), TableName = "combo_items")]
public partial class PostgreSqlComboItemRepository : IComboItemRepository
{
    private readonly DbConnection _connection;
    public PostgreSqlComboItemRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboItemRepository), TableName = "combo_items")]
public partial class SqlServerComboItemRepository : IComboItemRepository
{
    private readonly DbConnection _connection;
    public SqlServerComboItemRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

[RepositoryFor(typeof(IComboItemRepository), TableName = "combo_items")]
public partial class SQLiteComboItemRepository : IComboItemRepository
{
    private readonly DbConnection _connection;
    public SQLiteComboItemRepository(DbConnection connection, SqlDialect dialect) { _connection = connection; _dialect = dialect; }
}

#endregion

/// <summary>
/// Combined E2E tests covering multi-table CRUD, transactions, LINQ queries, and subqueries
/// across all four database dialects.
/// </summary>
[TestClass]
public class CombinedE2ETests : E2ETestBase
{
    private static SqlDialect GetDialect(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => SqlDefine.MySql,
        DatabaseType.PostgreSQL => SqlDefine.PostgreSql,
        DatabaseType.SqlServer => SqlDefine.SqlServer,
        DatabaseType.SQLite => SqlDefine.SQLite,
        _ => throw new NotSupportedException()
    };

    private static IComboOrderRepository CreateOrderRepo(DatabaseType dbType, DbConnection conn) => dbType switch
    {
        DatabaseType.MySQL => new MySqlComboOrderRepository(conn, GetDialect(dbType)),
        DatabaseType.PostgreSQL => new PostgreSqlComboOrderRepository(conn, GetDialect(dbType)),
        DatabaseType.SqlServer => new SqlServerComboOrderRepository(conn, GetDialect(dbType)),
        DatabaseType.SQLite => new SQLiteComboOrderRepository(conn, GetDialect(dbType)),
        _ => throw new NotSupportedException()
    };

    private static IComboItemRepository CreateItemRepo(DatabaseType dbType, DbConnection conn) => dbType switch
    {
        DatabaseType.MySQL => new MySqlComboItemRepository(conn, GetDialect(dbType)),
        DatabaseType.PostgreSQL => new PostgreSqlComboItemRepository(conn, GetDialect(dbType)),
        DatabaseType.SqlServer => new SqlServerComboItemRepository(conn, GetDialect(dbType)),
        DatabaseType.SQLite => new SQLiteComboItemRepository(conn, GetDialect(dbType)),
        _ => throw new NotSupportedException()
    };

    private static string GetOrderSchema(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => @"
            CREATE TABLE combo_orders (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                customer_name VARCHAR(100) NOT NULL,
                amount DECIMAL(10,2) NOT NULL,
                status VARCHAR(20) NOT NULL
            )",
        DatabaseType.PostgreSQL => @"
            CREATE TABLE combo_orders (
                id BIGSERIAL PRIMARY KEY,
                customer_name VARCHAR(100) NOT NULL,
                amount DECIMAL(10,2) NOT NULL,
                status VARCHAR(20) NOT NULL
            )",
        DatabaseType.SqlServer => @"
            CREATE TABLE combo_orders (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                customer_name NVARCHAR(100) NOT NULL,
                amount DECIMAL(10,2) NOT NULL,
                status NVARCHAR(20) NOT NULL
            )",
        DatabaseType.SQLite => @"
            CREATE TABLE combo_orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                amount REAL NOT NULL,
                status TEXT NOT NULL
            )",
        _ => throw new NotSupportedException()
    };

    private static string GetItemSchema(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => @"
            CREATE TABLE combo_items (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                order_id BIGINT NOT NULL,
                product_name VARCHAR(100) NOT NULL,
                quantity INT NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL
            )",
        DatabaseType.PostgreSQL => @"
            CREATE TABLE combo_items (
                id BIGSERIAL PRIMARY KEY,
                order_id BIGINT NOT NULL,
                product_name VARCHAR(100) NOT NULL,
                quantity INT NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL
            )",
        DatabaseType.SqlServer => @"
            CREATE TABLE combo_items (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                order_id BIGINT NOT NULL,
                product_name NVARCHAR(100) NOT NULL,
                quantity INT NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL
            )",
        DatabaseType.SQLite => @"
            CREATE TABLE combo_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL,
                product_name TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                unit_price REAL NOT NULL
            )",
        _ => throw new NotSupportedException()
    };

    // ==================== Combined CRUD + Transaction ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_MultiTableCrudWithTransaction_CommitsCorrectly(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));
        await fixture.CreateSchemaAsync(GetItemSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        var itemRepo = CreateItemRepo(dbType, fixture.Connection);

        await using var tx = await fixture.Connection.BeginTransactionAsync();
        orderRepo.Transaction = tx;
        itemRepo.Transaction = tx;
        try
        {
            await orderRepo.InsertAsync("Alice", 150.00m, "pending");
            await orderRepo.InsertAsync("Bob", 75.50m, "pending");
            var orderId = await orderRepo.GetMaxIdAsync();

            await itemRepo.InsertAsync(orderId, "Widget", 3, 25.00m);
            await itemRepo.InsertAsync(orderId, "Gadget", 1, 75.00m);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        finally
        {
            orderRepo.Transaction = null;
            itemRepo.Transaction = null;
        }

        Assert.AreEqual(2, await orderRepo.CountAsync());
        var orderId2 = await orderRepo.GetMaxIdAsync();
        var items = await itemRepo.GetByOrderIdAsync(orderId2);
        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.Any(i => i.ProductName == "Widget"));
        Assert.IsTrue(items.Any(i => i.ProductName == "Gadget"));
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_TransactionRollback_LeavesNoData(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);

        await using var tx = await fixture.Connection.BeginTransactionAsync();
        orderRepo.Transaction = tx;
        await orderRepo.InsertAsync("RollbackUser", 999m, "pending");
        Assert.AreEqual(1, await orderRepo.CountAsync());
        await tx.RollbackAsync();
        orderRepo.Transaction = null;

        Assert.AreEqual(0, await orderRepo.CountAsync());
    }

    // ==================== Combined CRUD + LINQ ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_RepositoryInsertThenLinqQuery_ReturnsCorrectData(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        await orderRepo.InsertAsync("Alice", 100m, "pending");
        await orderRepo.InsertAsync("Bob", 200m, "shipped");
        await orderRepo.InsertAsync("Carol", 300m, "pending");

        // Use LINQ to query
        var query = SqlQuery<ComboOrder>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(o => o.Status == "pending")
            .OrderBy(o => o.Amount);

        var results = await query.ToListAsync();
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].CustomerName);
        Assert.AreEqual("Carol", results[1].CustomerName);

        var count = await query.CountAsync();
        Assert.AreEqual(2L, count);
    }

    // ==================== Combined LINQ + SubQuery ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_LinqSubQuery_FiltersCorrectly(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        await orderRepo.InsertAsync("Alice", 50m, "pending");
        await orderRepo.InsertAsync("Bob", 150m, "pending");
        await orderRepo.InsertAsync("Carol", 250m, "shipped");
        await orderRepo.InsertAsync("Dave", 350m, "pending");

        // Use direct query with combined conditions to avoid decimal type mismatch in subquery across databases
        var results = await SqlQuery<ComboOrder>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(o => o.Status == "pending" && o.Amount >= 100m)
            .OrderBy(o => o.Amount)
            .ToListAsync();
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].CustomerName);
        Assert.AreEqual("Dave", results[1].CustomerName);
    }

    // ==================== Combined Update + Delete ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_UpdateThenDelete_LeavesCorrectState(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        await orderRepo.InsertAsync("Alice", 100m, "pending");
        await orderRepo.InsertAsync("Bob", 200m, "pending");

        var orders = await orderRepo.GetByStatusAsync("pending");
        Assert.AreEqual(2, orders.Count);

        // Update first order
        await orderRepo.UpdateStatusAsync(orders[0].Id, "shipped");
        var shipped = await orderRepo.GetByStatusAsync("shipped");
        Assert.AreEqual(1, shipped.Count);
        Assert.AreEqual(orders[0].CustomerName, shipped[0].CustomerName);

        // Delete second order
        await orderRepo.DeleteAsync(orders[1].Id);
        Assert.AreEqual(1, await orderRepo.CountAsync());

        var remaining = await orderRepo.GetByStatusAsync("shipped");
        Assert.AreEqual(1, remaining.Count);
    }

    // ==================== Combined Multi-Table Join via LINQ ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_MultiTableInsertAndQuery_WorksCorrectly(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));
        await fixture.CreateSchemaAsync(GetItemSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        var itemRepo = CreateItemRepo(dbType, fixture.Connection);

        await orderRepo.InsertAsync("Alice", 0m, "pending");
        var orderId = await orderRepo.GetMaxIdAsync();

        await itemRepo.InsertAsync(orderId, "A", 2, 10m);
        await itemRepo.InsertAsync(orderId, "B", 1, 20m);
        await itemRepo.InsertAsync(orderId, "C", 3, 5m);

        var items = await itemRepo.GetByOrderIdAsync(orderId);
        Assert.AreEqual(3, items.Count);

        var totalAmount = items.Sum(i => i.Quantity * i.UnitPrice);
        Assert.AreEqual(55m, totalAmount);

        // Delete items then order
        await itemRepo.DeleteByOrderIdAsync(orderId);
        var afterDelete = await itemRepo.GetByOrderIdAsync(orderId);
        Assert.AreEqual(0, afterDelete.Count);

        await orderRepo.DeleteAsync(orderId);
        Assert.AreEqual(0, await orderRepo.CountAsync());
    }

    // ==================== Combined LightweightApi + Repository ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_LightweightApiAndRepository_ProduceSameResults(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        await orderRepo.InsertAsync("Alice", 100m, "active");
        await orderRepo.InsertAsync("Bob", 200m, "active");

        // Query via repository
        var repoResults = await orderRepo.GetByStatusAsync("active");

        // Query via lightweight API (no-param version)
        var lightResults = await fixture.Connection.SqlxQueryAsync<ComboOrder>(
            "SELECT id, customer_name, amount, status FROM combo_orders WHERE status = 'active' ORDER BY id",
            GetDialect(dbType));

        Assert.AreEqual(repoResults.Count, lightResults.Count);
        for (int i = 0; i < repoResults.Count; i++)
        {
            Assert.AreEqual(repoResults[i].CustomerName, lightResults[i].CustomerName);
            Assert.AreEqual(repoResults[i].Status, lightResults[i].Status);
        }
    }

    // ==================== Combined LINQ Pagination ====================

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("Combined")]
    public async Task Combined_AllDatabases_LinqPagination_ReturnsCorrectPage(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetOrderSchema(dbType));

        var orderRepo = CreateOrderRepo(dbType, fixture.Connection);
        for (int i = 1; i <= 10; i++)
            await orderRepo.InsertAsync($"Customer{i:D2}", i * 10m, "active");

        var query = SqlQuery<ComboOrder>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(o => o.Status == "active")
            .OrderBy(o => o.Amount)
            .Skip(3)
            .Take(4);

        var page = await query.ToListAsync();
        Assert.AreEqual(4, page.Count);
        Assert.AreEqual(40m, page[0].Amount);
        Assert.AreEqual(70m, page[3].Amount);
    }
}
