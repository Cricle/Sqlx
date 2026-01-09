// -----------------------------------------------------------------------
// <copyright file="PostgreSQLFeaturePropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Sqlx;
using Sqlx.Tests.E2E;
using Sqlx.Tests.Infrastructure;
using Xunit;

namespace Sqlx.Tests.E2E.PropertyTests;

/// <summary>
/// Property-based tests for PostgreSQL-specific features.
/// **Feature: enhanced-predefined-interfaces-e2e-testing**
/// **Validates: Requirements 4.1, 4.2, 4.8, 4.9**
/// </summary>
public class PostgreSQLFeaturePropertyTests : IDisposable
{
    private readonly IDbConnection? _connection;
    private readonly E2EUserCommandRepository_PostgreSQL? _commandRepo;
    private readonly E2EUserQueryRepository_PostgreSQL? _queryRepo;
    private readonly E2EUserBatchRepository_PostgreSQL? _batchRepo;
    private readonly bool _isAvailable;
    private const string TestClassName = nameof(PostgreSQLFeaturePropertyTests);

    public PostgreSQLFeaturePropertyTests()
    {
        _connection = DatabaseConnectionHelper.GetPostgreSQLConnection(TestClassName);
        _isAvailable = _connection != null;

        if (_isAvailable)
        {
            // Create table
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS e2e_users (
                    id BIGSERIAL PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    salary NUMERIC NOT NULL DEFAULT 0,
                    is_active BOOLEAN NOT NULL DEFAULT true,
                    is_deleted BOOLEAN NOT NULL DEFAULT false,
                    created_at TIMESTAMP NOT NULL,
                    updated_at TIMESTAMP,
                    deleted_at TIMESTAMP
                )";
            cmd.ExecuteNonQuery();

            _commandRepo = new E2EUserCommandRepository_PostgreSQL(_connection);
            _queryRepo = new E2EUserQueryRepository_PostgreSQL(_connection);
            _batchRepo = new E2EUserBatchRepository_PostgreSQL(_connection);
        }
    }

    /// <summary>
    /// **Property 18: Upsert Idempotence**
    /// *For any* entity, upserting it multiple times should produce the same result
    /// as upserting it once (idempotent operation).
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Upsert_ShouldBeIdempotent(E2EUserData userData)
    {
        if (!_isAvailable) return true.ToProperty();

        // Arrange
        var user = userData.ToEntity();
        var id = _commandRepo!.InsertAndGetIdAsync(user).GetAwaiter().GetResult();
        user.Id = id;

        // Modify the entity
        user.Name = "Upserted Name";
        user.Age = 88;
        user.Salary = 88888m;

        // Act - Upsert multiple times
        _commandRepo.UpsertAsync(user).GetAwaiter().GetResult();
        var afterFirstUpsert = _queryRepo!.GetByIdAsync(id).GetAwaiter().GetResult();

        _commandRepo.UpsertAsync(user).GetAwaiter().GetResult();
        var afterSecondUpsert = _queryRepo.GetByIdAsync(id).GetAwaiter().GetResult();

        _commandRepo.UpsertAsync(user).GetAwaiter().GetResult();
        var afterThirdUpsert = _queryRepo.GetByIdAsync(id).GetAwaiter().GetResult();

        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();

        // Assert - All upserts should produce the same result
        var result = afterFirstUpsert != null &&
                afterSecondUpsert != null &&
                afterThirdUpsert != null &&
                afterFirstUpsert.Name == afterSecondUpsert.Name &&
                afterSecondUpsert.Name == afterThirdUpsert.Name &&
                afterFirstUpsert.Age == afterSecondUpsert.Age &&
                afterSecondUpsert.Age == afterThirdUpsert.Age &&
                Math.Abs(afterFirstUpsert.Salary - afterSecondUpsert.Salary) < 0.01m &&
                Math.Abs(afterSecondUpsert.Salary - afterThirdUpsert.Salary) < 0.01m;

        return result
            .Label($"Upsert idempotence: Name={afterFirstUpsert?.Name}, Age={afterFirstUpsert?.Age}");
    }

    /// <summary>
    /// **Property 30: Boolean Preservation**
    /// *For any* entity with boolean fields, inserting and retrieving should preserve
    /// the exact boolean values (true remains true, false remains false).
    /// **Validates: Requirements 4.8**
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Boolean_ShouldBePreservedInRoundTrip(E2EUserData userData)
    {
        if (!_isAvailable) return true.ToProperty();

        // Arrange
        var user = userData.ToEntity();
        var originalIsActive = user.IsActive;
        var originalIsDeleted = user.IsDeleted;

        // Act - Insert and retrieve
        var id = _commandRepo!.InsertAndGetIdAsync(user).GetAwaiter().GetResult();
        var retrieved = _queryRepo!.GetByIdAsync(id).GetAwaiter().GetResult();

        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();

        // Assert - Boolean values should be preserved exactly
        var result = retrieved != null &&
                retrieved.IsActive == originalIsActive &&
                retrieved.IsDeleted == originalIsDeleted;

        return result
            .Label($"Boolean preservation: IsActive={originalIsActive}, IsDeleted={originalIsDeleted}");
    }

    /// <summary>
    /// **Property 30b: Boolean Preservation in Batch Operations**
    /// *For any* list of entities with boolean fields, batch inserting and retrieving
    /// should preserve all boolean values correctly.
    /// **Validates: Requirements 4.8**
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Boolean_ShouldBePreservedInBatchOperations(NonEmptyArray<E2EUserData> userDataArray)
    {
        if (!_isAvailable) return true.ToProperty();

        // Arrange - Make names unique to ensure proper matching
        var users = userDataArray.Get.Select((ud, index) => 
        {
            var user = ud.ToEntity();
            user.Name = $"{user.Name}_{index}"; // Add index to make names unique
            return user;
        }).ToList();

        // Act - Batch insert and retrieve all
        _batchRepo!.BatchInsertAsync(users).GetAwaiter().GetResult();
        var retrieved = _queryRepo!.GetAllAsync().GetAwaiter().GetResult();

        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();

        // Assert - All boolean values should be preserved
        // Match by name (now guaranteed unique)
        var allPreserved = retrieved.Count == users.Count &&
                          users.All(original =>
                          {
                              var match = retrieved.FirstOrDefault(r => r.Name == original.Name);
                              return match != null &&
                                     match.IsActive == original.IsActive &&
                                     match.IsDeleted == original.IsDeleted;
                          });

        return allPreserved
            .Label($"Batch boolean preservation: {users.Count} users, {retrieved.Count} retrieved");
    }

    /// <summary>
    /// **Property 32: Random Query Validity**
    /// *For any* non-empty table, GetRandomAsync should always return valid entities
    /// that exist in the table.
    /// **Validates: Requirements 4.9**
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property GetRandom_ShouldReturnValidEntities(PositiveInt count, NonEmptyArray<E2EUserData> userDataArray)
    {
        if (!_isAvailable) return true.ToProperty();

        // Arrange - Insert multiple users
        var users = userDataArray.Get.Select(ud => ud.ToEntity()).ToList();
        _batchRepo!.BatchInsertAsync(users).GetAwaiter().GetResult();

        var allUsers = _queryRepo!.GetAllAsync().GetAwaiter().GetResult();
        var requestCount = Math.Min(count.Get % 10 + 1, allUsers.Count); // Request 1-10 random users

        // Act - Get random users
        var randomUsers = _queryRepo.GetRandomAsync(requestCount).GetAwaiter().GetResult();

        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();

        // Assert - All random users should be valid and exist in the original set
        var allValid = randomUsers != null &&
                      randomUsers.Count <= requestCount &&
                      randomUsers.All(r => allUsers.Any(u =>
                          u.Name == r.Name &&
                          u.Email == r.Email &&
                          u.Age == r.Age));

        return allValid
            .Label($"Random query: requested={requestCount}, returned={randomUsers?.Count}, total={allUsers.Count}");
    }

    /// <summary>
    /// **Property 32b: Random Query Uniqueness**
    /// *For any* table with sufficient records, GetRandomAsync should tend to return
    /// different results on multiple calls (statistical property).
    /// **Validates: Requirements 4.9**
    /// </summary>
    [Property(MaxTest = 5, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property GetRandom_ShouldTendToReturnDifferentResults(NonEmptyArray<E2EUserData> userDataArray)
    {
        if (!_isAvailable) return true.ToProperty();

        // Arrange - Insert at least 10 users for meaningful randomness
        var users = userDataArray.Get.Take(Math.Max(10, userDataArray.Get.Length)).Select(ud => ud.ToEntity()).ToList();
        if (users.Count < 10)
        {
            // Pad with additional users if needed
            for (int i = users.Count; i < 10; i++)
            {
                users.Add(new E2EUser
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + i,
                    Salary = 50000 + (i * 1000),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _batchRepo!.BatchInsertAsync(users).GetAwaiter().GetResult();

        // Act - Get random users multiple times
        var random1 = _queryRepo!.GetRandomAsync(3).GetAwaiter().GetResult();
        var random2 = _queryRepo.GetRandomAsync(3).GetAwaiter().GetResult();
        var random3 = _queryRepo.GetRandomAsync(3).GetAwaiter().GetResult();

        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();

        // Assert - At least one call should return different results (statistical property)
        // We check if the first user in each result is different
        var hasVariation = random1.Count > 0 && random2.Count > 0 && random3.Count > 0 &&
                          (random1[0].Id != random2[0].Id ||
                           random2[0].Id != random3[0].Id ||
                           random1[0].Id != random3[0].Id);

        return hasVariation
            .Label($"Random variation: IDs=[{random1[0].Id}, {random2[0].Id}, {random3[0].Id}]");
    }

    /// <summary>
    /// **Property 18b: Batch Upsert Correctness**
    /// *For any* list of entities (mix of existing and new), batch upsert should
    /// correctly update existing records and insert new ones.
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property BatchUpsert_ShouldCorrectlyHandleMixedOperations(NonEmptyArray<E2EUserData> existingData, NonEmptyArray<E2EUserData> newData)
    {
        if (!_isAvailable) return true.ToProperty();

        // Arrange - Insert existing users
        var existingUsers = existingData.Get.Select(ud => ud.ToEntity()).ToList();
        _batchRepo!.BatchInsertAsync(existingUsers).GetAwaiter().GetResult();

        var insertedUsers = _queryRepo!.GetAllAsync().GetAwaiter().GetResult();

        // Prepare upsert batch: modify existing + add new
        var upsertBatch = new List<E2EUser>();

        // Add modified existing users (deduplicate by ID to avoid PostgreSQL constraint)
        var usersToUpdate = insertedUsers.Take(Math.Min(3, insertedUsers.Count)).ToList();
        foreach (var user in usersToUpdate)
        {
            user.Name = $"Updated_{user.Name}";
            user.Age += 10;
            upsertBatch.Add(user);
        }

        // Add new users (ensure no duplicate IDs)
        var newUsers = newData.Get.Take(3).Select(ud => ud.ToEntity()).ToList();
        upsertBatch.AddRange(newUsers);

        // Deduplicate by ID (keep last occurrence) - PostgreSQL doesn't allow ON CONFLICT to affect same row twice
        var deduplicatedBatch = upsertBatch
            .GroupBy(u => u.Id)
            .Select(g => g.Last())
            .ToList();

        var originalCount = insertedUsers.Count;
        var updateCount = deduplicatedBatch.Count(u => u.Id > 0);
        var insertCount = deduplicatedBatch.Count(u => u.Id == 0);

        // Act - Batch upsert
        _batchRepo.BatchUpsertAsync(deduplicatedBatch).GetAwaiter().GetResult();

        var finalUsers = _queryRepo.GetAllAsync().GetAwaiter().GetResult();

        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();

        // Assert
        var expectedCount = originalCount + insertCount;
        var updatedUsersExist = deduplicatedBatch
            .Where(u => u.Id > 0)
            .All(u => finalUsers.Any(f => f.Id == u.Id && f.Name == u.Name));

        return (finalUsers.Count == expectedCount && updatedUsersExist)
            .Label($"Batch upsert: original={originalCount}, updates={updateCount}, inserts={insertCount}, final={finalUsers.Count}");
    }

    private async System.Threading.Tasks.Task ClearTableAsync()
    {
        if (_connection == null) return;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "TRUNCATE TABLE e2e_users RESTART IDENTITY";
        await System.Threading.Tasks.Task.Run(() => cmd.ExecuteNonQuery());
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            ClearTableAsync().GetAwaiter().GetResult();
            _connection.Dispose();
            DatabaseConnectionHelper.CleanupDatabaseAsync(TestClassName).GetAwaiter().GetResult();
        }
    }
}
