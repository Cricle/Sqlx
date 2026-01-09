// -----------------------------------------------------------------------
// <copyright file="BatchRepositoryPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Tests.E2E;
using Xunit;

namespace Sqlx.Tests.E2E.PropertyTests;

/// <summary>
/// Property-based tests for IBatchRepository operations.
/// **Feature: enhanced-predefined-interfaces-e2e-testing**
/// **Validates: Requirements 5.1, 5.2, 5.3**
/// </summary>
public class BatchRepositoryPropertyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly E2EUserBatchRepository _batchRepo;
    private readonly E2EUserQueryRepository _queryRepo;
    private readonly E2EUserAggregateRepository _aggregateRepo;

    public BatchRepositoryPropertyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        // Create table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE e2e_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT,
                age INTEGER NOT NULL,
                salary REAL NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT,
                deleted_at TEXT
            )";
        cmd.ExecuteNonQuery();
        
        _batchRepo = new E2EUserBatchRepository(_connection);
        _queryRepo = new E2EUserQueryRepository(_connection);
        _aggregateRepo = new E2EUserAggregateRepository(_connection);
    }

    /// <summary>
    /// **Property 2: Batch Insert Completeness**
    /// *For any* list of entities, BatchInsertAsync should insert all entities,
    /// and the count should increase by exactly the list size.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchInsert_ShouldInsertAllEntities(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Act
        var affected = _batchRepo.BatchInsertAsync(users).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == users.Count && countAfter == countBefore + users.Count).ToProperty();
    }

    /// <summary>
    /// **Property 2.1: Batch Insert with Empty List**
    /// *For any* empty list, BatchInsertAsync should return 0 and not modify the database.
    /// </summary>
    [Property(MaxTest = 10)]
    public Property BatchInsert_EmptyList_ShouldReturnZero()
    {
        // Arrange
        var users = new List<E2EUser>();
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Act
        var affected = _batchRepo.BatchInsertAsync(users).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == 0 && countAfter == countBefore).ToProperty();
    }

    /// <summary>
    /// **Property 2.2: Batch Insert with Single Item**
    /// *For any* single entity, BatchInsertAsync should work correctly.
    /// </summary>
    [Property(MaxTest = 5, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchInsert_SingleItem_ShouldInsert(E2EUserData userData)
    {
        // Arrange
        var users = new List<E2EUser> { userData.ToEntity() };
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Act
        var affected = _batchRepo.BatchInsertAsync(users).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == 1 && countAfter == countBefore + 1).ToProperty();
    }

    /// <summary>
    /// **Property 2.3: Batch Insert Large Collection**
    /// *For any* large collection (100+ items), BatchInsertAsync should handle it correctly.
    /// </summary>
    [Property(MaxTest = 3, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchInsert_LargeCollection_ShouldInsertAll(PositiveInt size)
    {
        // Arrange - Generate 50-200 users
        var count = Math.Max(50, Math.Min(size.Get, 200));
        var users = new List<E2EUser>();
        for (int i = 0; i < count; i++)
        {
            users.Add(new E2EUser
            {
                Name = $"User{i}",
                Age = 20 + (i % 50),
                Salary = 50000m + (i * 100),
                CreatedAt = DateTime.UtcNow
            });
        }
        
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Act
        var affected = _batchRepo.BatchInsertAsync(users).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == count && countAfter == countBefore + count).ToProperty();
    }

    /// <summary>
    /// **Property 4: Batch Delete Completeness**
    /// *For any* list of IDs, BatchDeleteAsync should delete exactly those entities that exist,
    /// and the count should decrease by the number of existing IDs.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchDelete_ShouldDeleteAllExistingEntities(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange - Insert entities
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        var commandRepo = new E2EUserCommandRepository(_connection);
        var insertedIds = new List<long>();
        
        foreach (var user in users)
        {
            var id = commandRepo.InsertAndGetIdAsync(user).GetAwaiter().GetResult();
            insertedIds.Add(id);
        }
        
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Delete subset of IDs plus some non-existent ones
        var idsToDelete = insertedIds.Take(Math.Max(1, insertedIds.Count / 2)).ToList();
        idsToDelete.Add(99999); // Non-existent ID
        idsToDelete.Add(88888); // Another non-existent ID
        
        var expectedDeleteCount = idsToDelete.Count(id => insertedIds.Contains(id));
        
        // Act
        var affected = _batchRepo.BatchDeleteAsync(idsToDelete).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == expectedDeleteCount && 
                countAfter == countBefore - expectedDeleteCount).ToProperty();
    }

    /// <summary>
    /// **Property 4.1: Batch Delete with Empty List**
    /// *For any* empty list, BatchDeleteAsync should return 0 and not modify the database.
    /// </summary>
    [Property(MaxTest = 5, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchDelete_EmptyList_ShouldReturnZero(E2EUserData userData)
    {
        // Arrange - Insert one entity to ensure table is not empty
        var user = userData.ToEntity();
        var commandRepo = new E2EUserCommandRepository(_connection);
        commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Act
        var affected = _batchRepo.BatchDeleteAsync(new List<long>()).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == 0 && countAfter == countBefore).ToProperty();
    }

    /// <summary>
    /// **Property 4.2: Batch Delete Non-Existent IDs**
    /// *For any* list of non-existent IDs, BatchDeleteAsync should return 0.
    /// </summary>
    [Property(MaxTest = 10)]
    public Property BatchDelete_NonExistentIds_ShouldReturnZero()
    {
        // Arrange
        var nonExistentIds = new List<long> { 99999, 88888, 77777 };
        var countBefore = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Act
        var affected = _batchRepo.BatchDeleteAsync(nonExistentIds).GetAwaiter().GetResult();
        var countAfter = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Assert
        return (affected == 0 && countAfter == countBefore).ToProperty();
    }

    /// <summary>
    /// **Property: Batch Insert and Get IDs Completeness**
    /// *For any* list of entities, BatchInsertAndGetIdsAsync should return exactly as many IDs
    /// as entities inserted, and all IDs should be unique and valid.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchInsertAndGetIds_ShouldReturnUniqueValidIds(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        // Act
        var ids = _batchRepo.BatchInsertAndGetIdsAsync(users).GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        var correctCount = ids.Count == users.Count;
        var allPositive = ids.All(id => id > 0);
        var allUnique = ids.Count == ids.Distinct().Count();
        
        return (correctCount && allPositive && allUnique).ToProperty();
    }

    private async System.Threading.Tasks.Task ClearTableAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM e2e_users";
        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
