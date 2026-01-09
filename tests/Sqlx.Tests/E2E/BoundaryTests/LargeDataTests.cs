// -----------------------------------------------------------------------
// <copyright file="LargeDataTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.BoundaryTests;

#region Test Entity

[TableName("large_data_users")]
public class LargeDataUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion

#region Repository

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IQueryRepository<LargeDataUser, long>))]
public partial class LargeDataUserQueryRepository(IDbConnection connection) : IQueryRepository<LargeDataUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAggregateRepository<LargeDataUser, long>))]
public partial class LargeDataUserAggregateRepository(IDbConnection connection) : IAggregateRepository<LargeDataUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

/// <summary>
/// E2E tests for large data handling in predefined interfaces.
/// Tests large batch inserts (1000+ records), long strings, and large result sets.
/// Requirements: 12.3
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("BoundaryTests")]
[TestCategory("LargeData")]
public class LargeDataTests : IDisposable
{
    private SqliteConnection? _connection;
    private LargeDataUserQueryRepository? _queryRepo;
    private LargeDataUserAggregateRepository? _aggregateRepo;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await CreateTableAsync();
        _queryRepo = new LargeDataUserQueryRepository(_connection);
        _aggregateRepo = new LargeDataUserAggregateRepository(_connection);
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
            CREATE TABLE IF NOT EXISTS large_data_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                age INTEGER NOT NULL,
                created_at TEXT NOT NULL
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertUsersAsync(int count)
    {
        using var transaction = _connection!.BeginTransaction();
        try
        {
            for (int i = 0; i < count; i++)
            {
                using var cmd = _connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO large_data_users (name, description, age, created_at)
                    VALUES (@name, @description, @age, @createdAt)";
                cmd.Parameters.Add(new SqliteParameter("@name", $"User{i}"));
                cmd.Parameters.Add(new SqliteParameter("@description", $"Description for user {i}"));
                cmd.Parameters.Add(new SqliteParameter("@age", 20 + (i % 50)));
                cmd.Parameters.Add(new SqliteParameter("@createdAt", DateTime.UtcNow.ToString("o")));
                await cmd.ExecuteNonQueryAsync();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task<long> InsertUserWithLongStringAsync(string name, string longDescription)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO large_data_users (name, description, age, created_at)
            VALUES (@name, @description, @age, @createdAt);
            SELECT last_insert_rowid();";
        cmd.Parameters.Add(new SqliteParameter("@name", name));
        cmd.Parameters.Add(new SqliteParameter("@description", longDescription));
        cmd.Parameters.Add(new SqliteParameter("@age", 25));
        cmd.Parameters.Add(new SqliteParameter("@createdAt", DateTime.UtcNow.ToString("o")));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    [TestMethod]
    public async Task LargeData_GetAllAsync_With1000Records_ShouldReturnAll()
    {
        // Arrange - Insert 1000 records
        await InsertUsersAsync(1000);

        // Act
        var users = await _queryRepo!.GetAllAsync(limit: 1000);

        // Assert
        Assert.AreEqual(1000, users.Count);
    }

    [TestMethod]
    public async Task LargeData_CountAsync_With1000Records_ShouldReturnCorrectCount()
    {
        // Arrange
        await InsertUsersAsync(1000);

        // Act
        var count = await _aggregateRepo!.CountAsync();

        // Assert
        Assert.AreEqual(1000L, count);
    }

    [TestMethod]
    public async Task LargeData_GetRangeAsync_Pagination_ShouldWorkCorrectly()
    {
        // Arrange
        await InsertUsersAsync(500);

        // Act - Get pages of 100
        var page1 = await _queryRepo!.GetRangeAsync(limit: 100, offset: 0);
        var page2 = await _queryRepo.GetRangeAsync(limit: 100, offset: 100);
        var page3 = await _queryRepo.GetRangeAsync(limit: 100, offset: 200);
        var page4 = await _queryRepo.GetRangeAsync(limit: 100, offset: 300);
        var page5 = await _queryRepo.GetRangeAsync(limit: 100, offset: 400);

        // Assert
        Assert.AreEqual(100, page1.Count);
        Assert.AreEqual(100, page2.Count);
        Assert.AreEqual(100, page3.Count);
        Assert.AreEqual(100, page4.Count);
        Assert.AreEqual(100, page5.Count);

        // Verify no duplicates across pages
        var allIds = page1.Concat(page2).Concat(page3).Concat(page4).Concat(page5)
            .Select(u => u.Id).ToList();
        Assert.AreEqual(500, allIds.Distinct().Count());
    }

    [TestMethod]
    public async Task LargeData_LongString_ShouldBeStoredAndRetrieved()
    {
        // Arrange - Create a very long string (10KB)
        var longDescription = new string('A', 10000);
        var id = await InsertUserWithLongStringAsync("LongStringUser", longDescription);

        // Act
        var user = await _queryRepo!.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(10000, user.Description?.Length);
        Assert.AreEqual(longDescription, user.Description);
    }

    [TestMethod]
    public async Task LargeData_VeryLongString_100KB_ShouldBeStoredAndRetrieved()
    {
        // Arrange - Create a 100KB string
        var veryLongDescription = new string('B', 100000);
        var id = await InsertUserWithLongStringAsync("VeryLongStringUser", veryLongDescription);

        // Act
        var user = await _queryRepo!.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(100000, user.Description?.Length);
    }

    [TestMethod]
    public async Task LargeData_GetWhereAsync_FilteringLargeDataset_ShouldWork()
    {
        // Arrange
        await InsertUsersAsync(1000);

        // Act - Filter for specific age range
        var filteredUsers = await _queryRepo!.GetWhereAsync(u => u.Age >= 30 && u.Age <= 40);

        // Assert - Should have users with ages 30-40 (11 different ages, ~220 users)
        Assert.IsTrue(filteredUsers.Count > 0);
        Assert.IsTrue(filteredUsers.All(u => u.Age >= 30 && u.Age <= 40));
    }

    [TestMethod]
    public async Task LargeData_SumAsync_LargeDataset_ShouldCalculateCorrectly()
    {
        // Arrange
        await InsertUsersAsync(1000);

        // Act
        var sumAge = await _aggregateRepo!.SumAsync("age");

        // Assert - Sum should be positive and reasonable
        Assert.IsTrue(sumAge > 0);
    }

    [TestMethod]
    public async Task LargeData_AvgAsync_LargeDataset_ShouldCalculateCorrectly()
    {
        // Arrange
        await InsertUsersAsync(1000);

        // Act
        var avgAge = await _aggregateRepo!.AvgAsync("age");

        // Assert - Average should be between 20 and 70 (our age range)
        Assert.IsTrue(avgAge >= 20 && avgAge <= 70);
    }
}
