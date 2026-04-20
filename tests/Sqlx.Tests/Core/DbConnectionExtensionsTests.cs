// -----------------------------------------------------------------------
// <copyright file="DbConnectionExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

[Sqlx]
[TableName("batch_test_entity")]
public class BatchTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LightweightBatchDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

[TableName("batch_test_entity")]
public class PlainLightweightEntity
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
    private static async Task<SqliteConnection> CreateTestConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE batch_test_entity (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            INSERT INTO batch_test_entity (id, name) VALUES
                (1, 'Alpha'),
                (2, 'Beta'),
                (3, 'Gamma');
            """;
        await createCmd.ExecuteNonQueryAsync();
        return connection;
    }


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

    [TestMethod]
    public async Task SqlxQueryAsync_WithTemplatePlaceholders_ReturnsEntities()
    {
        await using var connection = await CreateTestConnectionAsync();

        var users = await connection.SqlxQueryAsync<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id >= @minId ORDER BY id",
            SqlDefine.SQLite,
            new { minId = 2 });

        Assert.AreEqual(2, users.Count);
        CollectionAssert.AreEqual(new[] { 2, 3 }, users.Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Beta", "Gamma" }, users.Select(x => x.Name).ToArray());
    }

    [TestMethod]
    public async Task SqlxQueryAsync_WithDtoProjection_UsesEntityTemplateContext()
    {
        await using var connection = await CreateTestConnectionAsync();

        var users = await connection.SqlxQueryAsync<LightweightBatchDto, BatchTestEntity>(
            "SELECT id AS Id, name AS DisplayName FROM {{table}} WHERE id <= @maxId ORDER BY id",
            SqlDefine.SQLite,
            new Dictionary<string, object?> { ["maxId"] = 2 });

        Assert.AreEqual(2, users.Count);
        Assert.AreEqual(1, users[0].Id);
        Assert.AreEqual("Alpha", users[0].DisplayName);
        Assert.AreEqual(2, users[1].Id);
        Assert.AreEqual("Beta", users[1].DisplayName);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefaultAsync_WithPlainPoco_FallsBackToDynamicMetadata()
    {
        await using var connection = await CreateTestConnectionAsync();

        var user = await connection.SqlxQueryFirstOrDefaultAsync<PlainLightweightEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1 });

        Assert.IsNotNull(user);
        Assert.AreEqual(1, user!.Id);
        Assert.AreEqual("Alpha", user.Name);
    }

    [TestMethod]
    public async Task SqlxExecuteAsync_WithTemplatePlaceholders_UpdatesRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = await connection.SqlxExecuteAsync<BatchTestEntity>(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            SqlDefine.SQLite,
            new BatchTestEntity { Id = 2, Name = "Updated" });

        Assert.AreEqual(1, affected);

        using var verifyCmd = connection.CreateCommand();
        verifyCmd.CommandText = "SELECT name FROM batch_test_entity WHERE id = 2";
        var name = (string)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.AreEqual("Updated", name);
    }

    [TestMethod]
    public async Task SqlxScalarAsync_WithTemplatePlaceholders_ReturnsScalarValue()
    {
        await using var connection = await CreateTestConnectionAsync();

        var count = await connection.SqlxScalarAsync<long, BatchTestEntity>(
            "SELECT COUNT(*) FROM {{table}} WHERE id >= @minId",
            SqlDefine.SQLite,
            new { minId = 2 });

        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public void SqlxQuerySingle_WithTemplatePlaceholders_ReturnsSingleEntity()
    {
        using var connection = CreateTestConnectionAsync().GetAwaiter().GetResult();

        var user = connection.SqlxQuerySingle<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 3 });

        Assert.AreEqual(3, user.Id);
        Assert.AreEqual("Gamma", user.Name);
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
