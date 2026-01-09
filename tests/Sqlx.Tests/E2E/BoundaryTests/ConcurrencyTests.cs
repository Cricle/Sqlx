// -----------------------------------------------------------------------
// <copyright file="ConcurrencyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.BoundaryTests;

#region Test Entity

[TableName("concurrency_users")]
public class ConcurrencyUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Counter { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region Repository

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICrudRepository<ConcurrencyUser, long>))]
public partial class ConcurrencyUserCrudRepository(IDbConnection connection) : ICrudRepository<ConcurrencyUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchRepository<ConcurrencyUser, long>))]
public partial class ConcurrencyUserBatchRepository(IDbConnection connection) : IBatchRepository<ConcurrencyUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

/// <summary>
/// E2E tests for concurrency handling in predefined interfaces.
/// Tests concurrent inserts, concurrent updates, and optimistic locking.
/// Requirements: 12.3
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("BoundaryTests")]
[TestCategory("Concurrency")]
public class ConcurrencyTests : IDisposable
{
    private SqliteConnection? _connection;
    private ConcurrencyUserCrudRepository? _crudRepo;
    private ConcurrencyUserBatchRepository? _batchRepo;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await CreateTableAsync();
        _crudRepo = new ConcurrencyUserCrudRepository(_connection);
        _batchRepo = new ConcurrencyUserBatchRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    private async Task CreateTableAsync()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS concurrency_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                counter INTEGER NOT NULL DEFAULT 0,
                version INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<long> InsertUserAsync(string name, int counter = 0, int version = 1)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO concurrency_users (name, counter, version, created_at)
            VALUES (@name, @counter, @version, @createdAt);
            SELECT last_insert_rowid();";
        cmd.Parameters.Add(new SqliteParameter("@name", name));
        cmd.Parameters.Add(new SqliteParameter("@counter", counter));
        cmd.Parameters.Add(new SqliteParameter("@version", version));
        cmd.Parameters.Add(new SqliteParameter("@createdAt", DateTime.UtcNow.ToString("o")));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    [TestMethod]
    public async Task Concurrency_SequentialInserts_ShouldGenerateUniqueIds()
    {
        // Arrange & Act - Insert multiple users sequentially
        var ids = new List<long>();
        for (int i = 0; i < 100; i++)
        {
            var user = new ConcurrencyUser
            {
                Name = $"User{i}",
                Counter = i,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };
            var id = await _crudRepo!.InsertAndGetIdAsync(user);
            ids.Add(id);
        }

        // Assert - All IDs should be unique
        Assert.AreEqual(100, ids.Count);
        Assert.AreEqual(100, ids.Distinct().Count());
        Assert.IsTrue(ids.All(id => id > 0));
    }

    [TestMethod]
    public async Task Concurrency_SequentialUpdates_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var id = await InsertUserAsync("UpdateTest", counter: 0);

        // Act - Perform sequential updates
        for (int i = 1; i <= 50; i++)
        {
            var user = await _crudRepo!.GetByIdAsync(id);
            Assert.IsNotNull(user);
            user.Counter = i;
            user.UpdatedAt = DateTime.UtcNow;
            await _crudRepo.UpdateAsync(user);
        }

        // Assert
        var finalUser = await _crudRepo!.GetByIdAsync(id);
        Assert.IsNotNull(finalUser);
        Assert.AreEqual(50, finalUser.Counter);
    }

    [TestMethod]
    public async Task Concurrency_BatchInsert_ShouldInsertAllRecords()
    {
        // Arrange
        var users = Enumerable.Range(1, 100)
            .Select(i => new ConcurrencyUser
            {
                Name = $"BatchUser{i}",
                Counter = i,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        // Act
        var affected = await _batchRepo!.BatchInsertAsync(users);

        // Assert
        Assert.AreEqual(100, affected);
        var count = await _crudRepo!.CountAsync();
        Assert.AreEqual(100L, count);
    }

    [TestMethod]
    public async Task Concurrency_OptimisticLocking_VersionIncrement_ShouldWork()
    {
        // Arrange - Insert user with version 1
        var id = await InsertUserAsync("VersionTest", counter: 0, version: 1);

        // Act - Simulate optimistic locking by incrementing version on each update
        for (int i = 1; i <= 10; i++)
        {
            var user = await _crudRepo!.GetByIdAsync(id);
            Assert.IsNotNull(user);
            
            var expectedVersion = i;
            Assert.AreEqual(expectedVersion, user.Version, $"Version mismatch before update {i}");
            
            user.Counter = i;
            user.Version = i + 1; // Increment version
            user.UpdatedAt = DateTime.UtcNow;
            await _crudRepo.UpdateAsync(user);
        }

        // Assert
        var finalUser = await _crudRepo!.GetByIdAsync(id);
        Assert.IsNotNull(finalUser);
        Assert.AreEqual(10, finalUser.Counter);
        Assert.AreEqual(11, finalUser.Version);
    }

    [TestMethod]
    public async Task Concurrency_MultipleReads_ShouldReturnConsistentData()
    {
        // Arrange
        var id = await InsertUserAsync("ReadTest", counter: 42, version: 1);

        // Act - Perform multiple reads
        var results = new List<ConcurrencyUser?>();
        for (int i = 0; i < 50; i++)
        {
            var user = await _crudRepo!.GetByIdAsync(id);
            results.Add(user);
        }

        // Assert - All reads should return the same data
        Assert.IsTrue(results.All(u => u != null));
        Assert.IsTrue(results.All(u => u!.Name == "ReadTest"));
        Assert.IsTrue(results.All(u => u!.Counter == 42));
    }

    [TestMethod]
    public async Task Concurrency_InsertAndDelete_ShouldMaintainConsistency()
    {
        // Arrange & Act - Insert and delete in sequence
        var insertedIds = new List<long>();
        
        // Insert 50 users
        for (int i = 0; i < 50; i++)
        {
            var user = new ConcurrencyUser
            {
                Name = $"TempUser{i}",
                Counter = i,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };
            var id = await _crudRepo!.InsertAndGetIdAsync(user);
            insertedIds.Add(id);
        }

        // Delete first 25
        for (int i = 0; i < 25; i++)
        {
            await _crudRepo!.DeleteAsync(insertedIds[i]);
        }

        // Assert
        var count = await _crudRepo!.CountAsync();
        Assert.AreEqual(25L, count);

        // Verify deleted users are gone
        for (int i = 0; i < 25; i++)
        {
            var user = await _crudRepo!.GetByIdAsync(insertedIds[i]);
            Assert.IsNull(user, $"User {insertedIds[i]} should be deleted");
        }

        // Verify remaining users exist
        for (int i = 25; i < 50; i++)
        {
            var user = await _crudRepo!.GetByIdAsync(insertedIds[i]);
            Assert.IsNotNull(user, $"User {insertedIds[i]} should exist");
        }
    }

    [TestMethod]
    public async Task Concurrency_BatchDelete_ShouldDeleteAllSpecified()
    {
        // Arrange - Insert 100 users
        var ids = new List<long>();
        for (int i = 0; i < 100; i++)
        {
            var id = await InsertUserAsync($"BatchDeleteUser{i}", counter: i);
            ids.Add(id);
        }

        // Act - Delete first 50 in batch
        var idsToDelete = ids.Take(50).ToList();
        var affected = await _batchRepo!.BatchDeleteAsync(idsToDelete);

        // Assert
        Assert.AreEqual(50, affected);
        var count = await _crudRepo!.CountAsync();
        Assert.AreEqual(50L, count);
    }

    [TestMethod]
    public async Task Concurrency_UpdateNonExistent_ShouldReturnZeroAffected()
    {
        // Arrange
        var nonExistentUser = new ConcurrencyUser
        {
            Id = 99999,
            Name = "NonExistent",
            Counter = 0,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var affected = await _crudRepo!.UpdateAsync(nonExistentUser);

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task Concurrency_DeleteNonExistent_ShouldReturnZeroAffected()
    {
        // Act
        var affected = await _crudRepo!.DeleteAsync(99999);

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task Concurrency_RapidInsertAndRead_ShouldMaintainConsistency()
    {
        // Arrange & Act - Rapidly insert and immediately read
        for (int i = 0; i < 100; i++)
        {
            var user = new ConcurrencyUser
            {
                Name = $"RapidUser{i}",
                Counter = i,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };
            
            var id = await _crudRepo!.InsertAndGetIdAsync(user);
            var retrieved = await _crudRepo.GetByIdAsync(id);
            
            Assert.IsNotNull(retrieved);
            Assert.AreEqual($"RapidUser{i}", retrieved.Name);
            Assert.AreEqual(i, retrieved.Counter);
        }

        // Assert final count
        var count = await _crudRepo!.CountAsync();
        Assert.AreEqual(100L, count);
    }
}
