// -----------------------------------------------------------------------
// <copyright file="AdvancedSQLiteUserRepositoryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for AdvancedSQLiteUserRepository.
/// </summary>
public class AdvancedSQLiteUserRepositoryTests : TestBase
{
    private AdvancedSQLiteUserRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedSQLiteUserRepositoryTests"/> class.
    /// </summary>
    public AdvancedSQLiteUserRepositoryTests()
    {
        _repository = new AdvancedSQLiteUserRepository(Connection);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _repository = null!;
        }
        base.Dispose(disposing);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AdvancedSQLiteUserRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public void Constructor_WithValidConnection_ShouldCreateRepository()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");

        // Act
        var repository = new AdvancedSQLiteUserRepository(connection);

        // Assert
        repository.Should().NotBeNull();
    }

    #endregion

    #region Basic CRUD Tests (inherited from IUserService)

    [Fact]
    public async Task GetAllUsers_ShouldReturnOrderedByCreatedAtDesc()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.GetAllUsers();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        // Should be ordered by CreatedAt DESC (newest first)
        result.First().Name.Should().Be("Test User 3"); // Most recent (-5 days)
        result.Last().Name.Should().Be("Test User 1"); // Oldest (-30 days)
    }

    [Fact]
    public async Task CreateUser_WithInvalidUser_ShouldThrowArgumentException()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var invalidUser = CreateInvalidUser();

        // Act & Assert
        var act = () => _repository.CreateUser(invalidUser);
        act.Should().Throw<ArgumentException>().WithMessage("User validation failed*");
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidUser_ShouldThrowArgumentException()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var invalidUser = CreateInvalidUser();

        // Act & Assert
        var act = async () => await _repository.CreateUserAsync(invalidUser);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("User validation failed*");
    }

    [Fact]
    public async Task UpdateUser_WithInvalidUser_ShouldThrowArgumentException()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var invalidUser = CreateInvalidUser();
        invalidUser.Id = 1; // Set ID for update

        // Act & Assert
        var act = () => _repository.UpdateUser(invalidUser);
        act.Should().Throw<ArgumentException>().WithMessage("User validation failed*");
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateUsersBatch_WithValidUsers_ShouldCreateAllUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var users = new List<User>
        {
            CreateValidUser(1),
            CreateValidUser(2),
            CreateValidUser(3)
        };

        // Act
        var result = _repository.CreateUsersBatch(users);

        // Assert
        result.Should().Be(3);
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(3);
    }

    [Fact]
    public async Task CreateUsersBatch_WithNullUsers_ShouldThrowArgumentNullException()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert
        var act = () => _repository.CreateUsersBatch(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("users");
    }

    [Fact]
    public async Task CreateUsersBatch_WithEmptyUsers_ShouldReturnZero()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var users = new List<User>();

        // Act
        var result = _repository.CreateUsersBatch(users);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CreateUsersBatch_WithInvalidUser_ShouldThrowAndRollback()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var users = new List<User>
        {
            CreateValidUser(1),
            CreateInvalidUser(), // This should cause rollback
            CreateValidUser(3)
        };

        // Act & Assert
        var act = () => _repository.CreateUsersBatch(users);
        act.Should().Throw<ArgumentException>();

        // Verify rollback - no users should be created
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateUsersBatchAsync_WithValidUsers_ShouldCreateAllUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var users = new List<User>
        {
            CreateValidUser(1),
            CreateValidUser(2),
            CreateValidUser(3)
        };

        // Act
        var result = await _repository.CreateUsersBatchAsync(users);

        // Assert
        result.Should().Be(3);
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(3);
    }

    [Fact]
    public async Task CreateUsersBatchAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var users = new List<User> { CreateValidUser(1) };
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = async () => await _repository.CreateUsersBatchAsync(users, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task TransferUserData_WithValidUsers_ShouldUpdateTargetUser()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.TransferUserData(1, 2, "test transfer");

        // Assert
        result.Should().BeTrue();
        
        var targetUser = _repository.GetUserById(2);
        targetUser.Should().NotBeNull();
        targetUser!.Name.Should().Contain("Transferred from Test User 1");
        targetUser.Email.Should().StartWith("transferred_");
    }

    [Fact]
    public async Task TransferUserData_WithNonExistentSourceUser_ShouldReturnFalse()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.TransferUserData(999, 2, "test transfer");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TransferUserData_WithNonExistentTargetUser_ShouldReturnFalse()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.TransferUserData(1, 999, "test transfer");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TransferUserDataAsync_WithValidUsers_ShouldUpdateTargetUser()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = await _repository.TransferUserDataAsync(1, 2, "test transfer");

        // Assert
        result.Should().BeTrue();
        
        var targetUser = await _repository.GetUserByIdAsync(2);
        targetUser.Should().NotBeNull();
        targetUser!.Name.Should().Contain("Transferred from Test User 1");
        targetUser.Email.Should().StartWith("transferred_");
    }

    [Fact]
    public async Task ExecuteWithTransaction_ShouldProcessWithProvidedTransaction()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        await Connection.OpenAsync();
        using var transaction = await Connection.BeginTransactionAsync();

        // Act
        var result = _repository.ExecuteWithTransaction(transaction, 1, "TEST_OPERATION");

        // Assert
        result.Should().Be(1);
        
        // Commit to verify the change
        await transaction.CommitAsync();
        
        var user = _repository.GetUserById(1);
        user.Should().NotBeNull();
        user!.Name.Should().StartWith("PROCESSED_TEST_OPERATION_");
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetUsersPaginated_FirstPage_ShouldReturnCorrectResults()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var (users, totalCount) = _repository.GetUsersPaginated(1, 2);

        // Assert
        users.Should().HaveCount(2);
        totalCount.Should().Be(3);
        // Should be ordered by CreatedAt DESC
        users.First().Name.Should().Be("Test User 3");
    }

    [Fact]
    public async Task GetUsersPaginated_SecondPage_ShouldReturnCorrectResults()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var (users, totalCount) = _repository.GetUsersPaginated(2, 2);

        // Assert
        users.Should().HaveCount(1); // Only 1 user left on second page
        totalCount.Should().Be(3);
        users.First().Name.Should().Be("Test User 1"); // Oldest user
    }

    [Fact]
    public async Task GetUsersPaginated_InvalidPageNumber_ShouldCorrectToPageOne()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var (users, totalCount) = _repository.GetUsersPaginated(0, 10); // Invalid page number

        // Assert
        users.Should().HaveCount(3); // All users on corrected page 1
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetUsersPaginated_InvalidPageSize_ShouldCorrectToDefaultSize()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var (users, totalCount) = _repository.GetUsersPaginated(1, 0); // Invalid page size

        // Assert
        users.Should().HaveCount(3); // All users with corrected page size
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetUsersPaginatedAsync_ShouldReturnCorrectResults()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var (users, totalCount) = await _repository.GetUsersPaginatedAsync(1, 2);

        // Assert
        users.Should().HaveCount(2);
        totalCount.Should().Be(3);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchUsers_ByName_ShouldReturnMatchingUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers("User 1", UserSearchType.Name);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Test User 1");
    }

    [Fact]
    public async Task SearchUsers_ByEmail_ShouldReturnMatchingUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers("test1@", UserSearchType.Email);

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task SearchUsers_ByNameAndEmail_ShouldReturnMatchingUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers("example.com", UserSearchType.NameAndEmail);

        // Assert
        result.Should().HaveCount(3); // All test users have @example.com
    }

    [Fact]
    public async Task SearchUsers_FullText_ShouldReturnMatchingUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers("Test", UserSearchType.FullText);

        // Assert
        result.Should().HaveCount(3); // All test users contain "Test"
    }

    [Fact]
    public async Task SearchUsers_WithEmptySearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers("", UserSearchType.Name);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchUsers_WithWhitespaceSearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers("   ", UserSearchType.Name);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchUsers_WithNullSearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.SearchUsers(null!, UserSearchType.Name);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetUserStatistics_WithData_ShouldReturnCorrectStatistics()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var stats = _repository.GetUserStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalUsers.Should().Be(3);
        stats.MostCommonEmailDomain.Should().Be("example.com");
        stats.FirstUserCreated.Should().NotBeNull();
        stats.LastUserCreated.Should().NotBeNull();
        stats.AverageUsersPerDay.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUserStatistics_WithEmptyDatabase_ShouldReturnZeroStatistics()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act
        var stats = _repository.GetUserStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalUsers.Should().Be(0);
        stats.UsersCreatedToday.Should().Be(0);
        stats.UsersCreatedThisWeek.Should().Be(0);
        stats.UsersCreatedThisMonth.Should().Be(0);
        stats.FirstUserCreated.Should().BeNull();
        stats.LastUserCreated.Should().BeNull();
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task Repository_ConnectionNotOpen_ShouldHandleAutomatically()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await Connection.CloseAsync(); // Ensure connection is closed

        // Act & Assert - Should not throw, repository should handle connection opening
        var users = _repository.GetAllUsers();
        users.Should().NotBeNull();
    }

    [Fact]
    public async Task Repository_MultipleOperationsWithConnectionManagement_ShouldWork()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert - Multiple operations should work regardless of connection state
        var initialUsers = _repository.GetAllUsers();
        initialUsers.Should().BeEmpty();

        var user = CreateValidUser();
        var createResult = _repository.CreateUser(user);
        createResult.Should().Be(1);

        var afterCreateUsers = _repository.GetAllUsers();
        afterCreateUsers.Should().HaveCount(1);

        var stats = _repository.GetUserStatistics();
        stats.TotalUsers.Should().Be(1);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Repository_MultipleQueries_ShouldPerformEfficiently()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var startTime = DateTime.Now;
        
        for (int i = 0; i < 100; i++)
        {
            _repository.GetAllUsers();
        }
        
        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        // Assert
        duration.TotalMilliseconds.Should().BeLessThan(5000); // Should complete in less than 5 seconds
    }

    [Fact]
    public async Task Repository_ConcurrentReads_ShouldHandleCorrectly()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var tasks = new List<Task<IList<User>>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_repository.GetAllUsersAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.Count == 3); // All should return same count
    }

    #endregion
}
