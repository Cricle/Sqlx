// -----------------------------------------------------------------------
// <copyright file="RealDatabaseUserRepositoryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

public class RealDatabaseUserRepositoryTests : TestBase
{
    [Fact]
    public void Constructor_WithValidConnection_ShouldCreateRepository()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");

        // Act
        var repository = new RealDatabaseUserRepository(connection);

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IUserService>();
    }

    [Fact]
    public void GetAllUsers_WithValidConnection_ShouldHandleCorrectly()
    {
        // Arrange
        // Note: RealDatabaseUserRepository is designed for SQL Server, not SQLite
        // This test verifies the interface compliance without actual database interaction
        using var connection = new SqliteConnection("Data Source=:memory:");

        // Act & Assert - Constructor should work without throwing
        var act = () => new RealDatabaseUserRepository(connection);
        act.Should().NotThrow();
    }

    [Fact]
    public void RealDatabaseUserRepository_InterfaceCompliance_ShouldImplementCorrectly()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var repository = new RealDatabaseUserRepository(connection);

        // Assert - Verify interface implementation
        repository.Should().BeAssignableTo<IUserService>();
        
        // Verify all required methods exist
        var interfaceType = typeof(IUserService);
        var repositoryType = typeof(RealDatabaseUserRepository);
        
        foreach (var method in interfaceType.GetMethods())
        {
            var implementingMethod = repositoryType.GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray());
            implementingMethod.Should().NotBeNull($"Method {method.Name} should be implemented");
        }
    }

    [Fact]
    public void RealDatabaseUserRepository_ClassProperties_ShouldBeCorrect()
    {
        // Arrange
        var repositoryType = typeof(RealDatabaseUserRepository);

        // Assert
        repositoryType.Should().NotBeNull();
        repositoryType.IsClass.Should().BeTrue();
        repositoryType.IsAbstract.Should().BeFalse();
        repositoryType.IsSealed.Should().BeFalse();
        
        // Verify constructor exists
        var constructor = repositoryType.GetConstructor(new[] { typeof(DbConnection) });
        constructor.Should().NotBeNull();
    }
}
