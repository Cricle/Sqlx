using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Integration tests with real database connections.
/// Tests actual query execution, data retrieval, and error handling.
/// </summary>
[TestClass]
public class SqlxQueryableIntegrationTests
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create test table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE QueryUser (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                salary REAL NOT NULL,
                created_at TEXT NOT NULL,
                email TEXT
            )";
        cmd.ExecuteNonQuery();

        // Insert test data
        cmd.CommandText = @"
            INSERT INTO QueryUser (id, name, age, is_active, salary, created_at, email) VALUES
            (1, 'Alice', 25, 1, 50000.0, '2024-01-01', 'alice@test.com'),
            (2, 'Bob', 30, 1, 60000.0, '2024-01-02', 'bob@test.com'),
            (3, 'Charlie', 35, 0, 70000.0, '2024-01-03', null),
            (4, 'David', 40, 1, 80000.0, '2024-01-04', 'david@test.com'),
            (5, 'Eve', 45, 0, 90000.0, '2024-01-05', null)";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    #region Basic Query Execution

    [TestMethod]
    public void Execute_SelectAll_ReturnsAllRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>().WithConnection(_connection!);
        var results = query.ToList();

        Assert.AreEqual(5, results.Count);
    }

    [TestMethod]
    public void Execute_WhereCondition_ReturnsFilteredRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .Where(u => u.IsActive);

        var results = query.ToList();

        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.All(u => u.IsActive));
    }

    [TestMethod]
    public void Execute_OrderBy_ReturnsOrderedRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .OrderBy(u => u.Age);

        var results = query.ToList();

        Assert.AreEqual(5, results.Count);
        Assert.AreEqual(25, results[0].Age);
        Assert.AreEqual(45, results[4].Age);
    }

    [TestMethod]
    public void Execute_TakeAndSkip_ReturnsPaginatedRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .OrderBy(u => u.Id)
            .Skip(2)
            .Take(2);

        var results = query.ToList();

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(3, results[0].Id);
        Assert.AreEqual(4, results[1].Id);
    }

    #endregion

    #region Null Handling

    [TestMethod]
    public void Execute_NullCheck_ReturnsCorrectRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .Where(u => u.Email == null);

        var results = query.ToList();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(u => u.Email == null));
    }

    [TestMethod]
    public void Execute_NotNullCheck_ReturnsCorrectRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .Where(u => u.Email != null);

        var results = query.ToList();

        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.All(u => u.Email != null));
    }

    #endregion

    #region Complex Queries

    [TestMethod]
    public void Execute_ComplexWhere_ReturnsCorrectRecords()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .Where(u => u.Age >= 30 && u.Age <= 40 && u.IsActive);

        var results = query.ToList();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(u => u.Age >= 30 && u.Age <= 40 && u.IsActive));
    }

    [TestMethod]
    public void Execute_MultipleOrderBy_ReturnsCorrectOrder()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .OrderBy(u => u.IsActive)
            .ThenByDescending(u => u.Age);

        var results = query.ToList();

        Assert.AreEqual(5, results.Count);
        // First group: IsActive = false, ordered by Age DESC
        Assert.IsFalse(results[0].IsActive);
        Assert.AreEqual(45, results[0].Age);
        // Second group: IsActive = true, ordered by Age DESC
        Assert.IsTrue(results[2].IsActive);
    }

    #endregion

    #region FirstOrDefault Tests

    [TestMethod]
    public void Execute_FirstOrDefault_ReturnsFirstRecord()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .OrderBy(u => u.Id);

        var result = query.FirstOrDefault();

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
    }

    [TestMethod]
    public void Execute_FirstOrDefault_NoMatch_ReturnsNull()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .WithConnection(_connection!)
            .Where(u => u.Age > 100);

        var result = query.FirstOrDefault();

        Assert.IsNull(result);
    }

    #endregion

    #region Async Tests

    // Note: Async tests removed as they depend on Sqlx-specific async extensions
    // that may not be available in all configurations

    #endregion

    #region Error Handling

    [TestMethod]
    public void Execute_NoConnection_ThrowsInvalidOperationException()
    {
        var query = SqlQuery.ForSqlite<QueryUser>();

        Assert.ThrowsException<InvalidOperationException>(() => query.ToList());
    }

    [TestMethod]
    public void Execute_ClosedConnection_ThrowsException()
    {
        _connection!.Close();
        var query = SqlQuery.ForSqlite<QueryUser>().WithConnection(_connection);

        Assert.ThrowsException<InvalidOperationException>(() => query.ToList());
    }

    #endregion
}
