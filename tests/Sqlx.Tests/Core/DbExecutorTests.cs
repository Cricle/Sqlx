// -----------------------------------------------------------------------
// <copyright file="DbExecutorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

[Sqlx]
public class ExecutorTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Tests for DbExecutor internal functionality.
/// </summary>
[TestClass]
public class DbExecutorTests
{

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        using var createCmd = _connection.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE test_entity (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            )";
        await createCmd.ExecuteNonQueryAsync();

        // Insert test data
        using var insertCmd = _connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO test_entity (id, name) VALUES (1, 'Test1'), (2, 'Test2'), (3, 'Test3')";
        await insertCmd.ExecuteNonQueryAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    private SqliteConnection? _connection;

    [TestMethod]
    public async Task ExecuteScalarAsync_WithParameters_ReturnsValue()
    {
        // Arrange
        var sql = "SELECT COUNT(*) FROM test_entity WHERE id > @minId";
        var parameters = new Dictionary<string, object?>
        {
            ["@minId"] = 1
        };

        // Act
        var result = await DbExecutor.ExecuteScalarAsync(_connection!, sql, parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2L, (long)result);
    }

    [TestMethod]
    public async Task ExecuteScalarAsync_WithoutParameters_ReturnsValue()
    {
        // Arrange
        var sql = "SELECT COUNT(*) FROM test_entity";

        // Act
        var result = await DbExecutor.ExecuteScalarAsync(_connection!, sql, null);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3L, (long)result);
    }

    [TestMethod]
    public void ExecuteScalar_WithParameters_ReturnsValue()
    {
        // Arrange
        var sql = "SELECT name FROM test_entity WHERE id = @id";
        var parameters = new Dictionary<string, object?>
        {
            ["@id"] = 2
        };

        // Act
        var result = DbExecutor.ExecuteScalar(_connection!, sql, parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test2", result.ToString());
    }

    [TestMethod]
    public void ExecuteScalar_WithoutParameters_ReturnsValue()
    {
        // Arrange
        var sql = "SELECT COUNT(*) FROM test_entity";

        // Act
        var result = DbExecutor.ExecuteScalar(_connection!, sql, null);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3L, (long)result);
    }

    [TestMethod]
    public void ExecuteReader_WithParameters_ReturnsResults()
    {
        // Arrange
        var sql = "SELECT id, name FROM test_entity WHERE id > @minId ORDER BY id";
        var parameters = new Dictionary<string, object?>
        {
            ["@minId"] = 1
        };
        var reader = ExecutorTestEntityResultReader.Default;

        // Act
        var results = DbExecutor.ExecuteReader(_connection!, sql, parameters, reader).ToList();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(2, results[0].Id);
        Assert.AreEqual("Test2", results[0].Name);
        Assert.AreEqual(3, results[1].Id);
        Assert.AreEqual("Test3", results[1].Name);
    }

    [TestMethod]
    public void ExecuteReader_WithoutParameters_ReturnsResults()
    {
        // Arrange
        var sql = "SELECT id, name FROM test_entity ORDER BY id";
        var reader = ExecutorTestEntityResultReader.Default;

        // Act
        var results = DbExecutor.ExecuteReader(_connection!, sql, null, reader).ToList();

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test1", results[0].Name);
    }

    [TestMethod]
    public async Task ExecuteReaderAsync_WithParameters_ReturnsResults()
    {
        // Arrange
        var sql = "SELECT id, name FROM test_entity WHERE id <= @maxId ORDER BY id";
        var parameters = new Dictionary<string, object?>
        {
            ["@maxId"] = 2
        };
        var reader = ExecutorTestEntityResultReader.Default;

        // Act
        var results = await ReadAllAsync(DbExecutor.ExecuteReaderAsync(_connection!, sql, parameters, reader));

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test1", results[0].Name);
        Assert.AreEqual(2, results[1].Id);
        Assert.AreEqual("Test2", results[1].Name);
    }

    [TestMethod]
    public async Task ExecuteReaderAsync_WithoutParameters_ReturnsResults()
    {
        // Arrange
        var sql = "SELECT id, name FROM test_entity ORDER BY id";
        var reader = ExecutorTestEntityResultReader.Default;

        // Act
        var results = await ReadAllAsync(DbExecutor.ExecuteReaderAsync(_connection!, sql, null, reader));

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual(2, results[1].Id);
        Assert.AreEqual(3, results[2].Id);
    }

    [TestMethod]
    public async Task ExecuteScalarAsync_WithNullParameter_HandlesCorrectly()
    {
        // Arrange
        var sql = "SELECT COUNT(*) FROM test_entity WHERE name = @name";
        var parameters = new Dictionary<string, object?>
        {
            ["@name"] = null
        };

        // Act
        var result = await DbExecutor.ExecuteScalarAsync(_connection!, sql, parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0L, (long)result);
    }

    [TestMethod]
    public void ExecuteReader_WithClosedConnection_OpensConnection()
    {
        // Arrange
        using var closedConnection = new SqliteConnection("Data Source=:memory:");
        var sql = "SELECT 1 AS id, 'Test' AS name";
        var reader = ExecutorTestEntityResultReader.Default;

        // Act
        var results = DbExecutor.ExecuteReader(closedConnection, sql, null, reader).ToList();

        // Assert
        AssertSingleTestEntity(results);
        Assert.AreEqual(ConnectionState.Closed, closedConnection.State);
    }

    [TestMethod]
    public async Task ExecuteScalarAsync_WithClosedConnection_OpensConnection()
    {
        // Arrange
        using var closedConnection = new SqliteConnection("Data Source=:memory:");
        // Don't open the connection
        var sql = "SELECT 1";

        // Act
        var result = await DbExecutor.ExecuteScalarAsync(closedConnection, sql, null);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1L, (long)result);
        Assert.AreEqual(ConnectionState.Closed, closedConnection.State);
    }

    [TestMethod]
    public void ExecuteScalar_WithClosedConnection_ClosesConnectionAfterExecution()
    {
        // Arrange
        using var closedConnection = new SqliteConnection("Data Source=:memory:");
        var sql = "SELECT 1";

        // Act
        var result = DbExecutor.ExecuteScalar(closedConnection, sql, null);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1L, (long)result);
        Assert.AreEqual(ConnectionState.Closed, closedConnection.State);
    }

    [TestMethod]
    public async Task ExecuteReaderAsync_WithClosedConnection_ClosesConnectionAfterEnumeration()
    {
        // Arrange
        using var closedConnection = new SqliteConnection("Data Source=:memory:");
        var sql = "SELECT 1 AS id, 'Test' AS name";
        var reader = ExecutorTestEntityResultReader.Default;

        // Act
        var results = await ReadAllAsync(DbExecutor.ExecuteReaderAsync(closedConnection, sql, null, reader));

        // Assert
        AssertSingleTestEntity(results);
        Assert.AreEqual(ConnectionState.Closed, closedConnection.State);
    }

    private static async Task<List<ExecutorTestEntity>> ReadAllAsync(IAsyncEnumerator<ExecutorTestEntity> enumerator)
    {
        var results = new List<ExecutorTestEntity>();
        await using (enumerator)
        {
            while (await enumerator.MoveNextAsync())
            {
                results.Add(enumerator.Current);
            }
        }

        return results;
    }

    private static void AssertSingleTestEntity(IReadOnlyList<ExecutorTestEntity> results)
    {
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test", results[0].Name);
    }
}
