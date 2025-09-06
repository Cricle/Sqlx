// -----------------------------------------------------------------------
// <copyright file="UserRepositoryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

public class UserRepositoryTests
{
    [Fact]
    public void UserRepository_Constructor_ShouldNotThrow()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        
        // Act & Assert
        var act = () => new UserRepository(connection);
        act.Should().NotThrow();
    }

    [Fact]
    public void GetAllUsers_ShouldReturnMockData()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = repository.GetAllUsers();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "John Doe");
        result.Should().Contain(u => u.Name == "Jane Smith");
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnMockData()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = await repository.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "John Doe");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.GetAllUsersAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetUserById_WithValidId_ShouldReturnUser()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = repository.GetUserById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("John Doe");
    }

    [Fact]
    public void GetUserById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = repository.GetUserById(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = await repository.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = await repository.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreateUser_WithValidUser_ShouldReturnOne()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);
        var user = new User { Name = "Test User", Email = "test@example.com", CreatedAt = DateTime.Now };

        // Act
        var result = repository.CreateUser(user);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidUser_ShouldReturnOne()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);
        var user = new User { Name = "Test User", Email = "test@example.com", CreatedAt = DateTime.Now };

        // Act
        var result = await repository.CreateUserAsync(user);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void UpdateUser_WithValidUser_ShouldReturnOne()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);
        var user = new User { Id = 1, Name = "Updated User", Email = "updated@example.com", CreatedAt = DateTime.Now };

        // Act
        var result = repository.UpdateUser(user);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void DeleteUser_WithValidId_ShouldReturnOne()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act
        var result = repository.DeleteUser(1);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void UserRepository_ShouldImplementIUserService()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);

        // Act & Assert
        repository.Should().BeAssignableTo<IUserService>();
    }

    [Fact]
    public void UserRepository_AllMethodsShouldBeImplemented()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new UserRepository(connection);
        var interfaceType = typeof(IUserService);
        var repositoryType = typeof(UserRepository);

        // Act & Assert
        foreach (var method in interfaceType.GetMethods())
        {
            var implementingMethod = repositoryType.GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray());
            implementingMethod.Should().NotBeNull($"Method {method.Name} should be implemented");
        }
    }
}
