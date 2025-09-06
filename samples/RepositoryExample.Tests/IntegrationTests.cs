// -----------------------------------------------------------------------
// <copyright file="IntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests that verify end-to-end functionality.
/// </summary>
public class IntegrationTests : TestBase
{
    [Fact]
    public async Task FullRepositoryWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository = new AdvancedSQLiteUserRepository(Connection);

        // Act & Assert - Complete workflow
        
        // 1. Start with empty database
        var initialUsers = await repository.GetAllUsersAsync();
        initialUsers.Should().BeEmpty();

        // 2. Create users individually
        var user1 = CreateValidUser(1);
        var user2 = CreateValidUser(2);
        
        var createResult1 = await repository.CreateUserAsync(user1);
        var createResult2 = await repository.CreateUserAsync(user2);
        
        createResult1.Should().Be(1);
        createResult2.Should().Be(1);

        // 3. Verify creation
        var allUsers = await repository.GetAllUsersAsync();
        allUsers.Should().HaveCount(2);

        // 4. Create users in batch
        var batchUsers = new List<User>
        {
            CreateValidUser(3),
            CreateValidUser(4),
            CreateValidUser(5)
        };
        
        var batchResult = await repository.CreateUsersBatchAsync(batchUsers);
        batchResult.Should().Be(3);

        // 5. Verify total count
        allUsers = await repository.GetAllUsersAsync();
        allUsers.Should().HaveCount(5);

        // 6. Test pagination
        var (firstPage, totalCount) = await repository.GetUsersPaginatedAsync(1, 3);
        firstPage.Should().HaveCount(3);
        totalCount.Should().Be(5);

        var (secondPage, _) = await repository.GetUsersPaginatedAsync(2, 3);
        secondPage.Should().HaveCount(2);

        // 7. Test search functionality
        var searchResults = repository.SearchUsers("Test User", UserSearchType.Name);
        searchResults.Should().HaveCount(5);

        var emailSearch = repository.SearchUsers("@example.com", UserSearchType.Email);
        emailSearch.Should().HaveCount(5);

        // 8. Test statistics
        var stats = repository.GetUserStatistics();
        stats.TotalUsers.Should().Be(5);
        stats.FirstUserCreated.Should().NotBeNull();
        stats.LastUserCreated.Should().NotBeNull();

        // 9. Test transfer operation
        var transferResult = await repository.TransferUserDataAsync(1, 2, "integration test transfer");
        transferResult.Should().BeTrue();

        // 10. Update a user
        var userToUpdate = await repository.GetUserByIdAsync(3);
        userToUpdate.Should().NotBeNull();
        userToUpdate!.Name = "Updated Integration Test User";
        
        var updateResult = repository.UpdateUser(userToUpdate);
        updateResult.Should().Be(1);

        // 11. Verify update
        var updatedUser = await repository.GetUserByIdAsync(3);
        updatedUser!.Name.Should().Be("Updated Integration Test User");

        // 12. Delete a user
        var deleteResult = repository.DeleteUser(5);
        deleteResult.Should().Be(1);

        // 13. Verify final state
        var finalUsers = await repository.GetAllUsersAsync();
        finalUsers.Should().HaveCount(4); // Started with 5, deleted 1

        var finalStats = repository.GetUserStatistics();
        finalStats.TotalUsers.Should().Be(4);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository = new AdvancedSQLiteUserRepository(Connection);

        // Act - Perform concurrent operations
        var tasks = new List<Task>();

        // Concurrent reads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(repository.GetAllUsersAsync());
        }

        // Concurrent writes
        for (int i = 0; i < 5; i++)
        {
            var user = CreateValidUser(i + 100); // Use unique IDs
            tasks.Add(repository.CreateUserAsync(user));
        }

        // Wait for all operations to complete
        await Task.WhenAll(tasks);

        // Assert - Verify data integrity
        var finalUsers = await repository.GetAllUsersAsync();
        finalUsers.Should().HaveCount(5); // All 5 writes should have succeeded

        var stats = repository.GetUserStatistics();
        stats.TotalUsers.Should().Be(5);
    }

    [Fact]
    public async Task StressTest_LargeDataSet_ShouldPerformWell()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository = new AdvancedSQLiteUserRepository(Connection);
        const int userCount = 1000;

        // Act - Create large dataset
        var users = new List<User>();
        for (int i = 1; i <= userCount; i++)
        {
            users.Add(new User 
            { 
                Name = $"Stress Test User {i}", 
                Email = $"stress{i}@example.com", 
                CreatedAt = DateTime.Now.AddMinutes(-i) 
            });
        }

        var startTime = DateTime.Now;
        
        // Batch create users
        var batchSize = 100;
        for (int i = 0; i < userCount; i += batchSize)
        {
            var batch = users.Skip(i).Take(batchSize);
            await repository.CreateUsersBatchAsync(batch);
        }

        var createTime = DateTime.Now;

        // Test various operations on large dataset
        var allUsers = await repository.GetAllUsersAsync();
        var (paginatedUsers, totalCount) = await repository.GetUsersPaginatedAsync(1, 50);
        var searchResults = repository.SearchUsers("Stress", UserSearchType.Name);
        var stats = repository.GetUserStatistics();

        var queryTime = DateTime.Now;

        // Assert - Verify performance and correctness
        allUsers.Should().HaveCount(userCount);
        paginatedUsers.Should().HaveCount(50);
        totalCount.Should().Be(userCount);
        searchResults.Should().HaveCount(userCount);
        stats.TotalUsers.Should().Be(userCount);

        // Performance assertions
        var createDuration = createTime - startTime;
        var queryDuration = queryTime - createTime;

        createDuration.TotalSeconds.Should().BeLessThan(30); // Should create 1000 users in less than 30 seconds
        queryDuration.TotalSeconds.Should().BeLessThan(10);  // Should query in less than 10 seconds
    }

    [Fact]
    public async Task TransactionRollback_OnError_ShouldMaintainDataIntegrity()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository = new AdvancedSQLiteUserRepository(Connection);

        // Create initial data
        var validUser = CreateValidUser(1);
        await repository.CreateUserAsync(validUser);

        var initialCount = await GetUserCountAsync();
        initialCount.Should().Be(1);

        // Act - Attempt batch operation with invalid data (should rollback)
        var batchWithError = new List<User>
        {
            CreateValidUser(2),
            CreateValidUser(3),
            CreateInvalidUser(), // This should cause the entire batch to fail
            CreateValidUser(4)
        };

        var act = async () => await repository.CreateUsersBatchAsync(batchWithError);

        // Assert - Should throw and maintain original state
        await act.Should().ThrowAsync<ArgumentException>();

        var finalCount = await GetUserCountAsync();
        finalCount.Should().Be(1); // Should remain unchanged due to rollback
    }

    [Fact]
    public async Task ErrorRecovery_AfterFailedOperation_ShouldContinueWorking()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository = new AdvancedSQLiteUserRepository(Connection);

        // Act & Assert
        
        // 1. Attempt invalid operation
        var invalidUser = CreateInvalidUser();
        var act = async () => await repository.CreateUserAsync(invalidUser);
        await act.Should().ThrowAsync<ArgumentException>();

        // 2. Verify repository still works after error
        var validUser = CreateValidUser(1);
        var createResult = await repository.CreateUserAsync(validUser);
        createResult.Should().Be(1);

        // 3. Verify normal operations continue to work
        var users = await repository.GetAllUsersAsync();
        users.Should().HaveCount(1);

        var userById = await repository.GetUserByIdAsync(1);
        userById.Should().NotBeNull();

        var stats = repository.GetUserStatistics();
        stats.TotalUsers.Should().Be(1);
    }

    [Fact]
    public async Task MemoryEfficiency_MultipleOperations_ShouldNotLeak()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository = new AdvancedSQLiteUserRepository(Connection);

        // Act - Perform many operations to test for memory leaks
        var initialMemory = GC.GetTotalMemory(true);

        for (int iteration = 0; iteration < 100; iteration++)
        {
            // Create and delete users repeatedly
            var user = CreateValidUser(iteration);
            await repository.CreateUserAsync(user);
            
            var users = await repository.GetAllUsersAsync();
            users.Should().NotBeEmpty();
            
            repository.DeleteUser(user.Id);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - Memory usage should not grow significantly
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessThan(10 * 1024 * 1024); // Less than 10MB growth
    }

    [Fact]
    public async Task DataConsistency_MultipleRepositoryInstances_ShouldShareData()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var repository1 = new AdvancedSQLiteUserRepository(Connection);
        var repository2 = new AdvancedSQLiteUserRepository(Connection);

        // Act
        // Create user with first repository
        var user = CreateValidUser(1);
        await repository1.CreateUserAsync(user);

        // Read with second repository
        var usersFromRepo2 = await repository2.GetAllUsersAsync();
        var userFromRepo2 = await repository2.GetUserByIdAsync(1);

        // Modify with second repository
        userFromRepo2!.Name = "Modified by Repo2";
        repository2.UpdateUser(userFromRepo2);

        // Verify with first repository
        var modifiedUserFromRepo1 = await repository1.GetUserByIdAsync(1);

        // Assert
        usersFromRepo2.Should().HaveCount(1);
        userFromRepo2.Should().NotBeNull();
        modifiedUserFromRepo1.Should().NotBeNull();
        modifiedUserFromRepo1!.Name.Should().Be("Modified by Repo2");
    }
}
