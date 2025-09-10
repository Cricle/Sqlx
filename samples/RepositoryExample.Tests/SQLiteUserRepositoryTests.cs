// -----------------------------------------------------------------------
// <copyright file="SQLiteUserRepositoryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for SQLiteUserRepository.
/// </summary>
public class SQLiteUserRepositoryTests : TestBase
{
    private SQLiteUserRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteUserRepositoryTests"/> class.
    /// </summary>
    public SQLiteUserRepositoryTests()
    {
        _repository = new SQLiteUserRepository(Connection);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _repository = null!;
        }
        base.Dispose(disposing);
    }

    [Fact]
    public async Task GetAllUsers_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act
        var result = _repository.GetAllUsers();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllUsers_WithData_ShouldReturnAllUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.GetAllUsers();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(u => u.Name == "Test User 1");
        result.Should().Contain(u => u.Name == "Test User 2");
        result.Should().Contain(u => u.Name == "Test User 3");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act
        var result = await _repository.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllUsersAsync_WithData_ShouldReturnAllUsers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(u => u.Name == "Test User 1");
        result.Should().Contain(u => u.Name == "Test User 2");
        result.Should().Contain(u => u.Name == "Test User 3");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        await InitializeDatabaseAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = async () => await _repository.GetAllUsersAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.GetUserById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test User 1");
        result.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetUserById_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.GetUserById(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test User 1");
        result.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUser_WithValidUser_ShouldCreateAndReturnAffectedRows()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var user = CreateValidUser();

        // Act
        var result = _repository.CreateUser(user);

        // Assert
        result.Should().Be(1);
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateUser_WithNullUser_ShouldThrowNullReferenceException()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert
        // Generated code doesn't validate null parameters, so it throws NullReferenceException
        var act = () => _repository.CreateUser(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task CreateUserAsync_WithValidUser_ShouldCreateAndReturnAffectedRows()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var user = CreateValidUser();

        // Act
        var result = await _repository.CreateUserAsync(user);

        // Assert
        result.Should().Be(1);
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullUser_ShouldThrowNullReferenceException()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert
        // Generated code doesn't validate null parameters, so it throws NullReferenceException
        var act = async () => await _repository.CreateUserAsync(null!);
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task UpdateUser_WithValidUser_ShouldUpdateAndReturnAffectedRows()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();
        var user = new User { Id = 1, Name = "Updated User", Email = "updated@example.com", CreatedAt = DateTime.Now };

        // Act
        var result = _repository.UpdateUser(user);

        // Assert
        result.Should().Be(1);
        var updatedUser = _repository.GetUserById(1);
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be("Updated User");
        updatedUser.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task UpdateUser_WithNonExistingUser_ShouldReturnZero()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var user = new User { Id = 999, Name = "Non-existing User", Email = "nonexisting@example.com", CreatedAt = DateTime.Now };

        // Act
        var result = _repository.UpdateUser(user);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task UpdateUser_WithNullUser_ShouldThrowNullReferenceException()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert
        // Generated code doesn't validate null parameters, so it throws NullReferenceException
        var act = () => _repository.UpdateUser(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task DeleteUser_WithExistingUser_ShouldDeleteAndReturnAffectedRows()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.DeleteUser(1);

        // Assert
        result.Should().Be(1);
        var deletedUser = _repository.GetUserById(1);
        deletedUser.Should().BeNull();
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(2); // Started with 3, deleted 1
    }

    [Fact]
    public async Task DeleteUser_WithNonExistingUser_ShouldReturnZero()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await SeedTestDataAsync();

        // Act
        var result = _repository.DeleteUser(999);

        // Assert
        result.Should().Be(0);
        var userCount = await GetUserCountAsync();
        userCount.Should().Be(3); // No change
    }

    [Fact]
    public async Task Repository_MultipleCrudOperations_ShouldWorkCorrectly()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert
        // 1. Initially empty
        var initialUsers = _repository.GetAllUsers();
        initialUsers.Should().BeEmpty();

        // 2. Create a user
        var newUser = CreateValidUser();
        var createResult = _repository.CreateUser(newUser);
        createResult.Should().Be(1);

        // 3. Verify creation
        var allUsers = _repository.GetAllUsers();
        allUsers.Should().HaveCount(1);
        var createdUser = allUsers.First();
        createdUser.Name.Should().Be(newUser.Name);
        createdUser.Email.Should().Be(newUser.Email);

        // 4. Update the user
        createdUser.Name = "Updated Name";
        var updateResult = _repository.UpdateUser(createdUser);
        updateResult.Should().Be(1);

        // 5. Verify update
        var updatedUser = _repository.GetUserById(createdUser.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be("Updated Name");

        // 6. Delete the user
        var deleteResult = _repository.DeleteUser(createdUser.Id);
        deleteResult.Should().Be(1);

        // 7. Verify deletion
        var finalUsers = _repository.GetAllUsers();
        finalUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_AsyncOperations_ShouldWorkCorrectly()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert
        // 1. Initially empty
        var initialUsers = await _repository.GetAllUsersAsync();
        initialUsers.Should().BeEmpty();

        // 2. Create a user asynchronously
        var newUser = CreateValidUser();
        var createResult = await _repository.CreateUserAsync(newUser);
        createResult.Should().Be(1);

        // 3. Verify creation asynchronously
        var allUsers = await _repository.GetAllUsersAsync();
        allUsers.Should().HaveCount(1);
        var createdUser = allUsers.First();

        // 4. Get user by ID asynchronously
        var retrievedUser = await _repository.GetUserByIdAsync(createdUser.Id);
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Name.Should().Be(newUser.Name);
        retrievedUser.Email.Should().Be(newUser.Email);
    }

    [Fact]
    public async Task Repository_ConnectionManagement_ShouldHandleStateChanges()
    {
        // Arrange
        await InitializeDatabaseAsync();
        
        // Ensure connection is closed initially
        if (Connection.State == System.Data.ConnectionState.Open)
        {
            await Connection.CloseAsync();
        }

        // Act & Assert
        // Repository should handle opening connection when needed
        var users = _repository.GetAllUsers();
        users.Should().NotBeNull();

        // Connection should be managed properly for multiple operations
        var user = CreateValidUser();
        var createResult = _repository.CreateUser(user);
        createResult.Should().Be(1);

        var retrievedUsers = _repository.GetAllUsers();
        retrievedUsers.Should().HaveCount(1);
    }
}
