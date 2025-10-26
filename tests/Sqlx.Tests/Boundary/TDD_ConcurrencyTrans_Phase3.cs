using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Boundary;

/// <summary>
/// Phase 3: 并发和事务测试 - 确保80%边界场景覆盖
/// 新增8个并发和事务测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Boundary")]
[TestCategory("CoveragePhase3")]
public class TDD_ConcurrencyTrans_Phase3
{
    private IDbConnection? _connection;
    private ConcurrencyTransRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE accounts (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                balance REAL NOT NULL
            )");

        _repo = new ConcurrencyTransRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("Transaction commit should persist changes")]
    public async Task Transaction_Commit_ShouldPersist()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");

        using (var transaction = _connection.BeginTransaction())
        {
            _repo!.Transaction = transaction;
            await _repo.UpdateBalanceAsync(1, 1500.0);
            transaction.Commit();
        }

        _repo!.Transaction = null;
        var account = await _repo.GetByIdAsync(1);
        Assert.AreEqual(1500.0, account!.Balance, 0.01);
    }

    [TestMethod]
    [Description("Transaction rollback should revert changes")]
    public async Task Transaction_Rollback_ShouldRevert()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");

        using (var transaction = _connection.BeginTransaction())
        {
            _repo!.Transaction = transaction;
            await _repo.UpdateBalanceAsync(1, 1500.0);

            // Verify change is visible within transaction
            var inTrans = await _repo.GetByIdAsync(1);
            Assert.AreEqual(1500.0, inTrans!.Balance, 0.01);

            transaction.Rollback();
        }

        _repo!.Transaction = null;
        var account = await _repo.GetByIdAsync(1);
        Assert.AreEqual(1000.0, account!.Balance, 0.01);
    }

    [TestMethod]
    [Description("Multiple operations in transaction should be atomic")]
    public async Task Transaction_MultipleOps_ShouldBeAtomic()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");
        _connection.Execute("INSERT INTO accounts VALUES (2, 'Bob', 500.0)");

        using (var transaction = _connection.BeginTransaction())
        {
            _repo!.Transaction = transaction;

            // Transfer 200 from Alice to Bob
            await _repo.UpdateBalanceAsync(1, 800.0);
            await _repo.UpdateBalanceAsync(2, 700.0);

            transaction.Commit();
        }

        _repo!.Transaction = null;
        var alice = await _repo.GetByIdAsync(1);
        var bob = await _repo.GetByIdAsync(2);

        Assert.AreEqual(800.0, alice!.Balance, 0.01);
        Assert.AreEqual(700.0, bob!.Balance, 0.01);
    }

    [TestMethod]
    [Description("Transaction rollback on error should work")]
    public async Task Transaction_RollbackOnError_ShouldWork()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");

        try
        {
            using (var transaction = _connection.BeginTransaction())
            {
                _repo!.Transaction = transaction;
                await _repo.UpdateBalanceAsync(1, 1500.0);

                // Simulate an error
                throw new Exception("Simulated error");

                #pragma warning disable CS0162
                transaction.Commit();
                #pragma warning restore CS0162
            }
        }
        catch
        {
            // Expected
        }

        _repo!.Transaction = null;
        var account = await _repo.GetByIdAsync(1);
        Assert.AreEqual(1000.0, account!.Balance, 0.01);
    }

    [TestMethod]
    [Description("Sequential transactions should work")]
    public async Task Transactions_Sequential_ShouldWork()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");

        // Transaction 1
        using (var transaction = _connection.BeginTransaction())
        {
            _repo!.Transaction = transaction;
            await _repo.UpdateBalanceAsync(1, 1100.0);
            transaction.Commit();
        }

        // Transaction 2
        using (var transaction = _connection.BeginTransaction())
        {
            _repo!.Transaction = transaction;
            await _repo.UpdateBalanceAsync(1, 1200.0);
            transaction.Commit();
        }

        _repo!.Transaction = null;
        var account = await _repo.GetByIdAsync(1);
        Assert.AreEqual(1200.0, account!.Balance, 0.01);
    }

    [TestMethod]
    [Description("Concurrent reads should work")]
    public async Task Concurrent_Reads_ShouldWork()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");
        _connection.Execute("INSERT INTO accounts VALUES (2, 'Bob', 2000.0)");
        _connection.Execute("INSERT INTO accounts VALUES (3, 'Charlie', 3000.0)");

        // Simulate concurrent reads
        var task1 = _repo!.GetByIdAsync(1);
        var task2 = _repo.GetByIdAsync(2);
        var task3 = _repo.GetByIdAsync(3);

        await Task.WhenAll(task1, task2, task3);

        Assert.AreEqual(1000.0, task1.Result!.Balance, 0.01);
        Assert.AreEqual(2000.0, task2.Result!.Balance, 0.01);
        Assert.AreEqual(3000.0, task3.Result!.Balance, 0.01);
    }

    [TestMethod]
    [Description("INSERT and SELECT in transaction should work")]
    public async Task Transaction_InsertAndSelect_ShouldWork()
    {
        using (var transaction = _connection!.BeginTransaction())
        {
            _repo!.Transaction = transaction;

            // Note: SQLite in-memory doesn't support INSERT RETURNING in the same way
            // So we'll do INSERT via Execute and then SELECT
            _connection.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)", transaction: transaction);

            var account = await _repo.GetByIdAsync(1);
            Assert.IsNotNull(account);
            Assert.AreEqual(1000.0, account.Balance, 0.01);

            transaction.Commit();
        }

        _repo!.Transaction = null;
        var final = await _repo.GetByIdAsync(1);
        Assert.IsNotNull(final);
    }

    [TestMethod]
    [Description("Transaction with DELETE should work")]
    public async Task Transaction_WithDelete_ShouldWork()
    {
        _connection!.Execute("INSERT INTO accounts VALUES (1, 'Alice', 1000.0)");
        _connection.Execute("INSERT INTO accounts VALUES (2, 'Bob', 2000.0)");

        using (var transaction = _connection.BeginTransaction())
        {
            _repo!.Transaction = transaction;

            var affected = await _repo.DeleteByIdAsync(1);
            Assert.AreEqual(1, affected);

            // Verify within transaction
            var alice = await _repo.GetByIdAsync(1);
            Assert.IsNull(alice);

            transaction.Commit();
        }

        _repo!.Transaction = null;
        var aliceFinal = await _repo.GetByIdAsync(1);
        var bob = await _repo.GetByIdAsync(2);

        Assert.IsNull(aliceFinal);
        Assert.IsNotNull(bob);
    }
}

// Repository interface
public interface IConcurrencyTransRepository
{
    [SqlTemplate("SELECT * FROM accounts WHERE id = @id")]
    Task<ConcurrencyAccount?> GetByIdAsync(long id);

    [SqlTemplate("UPDATE accounts SET balance = @balance WHERE id = @id")]
    Task<int> UpdateBalanceAsync(long id, double balance);

    [SqlTemplate("DELETE FROM accounts WHERE id = @id")]
    Task<int> DeleteByIdAsync(long id);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IConcurrencyTransRepository))]
public partial class ConcurrencyTransRepository(IDbConnection connection) : IConcurrencyTransRepository { }

// Model
public class ConcurrencyAccount
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Balance { get; set; }
}

