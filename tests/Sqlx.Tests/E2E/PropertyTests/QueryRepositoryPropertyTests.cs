// -----------------------------------------------------------------------
// <copyright file="QueryRepositoryPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using Xunit;

namespace Sqlx.Tests.E2E.PropertyTests;

/// <summary>
/// Property-based tests for IQueryRepository operations.
/// **Feature: enhanced-predefined-interfaces-e2e-testing**
/// **Validates: Requirements 7.1, 7.2, 7.5, 7.7**
/// </summary>
public class QueryRepositoryPropertyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly E2EUserQueryRepository _repo;

    public QueryRepositoryPropertyTests()
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
        
        _repo = new E2EUserQueryRepository(_connection);
    }

    /// <summary>
    /// **Property 1: Entity Round-Trip Preservation**
    /// *For any* valid entity, inserting and then retrieving by ID should return an entity
    /// with the same property values (except auto-generated ID).
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property EntityRoundTrip_ShouldPreserveAllProperties(E2EUserData userData)
    {
        // Arrange
        var user = userData.ToEntity();
        var commandRepo = new E2EUserCommandRepository(_connection);
        
        // Act
        var id = commandRepo.InsertAndGetIdAsync(user).GetAwaiter().GetResult();
        var retrieved = _repo.GetByIdAsync(id).GetAwaiter().GetResult();
        
        // Assert
        return (retrieved != null &&
               retrieved.Name == user.Name &&
               retrieved.Email == user.Email &&
               retrieved.Age == user.Age &&
               Math.Abs(retrieved.Salary - user.Salary) < 0.01m &&
               retrieved.IsActive == user.IsActive)
            .ToProperty();
    }

    /// <summary>
    /// **Property 11: GetByIds Correctness**
    /// *For any* list of IDs, GetByIdsAsync should return exactly the entities with those IDs
    /// that exist in the database (no more, no less).
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property GetByIds_ShouldReturnExactlyRequestedEntities(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        var commandRepo = new E2EUserCommandRepository(_connection);
        var insertedIds = new List<long>();
        
        foreach (var user in users)
        {
            var id = commandRepo.InsertAndGetIdAsync(user).GetAwaiter().GetResult();
            insertedIds.Add(id);
        }
        
        // Request subset of IDs plus some non-existent ones
        var requestedIds = insertedIds.Take(Math.Max(1, insertedIds.Count / 2)).ToList();
        requestedIds.Add(99999); // Non-existent ID
        
        // Act
        var retrieved = _repo.GetByIdsAsync(requestedIds).GetAwaiter().GetResult();
        
        // Assert - should only return existing IDs
        var retrievedIds = retrieved.Select(u => u.Id).ToHashSet();
        var expectedIds = requestedIds.Where(id => insertedIds.Contains(id)).ToHashSet();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        return retrievedIds.SetEquals(expectedIds).ToProperty();
    }

    /// <summary>
    /// **Property 12: Pagination Correctness**
    /// *For any* valid limit and offset, GetRangeAsync should return at most 'limit' entities,
    /// and the union of all pages should equal GetAllAsync (no duplicates, no missing entities).
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Pagination_ShouldReturnCorrectSubsetsWithoutDuplicates(
        NonEmptyArray<E2EUserData> usersData,
        PositiveInt pageSize)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        var commandRepo = new E2EUserCommandRepository(_connection);
        
        foreach (var user in users)
        {
            commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        var limit = Math.Max(1, Math.Min(pageSize.Get, 10)); // Limit page size to 1-10
        
        // Act - Get all pages
        var allPages = new List<E2EUser>();
        var offset = 0;
        List<E2EUser> page;
        
        do
        {
            page = _repo.GetRangeAsync(limit, offset).GetAwaiter().GetResult();
            allPages.AddRange(page);
            offset += limit;
        } while (page.Count == limit && offset < users.Count * 2); // Safety limit
        
        var allEntities = _repo.GetAllAsync().GetAwaiter().GetResult();
        
        // Assert
        var allPagesIds = allPages.Select(u => u.Id).ToList();
        var allEntitiesIds = allEntities.Select(u => u.Id).ToList();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // No duplicates in paginated results
        var noDuplicates = allPagesIds.Count == allPagesIds.Distinct().Count();
        
        // All entities retrieved
        var allRetrieved = allPagesIds.OrderBy(id => id).SequenceEqual(allEntitiesIds.OrderBy(id => id));
        
        return (noDuplicates && allRetrieved).ToProperty();
    }

    /// <summary>
    /// **Property 13: Predicate Filtering Correctness**
    /// *For any* predicate, GetWhereAsync should return exactly the entities that satisfy
    /// the predicate when evaluated in-memory (no false positives, no false negatives).
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property PredicateFiltering_ShouldMatchInMemoryEvaluation(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        var commandRepo = new E2EUserCommandRepository(_connection);
        
        foreach (var user in users)
        {
            commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        // Test a simple predicate (GetWhereAsync requires Expression<Func<T, bool>>)
        System.Linq.Expressions.Expression<Func<E2EUser, bool>> predicate = u => u.Age >= 25;
        
        // Act
        var dbResults = _repo.GetWhereAsync(predicate).GetAwaiter().GetResult();
        var allEntities = _repo.GetAllAsync().GetAwaiter().GetResult();
        var memoryResults = allEntities.Where(predicate.Compile()).ToList();
        
        // Assert
        var dbIds = dbResults.Select(u => u.Id).OrderBy(id => id).ToList();
        var memoryIds = memoryResults.Select(u => u.Id).OrderBy(id => id).ToList();
        
        var result = dbIds.SequenceEqual(memoryIds);
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        return result.ToProperty();
    }

    private async Task ClearTableAsync()
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

/// <summary>
/// Test data class for property-based testing.
/// </summary>
public class E2EUserData
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }

    public E2EUser ToEntity()
    {
        return new E2EUser
        {
            Name = Name,
            Email = Email,
            Age = Age,
            Salary = Salary,
            IsActive = IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// FsCheck arbitrary generators for E2E user data.
/// </summary>
public static class E2EUserArbitraries
{
    private static readonly string[] FirstNames = { "Alice", "Bob", "Charlie", "David", "Eve", "Frank", "Grace", "Henry" };
    private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };

    public static Arbitrary<E2EUserData> E2EUserData()
    {
        var gen = from firstName in Gen.Elements(FirstNames)
                  from lastName in Gen.Elements(LastNames)
                  from age in Gen.Choose(18, 70)
                  from salary in Gen.Choose(30000, 150000)
                  from isActive in Arb.Generate<bool>()
                  select new E2EUserData
                  {
                      Name = $"{firstName} {lastName}",
                      Email = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
                      Age = age,
                      Salary = salary,
                      IsActive = isActive
                  };

        return gen.ToArbitrary();
    }
}
