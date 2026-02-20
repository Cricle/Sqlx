// <copyright file="MultipleResultSetsTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sqlx.Tests;

#region Test Interfaces and Repositories

public interface IMultiResultRepository
    {
        [SqlTemplate(@"
            INSERT INTO test_users (name, age) VALUES (@name, @age);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM test_users
        ")]
        [ResultSetMapping(0, "rowsAffected")]
        [ResultSetMapping(1, "userId")]
        [ResultSetMapping(2, "totalUsers")]
        Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name, int age);

        [SqlTemplate(@"
            INSERT INTO test_users (name, age) VALUES (@name, @age);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM test_users
        ")]
        [ResultSetMapping(0, "rowsAffected")]
        [ResultSetMapping(1, "userId")]
        [ResultSetMapping(2, "totalUsers")]
        (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(string name, int age);

        [SqlTemplate(@"
            SELECT COUNT(*) FROM test_users;
            SELECT MAX(id) FROM test_users;
            SELECT MIN(id) FROM test_users
        ")]
        [ResultSetMapping(0, "totalUsers")]
        [ResultSetMapping(1, "maxId")]
        [ResultSetMapping(2, "minId")]
        Task<(int totalUsers, long maxId, long minId)> GetStatsAsync();

        [SqlTemplate(@"
            SELECT COUNT(*) FROM test_users;
            SELECT MAX(id) FROM test_users;
            SELECT MIN(id) FROM test_users
        ")]
        [ResultSetMapping(0, "totalUsers")]
        [ResultSetMapping(1, "maxId")]
        [ResultSetMapping(2, "minId")]
        (int totalUsers, long maxId, long minId) GetStats();

        [SqlTemplate(@"
            INSERT INTO test_users (name, age) VALUES (@name, @age);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM test_users
        ")]
        Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsDefaultAsync(string name, int age);

        // Mixed scenarios: Output parameters + tuple return
        [SqlTemplate(@"
            INSERT INTO test_users (name, age) VALUES (@name, @age);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM test_users
        ")]
        [ResultSetMapping(0, "rowsAffected")]
        [ResultSetMapping(1, "userId")]
        [ResultSetMapping(2, "totalUsers")]
        (int rowsAffected, long userId, int totalUsers) InsertWithTimestamp(
            string name,
            int age,
            [OutputParameter(System.Data.DbType.DateTime)] ref System.DateTime timestamp);

        [SqlTemplate(@"
            INSERT INTO test_users (name, age) VALUES (@name, @age);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM test_users
        ")]
        [ResultSetMapping(0, "rowsAffected")]
        [ResultSetMapping(1, "userId")]
        [ResultSetMapping(2, "totalUsers")]
        Task<(int rowsAffected, long userId, int totalUsers)> InsertWithTimestampAsync(
            string name,
            int age,
            [OutputParameter(System.Data.DbType.DateTime)] OutputParameter<System.DateTime> timestamp);
    }

    [RepositoryFor(typeof(IMultiResultRepository), Dialect = (int)SqlDefineTypes.SQLite, TableName = "test_users")]
    public partial class MultiResultRepository : IMultiResultRepository
    {
        private readonly DbConnection _connection;

        public MultiResultRepository(DbConnection connection)
        {
            _connection = connection;
        }
    }

#endregion

/// <summary>
/// Integration tests for multiple result sets functionality.
/// </summary>
[TestClass]
public class MultipleResultSetsTests
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        // Create test table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public async Task InsertAndGetStatsAsync_WithResultSetMapping_ReturnsCorrectValues()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);

        // Act
        var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, total);
    }

    [TestMethod]
    public async Task InsertAndGetStatsAsync_MultipleInserts_ReturnsIncrementingValues()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);

        // Act
        var (rows1, userId1, total1) = await repo.InsertAndGetStatsAsync("Alice", 25);
        var (rows2, userId2, total2) = await repo.InsertAndGetStatsAsync("Bob", 30);
        var (rows3, userId3, total3) = await repo.InsertAndGetStatsAsync("Charlie", 35);

        // Assert
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1L, userId1);
        Assert.AreEqual(1, total1);

        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2L, userId2);
        Assert.AreEqual(2, total2);

        Assert.AreEqual(1, rows3);
        Assert.AreEqual(3L, userId3);
        Assert.AreEqual(3, total3);
    }

    [TestMethod]
    public void InsertAndGetStats_SyncMethod_ReturnsCorrectValues()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);

        // Act
        var (rows, userId, total) = repo.InsertAndGetStats("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, total);
    }

    [TestMethod]
    public void InsertAndGetStats_SyncMultipleInserts_ReturnsIncrementingValues()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);

        // Act
        var (rows1, userId1, total1) = repo.InsertAndGetStats("Alice", 25);
        var (rows2, userId2, total2) = repo.InsertAndGetStats("Bob", 30);

        // Assert
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1L, userId1);
        Assert.AreEqual(1, total1);

        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2L, userId2);
        Assert.AreEqual(2, total2);
    }

    [TestMethod]
    public async Task GetStatsAsync_WithData_ReturnsCorrectStats()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);
        await repo.InsertAndGetStatsAsync("Alice", 25);
        await repo.InsertAndGetStatsAsync("Bob", 30);
        await repo.InsertAndGetStatsAsync("Charlie", 35);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(3, total);
        Assert.AreEqual(3L, maxId);
        Assert.AreEqual(1L, minId);
    }

    [TestMethod]
    public void GetStats_SyncMethod_ReturnsCorrectStats()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);
        repo.InsertAndGetStats("Alice", 25);
        repo.InsertAndGetStats("Bob", 30);

        // Act
        var (total, maxId, minId) = repo.GetStats();

        // Assert
        Assert.AreEqual(2, total);
        Assert.AreEqual(2L, maxId);
        Assert.AreEqual(1L, minId);
    }

    [TestMethod]
    public async Task InsertAndGetStatsDefaultAsync_WithoutResultSetMapping_UsesDefaultMapping()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);

        // Act
        var (rows, userId, total) = await repo.InsertAndGetStatsDefaultAsync("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, total);
    }

    [TestMethod]
    public async Task MultipleResultSets_WithDifferentTypes_HandlesTypeConversion()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);
        await repo.InsertAndGetStatsAsync("Alice", 25);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.IsInstanceOfType(total, typeof(int));
        Assert.IsInstanceOfType(maxId, typeof(long));
        Assert.IsInstanceOfType(minId, typeof(long));
    }

    [TestMethod]
    [Ignore("SQLite does not support output parameters")]
    public void InsertWithTimestamp_SyncMethod_ReturnsCorrectValuesAndSetsOutputParameter()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);
        var timestamp = System.DateTime.UtcNow;

        // Act
        var (rows, userId, total) = repo.InsertWithTimestamp("Alice", 25, ref timestamp);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, total);
        Assert.IsNotNull(timestamp);
        Assert.AreNotEqual(System.DateTime.MinValue, timestamp);
    }

    [TestMethod]
    [Ignore("SQLite does not support output parameters")]
    public async Task InsertWithTimestampAsync_AsyncMethod_ReturnsCorrectValuesAndSetsOutputParameter()
    {
        // Arrange
        var repo = new MultiResultRepository(_connection!);
        var timestamp = new OutputParameter<System.DateTime>();

        // Act
        var (rows, userId, total) = await repo.InsertWithTimestampAsync("Bob", 30, timestamp);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, total);
        Assert.IsTrue(timestamp.HasValue);
        Assert.IsNotNull(timestamp.Value);
        Assert.AreNotEqual(System.DateTime.MinValue, timestamp.Value);
    }
}
