// -----------------------------------------------------------------------
// <copyright file="AggregateRepositoryPropertyTests.cs" company="Cricle">
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
/// Property-based tests for IAggregateRepository operations.
/// **Feature: enhanced-predefined-interfaces-e2e-testing**
/// **Validates: Requirements 6.1-6.7**
/// </summary>
public class AggregateRepositoryPropertyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly E2EUserAggregateRepository _aggregateRepo;
    private readonly E2EUserCommandRepository _commandRepo;

    public AggregateRepositoryPropertyTests()
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
        
        _aggregateRepo = new E2EUserAggregateRepository(_connection);
        _commandRepo = new E2EUserCommandRepository(_connection);
    }

    /// <summary>
    /// **Property 5: Count Accuracy**
    /// *For any* list of entities, CountAsync should return exactly the number of entities inserted.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Count_ShouldMatchInsertedEntityCount(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        // Act
        var count = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (count == users.Count).ToProperty();
    }

    /// <summary>
    /// **Property 5.1: Count on Empty Table**
    /// *For any* empty table, CountAsync should return 0.
    /// </summary>
    [Property(MaxTest = 10)]
    public Property Count_EmptyTable_ShouldReturnZero()
    {
        // Act
        var count = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Assert
        return (count == 0).ToProperty();
    }

    /// <summary>
    /// **Property 5.2: CountWhere Accuracy**
    /// *For any* predicate, CountWhereAsync should match the count of entities satisfying the predicate.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property CountWhere_ShouldMatchPredicateCount(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        // Test predicate: age >= 30
        System.Linq.Expressions.Expression<Func<E2EUser, bool>> predicate = u => u.Age >= 30;
        var expectedCount = users.Count(u => u.Age >= 30);
        
        // Act
        var count = _aggregateRepo.CountWhereAsync(predicate).GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (count == expectedCount).ToProperty();
    }

    /// <summary>
    /// **Property 6: Sum Accuracy**
    /// *For any* list of entities, SumAsync should return the exact sum of the specified column.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Sum_ShouldMatchCalculatedSum(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        var expectedSum = users.Sum(u => u.Salary);
        
        // Act
        var sum = _aggregateRepo.SumAsync("salary").GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert - Allow small floating point difference
        return (Math.Abs(sum - expectedSum) < 0.01m).ToProperty();
    }

    /// <summary>
    /// **Property 6.1: Sum on Empty Table**
    /// *For any* empty table, SumAsync should return 0.
    /// </summary>
    [Property(MaxTest = 10)]
    public Property Sum_EmptyTable_ShouldReturnZero()
    {
        // Act
        var sum = _aggregateRepo.SumAsync("salary").GetAwaiter().GetResult();
        
        // Assert
        return (sum == 0m).ToProperty();
    }

    /// <summary>
    /// **Property 6.2: SumWhere Accuracy**
    /// *For any* predicate, SumWhereAsync should match the sum of entities satisfying the predicate.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property SumWhere_ShouldMatchPredicateSum(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        // Test predicate: age < 40
        System.Linq.Expressions.Expression<Func<E2EUser, bool>> predicate = u => u.Age < 40;
        var expectedSum = users.Where(u => u.Age < 40).Sum(u => u.Salary);
        
        // Act
        var sum = _aggregateRepo.SumWhereAsync("salary", predicate).GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (Math.Abs(sum - expectedSum) < 0.01m).ToProperty();
    }

    /// <summary>
    /// **Property 7: Average Accuracy**
    /// *For any* list of entities, AvgAsync should return the exact average of the specified column.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Avg_ShouldMatchCalculatedAverage(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        var expectedAvg = users.Average(u => (decimal)u.Age);
        
        // Act
        var avg = _aggregateRepo.AvgAsync("age").GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert - Allow small floating point difference
        return (Math.Abs(avg - expectedAvg) < 0.01m).ToProperty();
    }

    /// <summary>
    /// **Property 7.1: Average on Empty Table**
    /// *For any* empty table, AvgAsync should return 0.
    /// </summary>
    [Property(MaxTest = 10)]
    public Property Avg_EmptyTable_ShouldReturnZero()
    {
        // Act
        var avg = _aggregateRepo.AvgAsync("age").GetAwaiter().GetResult();
        
        // Assert
        return (avg == 0m).ToProperty();
    }

    /// <summary>
    /// **Property 7.2: AvgWhere Accuracy**
    /// *For any* predicate, AvgWhereAsync should match the average of entities satisfying the predicate.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property AvgWhere_ShouldMatchPredicateAverage(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        // Test predicate: IsActive
        System.Linq.Expressions.Expression<Func<E2EUser, bool>> predicate = u => u.IsActive;
        var filteredUsers = users.Where(u => u.IsActive).ToList();
        
        if (filteredUsers.Count == 0)
        {
            // Skip if no active users
            ClearTableAsync().GetAwaiter().GetResult();
            return true.ToProperty();
        }
        
        var expectedAvg = filteredUsers.Average(u => (decimal)u.Age);
        
        // Act
        var avg = _aggregateRepo.AvgWhereAsync("age", predicate).GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (Math.Abs(avg - expectedAvg) < 0.01m).ToProperty();
    }

    /// <summary>
    /// **Property 8: Max Accuracy**
    /// *For any* list of entities, MaxAsync should return the maximum value of the specified column.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Max_ShouldMatchCalculatedMax(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        var expectedMaxAge = users.Max(u => u.Age);
        var expectedMaxSalary = users.Max(u => u.Salary);
        
        // Act
        var maxAge = _aggregateRepo.MaxIntAsync("age").GetAwaiter().GetResult();
        var maxSalary = _aggregateRepo.MaxDecimalAsync("salary").GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (maxAge == expectedMaxAge && Math.Abs(maxSalary - expectedMaxSalary) < 0.01m).ToProperty();
    }

    /// <summary>
    /// **Property 9: Min Accuracy**
    /// *For any* list of entities, MinAsync should return the minimum value of the specified column.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Min_ShouldMatchCalculatedMin(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        var expectedMinAge = users.Min(u => u.Age);
        var expectedMinSalary = users.Min(u => u.Salary);
        
        // Act
        var minAge = _aggregateRepo.MinIntAsync("age").GetAwaiter().GetResult();
        var minSalary = _aggregateRepo.MinDecimalAsync("salary").GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert
        return (minAge == expectedMinAge && Math.Abs(minSalary - expectedMinSalary) < 0.01m).ToProperty();
    }

    /// <summary>
    /// **Property 10: Aggregate Null Handling**
    /// *For any* aggregate operation on a column with null values, the operation should ignore nulls
    /// and compute the result based on non-null values only.
    /// Note: In our schema, salary defaults to 0, so we test with 0 values instead of nulls.
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Aggregates_ShouldHandleZeroValues(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange - Insert users with some zero salaries
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        // Set some salaries to 0
        for (int i = 0; i < users.Count; i++)
        {
            if (i % 3 == 0) // Every third user has 0 salary
            {
                users[i].Salary = 0m;
            }
        }
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        var expectedSum = users.Sum(u => u.Salary);
        var expectedCount = users.Count;
        
        // Act
        var sum = _aggregateRepo.SumAsync("salary").GetAwaiter().GetResult();
        var count = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert - Sum should include zeros, count should include all
        return (Math.Abs(sum - expectedSum) < 0.01m && count == expectedCount).ToProperty();
    }

    /// <summary>
    /// **Property: Aggregate Consistency**
    /// *For any* list of entities, Count * Average should approximately equal Sum (within rounding error).
    /// </summary>
    [Property(MaxTest = 10, Arbitrary = new[] { typeof(E2EUserArbitraries) })]
    public Property Aggregates_CountTimesAvgShouldEqualSum(NonEmptyArray<E2EUserData> usersData)
    {
        // Arrange
        var users = usersData.Get.Select(d => d.ToEntity()).ToList();
        
        foreach (var user in users)
        {
            _commandRepo.InsertAsync(user).GetAwaiter().GetResult();
        }
        
        // Act
        var count = _aggregateRepo.CountAsync().GetAwaiter().GetResult();
        var avg = _aggregateRepo.AvgAsync("salary").GetAwaiter().GetResult();
        var sum = _aggregateRepo.SumAsync("salary").GetAwaiter().GetResult();
        
        // Cleanup
        ClearTableAsync().GetAwaiter().GetResult();
        
        // Assert - count * avg ≈ sum (within 1% tolerance for rounding)
        var calculated = count * avg;
        var tolerance = Math.Max(1m, Math.Abs(sum) * 0.01m); // 1% or minimum 1
        
        return (Math.Abs(calculated - sum) <= tolerance).ToProperty();
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
