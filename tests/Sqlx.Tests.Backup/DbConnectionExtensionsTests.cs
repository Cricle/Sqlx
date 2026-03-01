// -----------------------------------------------------------------------
// <copyright file="DbConnectionExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlx.Tests;

[Sqlx]
public class BatchTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Tests for DbConnectionExtensions.
/// </summary>
[TestClass]
public class DbConnectionExtensionsTests
{

    [TestMethod]
    public async Task ExecuteBatchAsync_WithEntities_ExecutesSuccessfully()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE test_entity (id INTEGER PRIMARY KEY, name TEXT)";
        await createCmd.ExecuteNonQueryAsync();

        var entities = new List<BatchTestEntity>
        {
            new BatchTestEntity { Id = 1, Name = "Test1" },
            new BatchTestEntity { Id = 2, Name = "Test2" },
            new BatchTestEntity { Id = 3, Name = "Test3" }
        };

        var binder = BatchTestEntityParameterBinder.Default;
        var sql = "INSERT INTO test_entity (id, name) VALUES (@id, @name)";

        // Act
        var result = await connection.ExecuteBatchAsync<BatchTestEntity>(sql, entities, binder);

        // Assert
        Assert.AreEqual(3, result);

        // Verify data was inserted
        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_entity";
        var count = (long)(await selectCmd.ExecuteScalarAsync())!;
        Assert.AreEqual(3L, count);
    }

    [TestMethod]
    public async Task ExecuteBatchAsync_WithTransaction_CommitsSuccessfully()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE test_entity (id INTEGER PRIMARY KEY, name TEXT)";
        await createCmd.ExecuteNonQueryAsync();

        using var transaction = connection.BeginTransaction();

        var entities = new List<BatchTestEntity>
        {
            new BatchTestEntity { Id = 1, Name = "Test1" },
            new BatchTestEntity { Id = 2, Name = "Test2" }
        };

        var binder = BatchTestEntityParameterBinder.Default;
        var sql = "INSERT INTO test_entity (id, name) VALUES (@id, @name)";

        // Act
        var result = await connection.ExecuteBatchAsync<BatchTestEntity>(sql, entities, binder, transaction);
        await transaction.CommitAsync();

        // Assert
        Assert.AreEqual(2, result);

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_entity";
        var count = (long)(await selectCmd.ExecuteScalarAsync())!;
        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public async Task ExecuteBatchAsync_WithCustomBatchSize_ProcessesInBatches()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE test_entity (id INTEGER PRIMARY KEY, name TEXT)";
        await createCmd.ExecuteNonQueryAsync();

        var entities = new List<BatchTestEntity>();
        for (int i = 1; i <= 10; i++)
        {
            entities.Add(new BatchTestEntity { Id = i, Name = $"Test{i}" });
        }

        var binder = BatchTestEntityParameterBinder.Default;
        var sql = "INSERT INTO test_entity (id, name) VALUES (@id, @name)";

        // Act - use small batch size to test batching
        var result = await connection.ExecuteBatchAsync<BatchTestEntity>(sql, entities, binder, batchSize: 3);

        // Assert
        Assert.AreEqual(10, result);

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_entity";
        var count = (long)(await selectCmd.ExecuteScalarAsync())!;
        Assert.AreEqual(10L, count);
    }

    [TestMethod]
    public async Task ExecuteBatchAsync_WithEmptyList_ReturnsZero()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var entities = new List<BatchTestEntity>();
        var binder = BatchTestEntityParameterBinder.Default;
        var sql = "INSERT INTO test_entity (id, name) VALUES (@id, @name)";

        // Act
        var result = await connection.ExecuteBatchAsync<BatchTestEntity>(sql, entities, binder);

        // Assert
        Assert.AreEqual(0, result);
    }

#if NET6_0_OR_GREATER
    [TestMethod]
    public async Task ExecuteBatchAsync_Generic_WithEntities_ExecutesSuccessfully()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE test_entity (id INTEGER PRIMARY KEY, name TEXT)";
        await createCmd.ExecuteNonQueryAsync();

        var entities = new List<BatchTestEntity>
        {
            new BatchTestEntity { Id = 1, Name = "Test1" },
            new BatchTestEntity { Id = 2, Name = "Test2" }
        };

        var binder = BatchTestEntityParameterBinder.Default;
        var sql = "INSERT INTO test_entity (id, name) VALUES (@id, @name)";

        // Act
        var result = await connection.ExecuteBatchAsync<BatchTestEntity, SqliteParameter>(sql, entities, binder);

        // Assert
        Assert.AreEqual(2, result);

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_entity";
        var count = (long)(await selectCmd.ExecuteScalarAsync())!;
        Assert.AreEqual(2L, count);
    }
#endif
}
