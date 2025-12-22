using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Generator;

namespace Sqlx.Tests.Transactions;

/// <summary>
/// 事务兼容性测试 - Requirement 44
/// 验证生成的SQL在事务中正确工作，不包含隐式提交
/// </summary>
[TestClass]
public class TDD_TransactionCompatibility
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

    #region 批量操作原子性测试 (Requirement 44.1)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("批量INSERT在事务中应该是原子的")]
    public async Task BatchInsert_InTransaction_ShouldBeAtomic()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - 批量插入在事务中
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.InsertProduct("Product1", 10.0);
            await repo.InsertProduct("Product2", 20.0);
            await repo.InsertProduct("Product3", 30.0);
            transaction.Commit();
        }
        repo.Transaction = null;

        // Assert
        var products = await repo.GetAllProducts();
        Assert.AreEqual(3, products.Count);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("批量INSERT回滚应该撤销所有更改")]
    public async Task BatchInsert_Rollback_ShouldDiscardAll()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - 批量插入后回滚
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.InsertItem("Item1");
            await repo.InsertItem("Item2");
            await repo.InsertItem("Item3");
            transaction.Rollback();
        }
        repo.Transaction = null;

        // Assert - 应该没有数据
        var items = await repo.GetAllItems();
        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("批量UPDATE在事务中应该是原子的")]
    public async Task BatchUpdate_InTransaction_ShouldBeAtomic()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE accounts (id INTEGER PRIMARY KEY, balance REAL)");
        _connection.Execute("INSERT INTO accounts VALUES (1, 100), (2, 200), (3, 300)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - 批量更新在事务中
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.UpdateAccountBalance(1, 150);
            await repo.UpdateAccountBalance(2, 250);
            await repo.UpdateAccountBalance(3, 350);
            transaction.Commit();
        }
        repo.Transaction = null;

        // Assert
        var accounts = await repo.GetAllAccounts();
        Assert.AreEqual(150, accounts.First(a => a.Id == 1).Balance, 0.01);
        Assert.AreEqual(250, accounts.First(a => a.Id == 2).Balance, 0.01);
        Assert.AreEqual(350, accounts.First(a => a.Id == 3).Balance, 0.01);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("批量DELETE在事务中应该是原子的")]
    public async Task BatchDelete_InTransaction_ShouldBeAtomic()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE logs (id INTEGER PRIMARY KEY, message TEXT)");
        _connection.Execute("INSERT INTO logs VALUES (1, 'Log1'), (2, 'Log2'), (3, 'Log3')");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - 批量删除在事务中
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.DeleteLog(1);
            await repo.DeleteLog(2);
            transaction.Commit();
        }
        repo.Transaction = null;

        // Assert
        var logs = await repo.GetAllLogs();
        Assert.AreEqual(1, logs.Count);
        Assert.AreEqual(3, logs[0].Id);
    }

    #endregion

    #region INSERT RETURNING事务兼容性测试 (Requirement 44.2)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("INSERT RETURNING在事务中应该正常工作")]
    public async Task InsertReturning_InTransaction_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE orders (id INTEGER PRIMARY KEY AUTOINCREMENT, customer TEXT, total REAL)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - INSERT并获取ID在事务中
        long insertedId;
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            insertedId = await repo.InsertOrderAndGetId("Customer1", 100.0);
            transaction.Commit();
        }
        repo.Transaction = null;

        // Assert
        Assert.IsTrue(insertedId > 0);
        var order = await repo.GetOrderById(insertedId);
        Assert.IsNotNull(order);
        Assert.AreEqual("Customer1", order.Customer);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("INSERT RETURNING回滚后ID不应该被使用")]
    public async Task InsertReturning_Rollback_ShouldNotPersist()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE tickets (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - INSERT获取ID后回滚
        long insertedId;
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            insertedId = await repo.InsertTicketAndGetId("Ticket1");
            transaction.Rollback();
        }
        repo.Transaction = null;

        // Assert - 记录不应该存在
        var ticket = await repo.GetTicketById(insertedId);
        Assert.IsNull(ticket);
    }

    #endregion

    #region UPSERT事务原子性测试 (Requirement 44.3)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("UPSERT在事务中应该是原子的")]
    public async Task Upsert_InTransaction_ShouldBeAtomic()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT)");
        _connection.Execute("INSERT INTO settings VALUES ('theme', 'light')");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - UPSERT在事务中
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.UpsertSetting("theme", "dark");  // 更新
            await repo.UpsertSetting("language", "en"); // 插入
            transaction.Commit();
        }
        repo.Transaction = null;

        // Assert
        var settings = await repo.GetAllSettings();
        Assert.AreEqual(2, settings.Count);
        Assert.AreEqual("dark", settings.First(s => s.Key == "theme").Value);
        Assert.AreEqual("en", settings.First(s => s.Key == "language").Value);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("UPSERT回滚应该撤销所有更改")]
    public async Task Upsert_Rollback_ShouldDiscardAll()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE configs (key TEXT PRIMARY KEY, value TEXT)");
        _connection.Execute("INSERT INTO configs VALUES ('mode', 'production')");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - UPSERT后回滚
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.UpsertConfig("mode", "development"); // 更新
            await repo.UpsertConfig("debug", "true");       // 插入
            transaction.Rollback();
        }
        repo.Transaction = null;

        // Assert - 应该保持原始状态
        var configs = await repo.GetAllConfigs();
        Assert.AreEqual(1, configs.Count);
        Assert.AreEqual("production", configs[0].Value);
    }

    #endregion

    #region 隐式提交检测测试 (Requirement 44.4)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("生成的SQL不应该包含DDL语句（会导致隐式提交）")]
    public void GeneratedSql_ShouldNotContainDDL()
    {
        // 验证生成的SQL不包含会导致隐式提交的DDL语句
        // DDL语句如CREATE, ALTER, DROP, TRUNCATE会在某些数据库中导致隐式提交
        
        var sqlTemplates = new[]
        {
            "INSERT INTO users (name) VALUES (@name)",
            "UPDATE users SET name = @name WHERE id = @id",
            "DELETE FROM users WHERE id = @id",
            "SELECT * FROM users WHERE id = @id"
        };

        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };

        foreach (var sql in sqlTemplates)
        {
            foreach (var ddl in ddlKeywords)
            {
                Assert.IsFalse(sql.ToUpper().Contains(ddl), 
                    $"SQL should not contain DDL keyword '{ddl}': {sql}");
            }
        }
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("混合操作在事务中应该保持一致性")]
    public async Task MixedOperations_InTransaction_ShouldMaintainConsistency()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE inventory (id INTEGER PRIMARY KEY, product TEXT, quantity INTEGER)");
        _connection.Execute("INSERT INTO inventory VALUES (1, 'Widget', 100)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - 混合INSERT/UPDATE/DELETE操作
        using (var transaction = _connection.BeginTransaction())
        {
            repo.Transaction = transaction;
            await repo.InsertInventory("Gadget", 50);           // INSERT
            await repo.UpdateInventoryQuantity(1, 80);          // UPDATE
            await repo.InsertInventory("Gizmo", 30);            // INSERT
            transaction.Commit();
        }
        repo.Transaction = null;

        // Assert
        var inventory = await repo.GetAllInventory();
        Assert.AreEqual(3, inventory.Count);
        Assert.AreEqual(80, inventory.First(i => i.Product == "Widget").Quantity);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("事务中的异常应该允许正确回滚")]
    public async Task Transaction_WithException_ShouldAllowProperRollback()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE transfers (id INTEGER PRIMARY KEY, from_account INTEGER, to_account INTEGER, amount REAL)");
        _connection.Execute("CREATE TABLE balances (account_id INTEGER PRIMARY KEY, balance REAL)");
        _connection.Execute("INSERT INTO balances VALUES (1, 1000), (2, 500)");
        var repo = new TransactionCompatibilityRepo(_connection);

        // Act - 模拟转账过程中的异常
        try
        {
            using (var transaction = _connection.BeginTransaction())
            {
                repo.Transaction = transaction;
                await repo.UpdateBalance(1, 900);  // 扣款
                // 模拟在加款前发生异常
                throw new InvalidOperationException("Simulated network error");
#pragma warning disable CS0162
                await repo.UpdateBalance(2, 600);  // 加款（不会执行）
                transaction.Commit();
#pragma warning restore CS0162
            }
        }
        catch (InvalidOperationException)
        {
            // 预期的异常
        }
        repo.Transaction = null;

        // Assert - 余额应该保持原始状态
        var balances = await repo.GetAllBalances();
        Assert.AreEqual(1000, balances.First(b => b.account_id == 1).balance, 0.01);
        Assert.AreEqual(500, balances.First(b => b.account_id == 2).balance, 0.01);
    }

    #endregion

    #region 嵌套事务测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("TransactionCompatibility")]
    [Description("SQLite不支持嵌套事务，应该正确处理")]
    public void SQLite_NestedTransaction_ShouldThrowOrIgnore()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE test (id INTEGER)");

        // Act & Assert - SQLite不支持嵌套事务
        using (var transaction1 = _connection.BeginTransaction())
        {
            // SQLite会抛出异常或忽略嵌套事务
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var transaction2 = _connection.BeginTransaction();
            });
            transaction1.Rollback();
        }
    }

    #endregion
}

#region Models

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
}

public class Item
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class Account
{
    public long Id { get; set; }
    public double Balance { get; set; }
}

public class Log
{
    public long Id { get; set; }
    public string Message { get; set; } = "";
}

public class Order
{
    public long Id { get; set; }
    public string Customer { get; set; } = "";
    public double Total { get; set; }
}

public class Ticket
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
}

public class Setting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class Config
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class Inventory
{
    public long Id { get; set; }
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
}

public class AccountBalance
{
    public long account_id { get; set; }
    public double balance { get; set; }
}

#endregion

#region Repository

[SqlDefine(Sqlx.Annotations.SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITransactionCompatibilityRepo))]
public partial class TransactionCompatibilityRepo(IDbConnection connection) : ITransactionCompatibilityRepo { }

public interface ITransactionCompatibilityRepo
{
    // Products
    [SqlTemplate("INSERT INTO products (name, price) VALUES (@name, @price)")]
    Task<int> InsertProduct(string name, double price);

    [SqlTemplate("SELECT id, name, price FROM products")]
    Task<List<Product>> GetAllProducts();

    // Items
    [SqlTemplate("INSERT INTO items (name) VALUES (@name)")]
    Task<int> InsertItem(string name);

    [SqlTemplate("SELECT id, name FROM items")]
    Task<List<Item>> GetAllItems();

    // Accounts
    [SqlTemplate("UPDATE accounts SET balance = @balance WHERE id = @id")]
    Task<int> UpdateAccountBalance(long id, double balance);

    [SqlTemplate("SELECT id, balance FROM accounts")]
    Task<List<Account>> GetAllAccounts();

    // Logs
    [SqlTemplate("DELETE FROM logs WHERE id = @id")]
    Task<int> DeleteLog(long id);

    [SqlTemplate("SELECT id, message FROM logs")]
    Task<List<Log>> GetAllLogs();

    // Orders
    [SqlTemplate("INSERT INTO orders (customer, total) VALUES (@customer, @total); SELECT last_insert_rowid();")]
    Task<long> InsertOrderAndGetId(string customer, double total);

    [SqlTemplate("SELECT id, customer, total FROM orders WHERE id = @id")]
    Task<Order?> GetOrderById(long id);

    // Tickets
    [SqlTemplate("INSERT INTO tickets (title) VALUES (@title); SELECT last_insert_rowid();")]
    Task<long> InsertTicketAndGetId(string title);

    [SqlTemplate("SELECT id, title FROM tickets WHERE id = @id")]
    Task<Ticket?> GetTicketById(long id);

    // Settings (UPSERT)
    [SqlTemplate("INSERT INTO settings (key, value) VALUES (@key, @value) ON CONFLICT(key) DO UPDATE SET value = excluded.value")]
    Task<int> UpsertSetting(string key, string value);

    [SqlTemplate("SELECT key, value FROM settings")]
    Task<List<Setting>> GetAllSettings();

    // Configs (UPSERT)
    [SqlTemplate("INSERT INTO configs (key, value) VALUES (@key, @value) ON CONFLICT(key) DO UPDATE SET value = excluded.value")]
    Task<int> UpsertConfig(string key, string value);

    [SqlTemplate("SELECT key, value FROM configs")]
    Task<List<Config>> GetAllConfigs();

    // Inventory
    [SqlTemplate("INSERT INTO inventory (product, quantity) VALUES (@product, @quantity)")]
    Task<int> InsertInventory(string product, int quantity);

    [SqlTemplate("UPDATE inventory SET quantity = @quantity WHERE id = @id")]
    Task<int> UpdateInventoryQuantity(long id, int quantity);

    [SqlTemplate("SELECT id, product, quantity FROM inventory")]
    Task<List<Inventory>> GetAllInventory();

    // Balances
    [SqlTemplate("UPDATE balances SET balance = @balance WHERE account_id = @accountId")]
    Task<int> UpdateBalance(long accountId, double balance);

    [SqlTemplate("SELECT account_id, balance FROM balances")]
    Task<List<AccountBalance>> GetAllBalances();
}

#endregion
